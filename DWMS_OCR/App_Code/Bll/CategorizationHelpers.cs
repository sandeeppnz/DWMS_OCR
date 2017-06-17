using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using DWMS_OCR.App_Code.Helper;
using System.Text.RegularExpressions;
using DWMS_OCR.App_Code.Dal;
using System.Data;
using NHunspell;
using System.IO;
using System.Diagnostics;

namespace DWMS_OCR.App_Code.Bll
{
    class CategorizationHelpers
    {
        /// <summary>
        /// Check if input is integer
        /// </summary>
        /// <param name="sInt"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check if input is HLE
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsHle(string s)
        {
            if (s.Length != 9)
                return false;

            char[] arr = s.ToCharArray();
            int count = 0;

            foreach (char c in arr)
            {
                if (IsInteger(c.ToString()))
                {
                    count++;
                }
            }

            bool alphaIsValid = (Char.IsLetter(arr[0]) && Char.IsLetter(arr[3]));
            bool numberIsValid = count >= 4;

            return alphaIsValid && numberIsValid;
        }        

        /// <summary>
        /// Check if input is NRIC
        /// </summary>
        /// <param name="nric"></param>
        /// <returns></returns>
        public static bool IsNric(string nric)
        {
            //bool b = false;
            nric = nric.Trim().ToUpper();

            if (Validation.IsNric(nric) || Validation.IsFin(nric))
                return true;

            return false;

            //if (nric.Length < 9)
            //    return false;

            //string a = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            //string middle = nric.Substring(1, 7);
            //string last = nric.Substring(8, 1);

            //bool b2 = (nric.StartsWith("S") || nric.StartsWith("T")) && CountInteger(middle) > 3 && a.Contains(last);
            //b = a.Contains(nric.Substring(0, 1)) && Validation.IsInteger(middle) && a.Contains(last);

            //return b || b2;
        }

        /// <summary>
        /// Count the integers in the in input
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static int CountInteger(string inputString)
        {
            int i = 0;
            for (int k = 0; k < inputString.Length; k++)
            {
                if (Validation.IsInteger(inputString.Substring(k, 1)))
                    i++;
            }

            return i;
        }

        /// <summary>
        /// Remove the non-alphanumeric characters from the input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveNonAlphanumericCharacters(string input)
        {
            return Format.RemoveNonAlphanumericCharacters(input);
        }

        /// <summary>
        /// Remove alpha characters from the input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveAlphaCharacters(string input)
        {
            string temp = input.Trim();

            // Source: http://stackoverflow.com/questions/3210393/how-to-remove-all-non-alphanumeric-characters-from-a-string-except-dash
            Regex regex = new Regex("[a-z]*");
            temp = regex.Replace(temp, "");
            temp = temp.Replace(" ", string.Empty);
            return temp;
        }

        /// <summary>
        /// Do some modifications of the nric
        /// </summary>
        /// <param name="nric"></param>
        /// <returns></returns>
        public static string NricMapping(string nric)
        {
            if (string.IsNullOrEmpty(nric))
                return nric;

            nric = nric.ToLower();
            StringBuilder sb = new StringBuilder(nric);

            if (nric.StartsWith("5") || nric.StartsWith("8"))
            {
                sb[0] = 's';
            }

            if (nric.EndsWith("2"))
            {
                sb[8] = 'z';
            }

            if (nric.EndsWith("1"))
            {
                sb[8] = 'l';
            }

            for (int i = 1; i < 8; i++)
            {
                if (sb[i] == 's')
                {
                    sb[i] = '5';
                }

                if (sb[i] == 'o')
                {
                    sb[i] = '0';
                }
            }

            nric = sb.ToString().ToUpper();

            return nric;
        }

        /// <summary>
        /// Get the NRIC from the OCR text
        /// </summary>
        /// <param name="ocrText"></param>
        /// <returns></returns>
        public static string GetNricFromText(string ocrText)
        {
            string nric = string.Empty;

            if (String.IsNullOrEmpty(ocrText))
                return nric;

            string ocrTextLower = ocrText.ToLower();
            string[] words = ocrTextLower.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                string s = RemoveNonAlphanumericCharacters(word);

                if (IsNric(s))
                {
                    nric = s.ToUpper();
                    //nric = NricMapping(nric);
                    break;
                }
            }

