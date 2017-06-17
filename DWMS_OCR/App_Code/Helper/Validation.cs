using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DWMS_OCR.App_Code.Helper
{
    class Validation
    {
        public static bool IsNric(string nric)
        {
            if (nric.Length == 9)
            {
                nric = nric.ToUpper();
                string first = nric.Substring(0, 1);
                if (!(first == "S" || first == "T"))
                    return false;

                //|| nric.StartsWith("T") || nric.StartsWith("G")
                //remainder 10->A,9->B
                string[] checkChar = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "Z", "J", "P" };
                int[] weight = { 2, 7, 6, 5, 4, 3, 2 };
                int sum = 0;
                int remainder;

                for (int i = 0; i < 7; i++)
                {
                    string s = nric.Substring(i + 1, 1);

                    if (!IsInteger(s))
                        return false;

                    sum = sum + Convert.ToInt32(s) * weight[i];
                }
                if (first == "T") sum += 4;
                Math.DivRem(sum, 11, out remainder);
                remainder = 11 - remainder;
                string c = checkChar[remainder - 1];

                return (c == nric.Substring(8, 1));
            }
            else
            {
                return false;
            }
        }

        public static bool IsFin(string fin)
        {
            if (fin.Length > 0)
            {
                fin = fin.ToUpper();
                string first = fin.Substring(0, 1);

                if (first == "X")
                {
                    if (first.Length >= 8 && first.Length <= 10)
                        return true;
                    else
                        return false;
                }

                if (fin.Length == 9)
                {               
                    if (!(first == "F" || first == "G"))
                        return false;

                    //remainder =10->K, 9->L ...
                    string[] checkChar = { "K", "L", "M", "N", "P", "Q", "R", "T", "U", "W", "X" };
                    int[] weight = { 2, 7, 6, 5, 4, 3, 2 };
                    int sum = 0;
                    int remainder;

                    for (int i = 0; i < 7; i++)
                    {
                        string s = fin.Substring(i + 1, 1);

                        if (!IsInteger(s))
                            return false;

                        sum = sum + Convert.ToInt32(s) * weight[i];
                    }
                    if (first == "G") sum += 4;// add total to sum if start with T or G
                    Math.DivRem(sum, 11, out remainder);
                    remainder = 11 - remainder;
                    string c = checkChar[remainder - 1];

                    return (c == fin.Substring(8, 1));
                }
                else
                {
                    return false;
                }
            }
            else
                return false;
        }

        public static bool IsInteger(string sInt)
        {
            if (String.IsNullOrEmpty(sInt))
                return false;

            int i;
            bool isInteger = true;

            try
            {
                i = Convert.ToInt32(sInt);
            }
            catch
            {
                isInteger = false;
            }

            return isInteger;
        }

        public static bool IsDate(string sDate)
        {
            if (String.IsNullOrEmpty(sDate))
                return false;

            DateTime dt;
            bool isDate = true;

            try
            {
                dt = DateTime.Parse(sDate);
            }
            catch
            {
                isDate = false;
            }

            return isDate;
        }

        public static bool IsNricFormat(string nric)
        {
            if (nric.Length == 9)
            {
                char[] arr = nric.ToCharArray();
                return (Char.IsLetter(arr[0]) && IsNaturalNumber(nric.Substring(1, 7)) && Char.IsLetter(arr[8]));
            }
            else
            {
                return false;
            }
        }

        public static bool IsNaturalNumber(string sNumber)
        {
            if (IsInteger(sNumber))
                return (Convert.ToInt32(sNumber) > 0);
            else
                return false;
        }

        /// <summary>
        /// Check if the input string is a HLE reference number
        /// Sample: N11N12345
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsHLENumber(string s)
        {
            if (s.Length != 9)
                return false;

            //char[] arr=s.ToCharArray();
            //return (Char.IsLetter(arr[0]) && Char.IsLetter(arr[3]));

            string pattern = @"[a-zA-Z]{1}[\d]{2}[a-zA-Z]{1}[\d]{5}";
            return Regex.IsMatch(s, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Check if the input string is a Case reference number
        /// Sample: 12345R12
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsCaseNumber(string s)
        {
            if (s.Length != 8)
                return false;

            //string pattern = @"[\d]{5}[a-zA-Z]{1}[\d]{2}";
            string pattern = @"[a-zA-Z0-9]{1}[\d]{4}[a-zA-Z]{1}[\d]{2}";
            return Regex.IsMatch(s, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Check if the input string is a Sales reference number
        /// Sample: 1234567c
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsSalesNumber(string s)
        {
            if (s.Length != 8)
                return false;

            string pattern = @"[\d]{7}[a-zA-Z]{1}";
            return Regex.IsMatch(s, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Check if the input string is a SERS reference number
        /// Sample: 123456789
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsSersNumber(string s)
        {
            if (s.Length != 9)
                return false;

            string pattern = @"[\d]{9}";
            return Regex.IsMatch(s, pattern, RegexOptions.IgnoreCase);
        }

        public static bool IsGuid(string s)
        {
            if (String.IsNullOrEmpty(s))
                return false;

            Guid guid;
            bool isGuid = true;

            try
            {
                guid = new Guid(s);
            }
            catch
            {
                isGuid = false;
            }

            return isGuid;
        }

        public static bool IsEmail(string email)
        {
            Regex re = new Regex(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
            return re.IsMatch(email);
        }
    }
}
