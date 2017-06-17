using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DWMS_OCR.App_Code.Bll;
using System.Globalization;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Helper
{
    class Format
    {

        /// <summary>
        /// Format date time
        /// </summary>
        /// <param name="date"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string FormatDateTime(object date, DateTimeFormat type)
        {
            string typeStr = type.ToString();

            if (string.IsNullOrEmpty(typeStr))
                return null;
            else
            {
                typeStr = typeStr.Replace("_dash_", "-");
                typeStr = typeStr.Replace("_C_", ", ");
                typeStr = typeStr.Replace("_Col_", ":");
                typeStr = typeStr.Replace("_Hyp_", "-");
                typeStr = typeStr.Replace("__", " ");
                typeStr = typeStr.Replace("_", "/");
            }

            string dateString = String.Format("{0:" + typeStr + "}", date);
            return dateString;
        }

        /// <summary>
        /// Remove non-alphanumeric characters
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveNonAlphanumericCharacters(string input)
        {
            string temp = input.Trim();

            // Source: http://stackoverflow.com/questions/3210393/how-to-remove-all-non-alphanumeric-characters-from-a-string-except-dash
            Regex regex = new Regex("[^\\w]*");
            temp = regex.Replace(temp, "");

            return temp;
        }

        /// <summary>
        /// Create the Set number
        /// </summary>
        /// <param name="groupCode"></param>
        /// <param name="operationCode"></param>
        /// <param name="dateIn"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static string FormatSetNumber(string groupCode, string operationCode, DateTime dateIn, int sequence)
        {
            //return String.Format(Constants.SetNumberFormat, groupCode.ToUpper(), operationCode.ToUpper(), FormatDateTime(dateIn, DateTimeFormat.yyMMdd), sequence.ToString().PadLeft(5, '0'));
            return String.Format(Constants.SetNumberFormat, "EA", operationCode.ToUpper(), FormatDateTime(dateIn, DateTimeFormat.yyMMdd), sequence.ToString().PadLeft(5, '0'));
        }

        public enum DateTimeFormatCDB
        {
            yyMMdd,
            dd_MM_yy,
            dd_Hyp_MM_Hyp_yyyy,
            dd_MM_yyyy,
            dd__MMM__yyyy,
            dd__MMM__yy,
            ddd_C_d__MMM__yyyy,
            dMMMyyyyhmmtt,
            yyyyMMdd_dash_HHmmss,//edit by Calvin search format
            d__MMM__yyyy_C_h_Col_mm__tt
        }
        //No use
        public static string GetMetaDataValueInMetaDataEndDateFormat(string dateValue)
        {
            dateValue = dateValue.Trim();

            //return if empty
            if (string.IsNullOrEmpty(dateValue))
                return dateValue;

            try
            {
                DateTime result = new DateTime();
                if (DateTime.TryParseExact(dateValue, "dd/MM/yyyy HH:mm:ss", CultureInfo.CreateSpecificCulture("en-GB"), DateTimeStyles.None, out result))
                {
                    DateTime formatedDateTime = result;

                    //if the day part is 01, then update to the last day of the month
                    if (result.Day == 1)
                        formatedDateTime = new DateTime(result.Year, result.Month, DateTime.DaysInMonth(result.Year, result.Month));

                    dateValue = Format.FormatDateTime(formatedDateTime, DateTimeFormat.dd_Hyp_MM_Hyp_yyyy);
                }
            }
            catch
            {
            }

            return dateValue;
        }




        public static string FormatDateTimeCDB(object date, DateTimeFormatCDB type)
        {
            string typeStr = type.ToString();

            if (string.IsNullOrEmpty(typeStr))
                return null;
            else
            {
                typeStr = typeStr.Replace("_dash_", "-");
                typeStr = typeStr.Replace("_Hyp_", "-");
                typeStr = typeStr.Replace("_C_", ", ");
                typeStr = typeStr.Replace("_Col_", ":");
                typeStr = typeStr.Replace("__", " ");
                typeStr = typeStr.Replace("_", "/");
            }

            string dateString = String.Format("{0:" + typeStr + "}", date);
            return dateString;
        }

        public static DateTime GetMetaDataValueInMetaDataDateFormatCDB(string dateValue)
        {
            dateValue = dateValue.Trim();

            if (string.IsNullOrEmpty(dateValue))
                return GetDefaultDateCDB();

            try
            {
                // TODO: Fix Date time 
                DateTime result = new DateTime();
                if (DateTime.TryParseExact(dateValue, "dd-MM-yyyy", CultureInfo.CreateSpecificCulture("en-GB"), DateTimeStyles.None, out result))
                {
                    return result;
                }
                else
                {
                    return new DateTime(int.Parse(dateValue), 1, 1);
                }
            }
            catch
            {
                return GetDefaultDateCDB();
            }
        }

        //public static DateTime GetDatePartCDB(DateTime date)
        //{
        //    return DateTime.Parse(FormatDateTimeCDB(date, DateTimeFormatCDB.dd__MMM__yy));
        //}

        public static DateTime GetDefaultDateCDB()
        {
            return DateTime.Parse(Constants.WebServiceNullDate, new System.Globalization.CultureInfo("en-GB", false));
        }



    }
}