            return nric;
        }

        /// <summary>
        /// Check if input has HLE number
        /// </summary>
        /// <param name="ocrText"></param>
        /// <returns></returns>
        public static bool HasStringHleNumber(string ocrText)
        {
            if (string.IsNullOrEmpty(ocrText))
                return false;

            string ocrTextLower = ocrText;
            string[] lines = ocrTextLower.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (line.Contains("ref"))
                {
                    string[] words = line.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string word in words)
                    {
                        string s = RemoveNonAlphanumericCharacters(word);

                        if (IsStrictHle(s))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the OCR Text contains the given name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool ContainsName(string name, string content)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(content))
                return false;

            var lines = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int count = 0;

            foreach (string s in lines)
            {
                if (content.Contains(s))
                    count++;
            }

            return count >= 2;
        }

        /// <summary>
        /// Check if the variable is found in the OCR text
        /// </summary>
        /// <param name="ocrText"></param>
        /// <param name="keywordVariable"></param>
        /// <returns></returns>
        public static bool HasVariable(string ocrText, KeywordVariableEnum keywordVariable)
        {
            bool result = false;

            if (!String.IsNullOrEmpty(ocrText))
            {
                // Split the OCR text into lines
                string ocrTextLower = ocrText.ToLower();
                string[] lines = ocrTextLower.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (keywordVariable == KeywordVariableEnum.HLE_Number)
                    result = HasHleNumber(lines);
                else if (keywordVariable == KeywordVariableEnum.NRIC)
                    result = HasNric(lines);
            }

            return result;
        }

