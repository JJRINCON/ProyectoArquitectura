﻿using Microsoft.AspNetCore.Mvc;
using ProyectoArquitectura.Models;
using System.Diagnostics;

namespace ProyectoArquitectura.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Result(DataModel data)
        {
            ViewBag.result = ConvertNumber(data.Number);
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private ResultModel ConvertNumber(string number)
        {
            var sign = double.Parse(number) > 0 ? "0" : "1"; 
            var splitNumber = number.Split('.');
            var binaryIntegerPart = ConvertIntegerPartToBinary(int.Parse(splitNumber[0]));
            var binaryDecimalPart = ConvertToBinaryDecimalPart(int.Parse(splitNumber[1]), (binaryIntegerPart.Contains('1') ? binaryIntegerPart.Length : 0), 23);
            var doubleBinaryDecimalPart = ConvertToBinaryDecimalPart(int.Parse(splitNumber[1]), (binaryIntegerPart.Contains('1') ? binaryIntegerPart.Length : 0), 52);
            var simpleDenormalize = Denormalize(binaryIntegerPart, binaryDecimalPart).Split("*");
            var doubleDenormalize = Denormalize(binaryIntegerPart, doubleBinaryDecimalPart).Split("*");
            var simpleExponent = ValidateExponent(ConvertIntegerPartToBinary(127 + int.Parse(simpleDenormalize[1])), 8);
            var doubleExponent = ValidateExponent(ConvertIntegerPartToBinary(1023 + int.Parse(doubleDenormalize[1])), 11);
            var mantissa = simpleDenormalize[0].Split(".")[1].Substring(0, 23);
            var doubleMantissa = doubleDenormalize[0].Split(".")[1].Substring(0,52);
            _logger.LogInformation("signo: " + sign);
            _logger.LogInformation("exponente: " + simpleExponent);
            _logger.LogInformation("simple manisa: " + mantissa);
            return new ResultModel()
            {
                 Sign = sign,
                 SimpleExponent = simpleExponent,
                 DoubleExponent = doubleExponent,
                 SimpleMantissa = mantissa,
                 DoubleMantissa = doubleMantissa,
                 SimpleHexadecimalValue = ConvertToHexadecimal(sign + simpleExponent + mantissa),
                 DoubleHexadecimalValue = ConvertToHexadecimal(sign + doubleExponent + doubleMantissa),
            };
        }

        private string ValidateExponent(string exponent, int size)
        {
            string result = exponent;
            if (exponent.Length < size)
            {
                result = exponent.PadLeft(size, '0');
            }
            return result;
        }

        private string ConvertIntegerPartToBinary(int integerPart)
        {
            string result = "";
            int actualNumber = integerPart;
            while(actualNumber > 1)
            {
                result += actualNumber % 2;
                actualNumber /= 2;
            }
            result += actualNumber;
            return new String(result.ToCharArray().Reverse().ToArray());
        }

        private string ConvertToBinaryDecimalPart(int decimalPart, int integerPartSize, int precision)
        {
            var result = "";
            var actualNumber = decimalPart;
            _logger.LogInformation("tmanio parte entera: " + integerPartSize);
            for (int i = 0; i < precision + 10; i++)
            {
                var aux = actualNumber * 2;
                if (aux.ToString().Length > decimalPart.ToString().Length)
                {
                    result += "1";
                    actualNumber = int.Parse(aux.ToString().Substring(1, aux.ToString().Length - 1));
                } else
                {
                    result += "0";
                    actualNumber = aux;
                }
            }
            return result;
        }

        private string Denormalize(string binaryIntegerPart, string binaryDecimalPart)
        {
            var result = "";
            var slipping = 0;
            if (binaryIntegerPart.Contains('1'))
            {
                var firstOne = binaryIntegerPart.IndexOf('1') + 1;
                result = "1." + binaryIntegerPart.Substring(firstOne, binaryIntegerPart.Length - 1) + binaryDecimalPart;
                slipping = binaryIntegerPart.Length - firstOne;
            } else
            {
                var firstOne = binaryDecimalPart.IndexOf('1') + 1;
                result = "1." + binaryDecimalPart.Substring(firstOne, binaryDecimalPart.Length - 1);
                slipping -= firstOne;
                _logger.LogInformation("negativa: " + result);
            }
            return result + "*" + slipping;
        }

        private string ConvertToHexadecimal(string result)
        {
            _logger.LogInformation("hexa: " + result);
            var hexadecimalValue = "";
            for (int i = 0; i < result.Length; i += 4)
            {
                hexadecimalValue += ValidateNumber(ConvertBinaryToDecimal(result.Substring(i,  4)));
            }
            return hexadecimalValue;
        }

        private int ConvertBinaryToDecimal(string binary)
        {
            var currentExponent = 0;
            var decimalValue = 0;
            for (int i = binary.Length - 1; i >= 0; i--)
            {
                int actualNumber = int.Parse(binary[i].ToString());
                decimalValue += (actualNumber == 1) ? ((int)Math.Pow(2, currentExponent)) : 0;
                currentExponent++;
            }
            return decimalValue;
        }

        private string ValidateNumber(int number)
        {
            var newNumber = number.ToString();
            switch (number)
            {
                case 10:
                    newNumber = "A";
                    break;
                case 11:
                    newNumber = "B";
                    break;
                case 12:
                    newNumber = "C";
                    break;
                case 13:
                    newNumber = "D";
                    break;
                case 14:
                    newNumber = "E";
                    break;
                case 15:
                    newNumber = "F";
                    break;
            }

            return newNumber;
        }
    }
}