        /// <summary>
        /// Check if the line contains a HLE number
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool HasHleNumber(string[] lines)
        {
            bool result = false;

            foreach (string line in lines)
            {
                // If the text "Ref" text
                if (line.Contains(Constants.HleNumberRefPrefix.ToLower()))
                {
                    // Split the line into words
                    string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string word in words)
                    {
                        string s = RemoveNonAlphanumericCharacters(word);

                        if (IsStrictHle(s))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }        

        /// <summary>
        /// Check if the line contains a NRIC
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool HasNric(string[] lines)
        {
            bool result = false;

            foreach (string line in lines)
            {
                // Split the line into words
                string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in words)
                {
                    string s = RemoveNonAlphanumericCharacters(word);

                    if (IsNric(s))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check if the keyword exists in the OCR text
        /// </summary>
        /// <param name="ocrText"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static bool HasKeyword(string ocrText, string keyword)
        {
            bool result = false;

            if (!String.IsNullOrEmpty(ocrText))
                result = ocrText.ToLower().Contains(keyword.ToLower());

            return result;
        }

        /// <summary>
        /// Check if the input string is valid for relevance ranking.
        /// </summary>
        /// <param name="inputText">String to validate</param>
        /// <returns>True if the string is valid for relevance ranking. False if otherwise.</returns>
        public static bool IsValidTextForRelevanceRanking(string inputText)
        {
            // Get the parameter values
            ParameterDb parameterDb = new ParameterDb();
            int MINIMUM_ENGLISH_WORD_COUNT = parameterDb.GetMinimumEnglishWordCount();
            decimal MINIMUM_ENGLISH_WORD_PERCENTAGE = parameterDb.GetMinimumEnglishWordPercentage();
            int MINIMUM_WORD_LENGTH = parameterDb.GetMinimumWordLength();

            string libAffPath = string.Empty;
            string libDicPath = string.Empty;

            Retrieve.GetHunspellResourcesPath(out libAffPath, out libDicPath);

            bool result = false;
            ArrayList englishWords = new ArrayList();

            using (Hunspell spellChecker = new Hunspell(libAffPath, libDicPath))
            {
                result = IsValidTextForRelevanceRanking(inputText, spellChecker, MINIMUM_WORD_LENGTH, MINIMUM_ENGLISH_WORD_COUNT, MINIMUM_ENGLISH_WORD_PERCENTAGE, ref englishWords);
            }

            return result;
        }

        /// <summary>
        /// Check if the input string is valid for relevance ranking.
        /// </summary>
        /// <param name="inputText"></param>
        /// <param name="MINIMUM_WORD_LENGTH"></param>
        /// <param name="MINIMUM_ENGLISH_WORD_COUNT"></param>
        /// <param name="MINIMUM_ENGLISH_WORD_PERCENTAGE"></param>
        /// <param name="englishWords"></param>
        /// <returns></returns>
        public static bool IsValidTextForRelevanceRanking(string inputText, int MINIMUM_WORD_LENGTH,
            int MINIMUM_ENGLISH_WORD_COUNT, decimal MINIMUM_ENGLISH_WORD_PERCENTAGE, ref ArrayList englishWords)
        {
            string libAffPath = string.Empty;
            string libDicPath = string.Empty;

            Retrieve.GetHunspellResourcesPath(out libAffPath, out libDicPath);

            using (Hunspell spellChecker = new Hunspell(libAffPath, libDicPath))
            {
                return IsValidTextForRelevanceRanking(inputText, spellChecker, MINIMUM_WORD_LENGTH, MINIMUM_ENGLISH_WORD_COUNT, MINIMUM_ENGLISH_WORD_PERCENTAGE, ref englishWords);
            }
        }

        /// <summary>
        /// Check if the input string is valid for relevance ranking.
        /// </summary>
        /// <param name="ocrText"></param>
        /// <param name="spellChecker"></param>
        /// <param name="MINIMUM_WORD_LENGTH"></param>
        /// <param name="MINIMUM_ENGLISH_WORD_COUNT"></param>
        /// <param name="MINIMUM_ENGLISH_WORD_PERCENTAGE"></param>
        /// <param name="englishWords"></param>
        /// <returns></returns>
        public static bool IsValidTextForRelevanceRanking(string ocrText, Hunspell spellChecker, int MINIMUM_WORD_LENGTH,
            int MINIMUM_ENGLISH_WORD_COUNT, decimal MINIMUM_ENGLISH_WORD_PERCENTAGE, ref ArrayList englishWords)
        {
            string[] arr = Util.SplitString(ocrText, true, true);

            decimal wordCount = 0;

            foreach (string word in arr)
            {
                if (word.Length >= MINIMUM_WORD_LENGTH)
                    wordCount++;
            }

            if (wordCount == 0)
                return false;

            decimal englishWordCount = 0;

            // Get all the stop words
            StopWordDb stopWordDb = new StopWordDb();
            ArrayList stopWordsList = stopWordDb.GetStopWordsToArrayList();

            foreach (string word in arr)
            {
                try
                {
                    if (word.Length >= MINIMUM_WORD_LENGTH && !stopWordsList.Contains(word.ToLower()) && spellChecker.Spell(word))
                    {
                        englishWords.Add(word);
                        englishWordCount++;
                    }
                }
                catch (Exception ex)
                {
                    string warningString = String.Format("Warning (CategorizationHelpers.IsValidTextForRelevanceRanking): Message={0}, StackTrace={1}",
                        ex.Message, ex.StackTrace);

                    Util.DWMSLog("CategorizationHelpers.IsValidTextForRelevanceRanking", warningString, EventLogEntryType.Warning);
                }
            }

            if (englishWordCount < MINIMUM_ENGLISH_WORD_COUNT)
                return false;

            if ((englishWordCount / wordCount) < MINIMUM_ENGLISH_WORD_PERCENTAGE)
                return false;

            return true;
        }

        public static ArrayList GetReferenceNumbers(string[] lines)
        {
            ArrayList refNos = new ArrayList();

            foreach (string line in lines)
            {
                string refNoTemp = string.Empty;

                // Split the line into words
                string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in words)
                {
                    string s = RemoveNonAlphanumericCharacters(word);

                    if (IsStrictHle(s) || IsStrictResale(s) || IsStrictSales(s) || IsStrictSers(s))
                    {
                        if (!refNos.Contains(s))
                            refNos.Add(s);
                    }
                }
            }

            return refNos;
        }

        public static string GetHleRefNo(string[] lines)
        {
            string result = string.Empty;

            foreach (string line in lines)
            {
                // If the text "Ref" text
                if (line.Contains(Constants.HleNumberRefPrefix.ToLower()))
                {
                    // Split the line into words
                    string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string word in words)
                    {
                        string s = RemoveNonAlphanumericCharacters(word);

                        if (IsStrictHle(s))
                        {
                            result = s;
                        }
                    }
                }
            }

            return result;
        }

        public static string GetResaleRefNo(string[] lines)
        {
            string result = string.Empty;

            foreach (string line in lines)
            {
                // Split the line into words
                string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in words)
                {
                    string s = RemoveNonAlphanumericCharacters(word);

                    if (IsStrictResale(s))
                    {
                        result = s;
                    }
                }
            }

            return result;
        }

        public static string GetSaleRefNo(string[] lines)
        {
            string result = string.Empty;

            foreach (string line in lines)
            {
                // Split the line into words
                string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in words)
                {
                    string s = RemoveNonAlphanumericCharacters(word);

                    if (IsStrictSales(s))
                    {
                        result = s;
                    }
                }
            }

            return result;
        }

        public static string GetSersRefNo(string[] lines)
        {
            string result = string.Empty;

            foreach (string line in lines)
            {
                // Split the line into words
                string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in words)
                {
                    string s = RemoveNonAlphanumericCharacters(word);

                    if (IsStrictSers(s))
                    {
                        result = s;
                    }
                }
            }

            return result;
        }

        public static bool IsStrictHle(string s)
        {
            if (s.Length != 9)
                return false;

            char[] arr = s.ToCharArray();
            int count = 0;

            foreach (char c in arr)
            {
                if (IsInteger(c.ToString()))
                {
                    count++;
                }
            }

            bool alphaIsValid = (Char.IsLetter(arr[0]) && Char.IsLetter(arr[3]));
            bool numberIsValid = count == 7;

            return alphaIsValid && numberIsValid;
        }

        public static bool IsStrictResale(string s)
        {
            if (s.Length != 8)
                return false;

            char[] arr = s.ToCharArray();
            int count = 0;

            foreach (char c in arr)
            {
                if (IsInteger(c.ToString()))
                {
                    count++;
                }
            }

            bool alphaIsValid = Char.IsLetter(arr[5]);
            bool numberIsValid = count == 7;

            return alphaIsValid && numberIsValid;
        }

        public static bool IsStrictSales(string s)
        {
            if (s.Length != 8)
                return false;

            char[] arr = s.ToCharArray();
            int count = 0;

            foreach (char c in arr)
            {
                if (IsInteger(c.ToString()))
                {
                    count++;
                }
            }

            bool alphaIsValid = Char.IsLetter(arr[7]);
            bool numberIsValid = count == 7;

            return alphaIsValid && numberIsValid;
        }

        public static bool IsStrictSers(string s)
        {
            if (s.Length != 9)
                return false;

            char[] arr = s.ToCharArray();
            int count = 0;

            foreach (char c in arr)
            {
                if (IsInteger(c.ToString()))
                {
                    count++;
                }
            }

            bool numberIsValid = count == 9;

            return numberIsValid;
        }

        public static string GetHleNumberFromOcr(string ocrText)
        {
            string result = string.Empty;

            if (string.IsNullOrEmpty(ocrText))
                return result;

            string ocrTextLower = ocrText;
            string[] lines = ocrTextLower.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Split the line into words
                string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);
                bool breakInner = false;
                foreach (string word in words)
                {
                    string s = RemoveNonAlphanumericCharacters(word);

                    if (IsStrictHle(s) || IsStrictResale(s) || IsStrictSales(s) || IsStrictSers(s))
                    {
                        result = s;
                        breakInner = true;
                        break;
                    }
                }

                if (breakInner)
                    break;
            }

            return result;
        }
    }
}
