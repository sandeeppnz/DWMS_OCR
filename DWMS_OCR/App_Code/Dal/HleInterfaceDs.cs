using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
//using System.Web.Security;
using DWMS_OCR.App_Code.Bll;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Dal
{
    public class HleInterfceDs
    {
        private static string GetConnectionString()
        {
            ConnectionStringSettings cts = Retrieve.GetConnectionString();
            return cts.ConnectionString;
        }

        #region Retrieve Methods

        /// <summary>
        /// 2012-08-31 : SP
        /// </summary>
        /// <param name="hleNumber"></param>
        /// <returns>dataset</returns>
        public static DataTable GetApplicantDetailsByRefNo(string hleNumber)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            //retrieve info
            sqlStatement.Append("SELECT Name, ApplicantType, OrderNo FROM HleInterface ");
            sqlStatement.Append("WHERE HleNumber= @HleNumber ORDER BY ApplicantType, OrderNo");

            command.Parameters.Add("@HleNumber", SqlDbType.NVarChar);
            command.Parameters["@HleNumber"].Value = hleNumber;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                command.CommandText = sqlStatement.ToString();
                command.Connection = connection;
                DataSet dataSet = new DataSet();
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                connection.Open();
                adapter.Fill(dataSet);
                return dataSet.Tables[0];
            }
        }

        public static string GetHleStatusByRefNo(string hleNumber)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            //retrieve info
            sqlStatement.Append("SELECT DISTINCT HleStatus FROM HleInterface ");
            sqlStatement.Append("WHERE HleNumber= @HleNumber");

            command.Parameters.Add("@HleNumber", SqlDbType.NVarChar);
            command.Parameters["@HleNumber"].Value = hleNumber;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                command.CommandText = sqlStatement.ToString();
                command.Connection = connection;
                DataSet dataSet = new DataSet();
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                connection.Open();
                adapter.Fill(dataSet);
                if (dataSet.Tables[0].Rows.Count > 0)
                    if (dataSet.Tables[0].Rows.Count > 1)
                        return "Mixed";
                    else
                        return dataSet.Tables[0].Rows[0][0].ToString();
                else
                    return string.Empty;
            }
        }

        #endregion


       
        /// <summary>
        /// Delete Wrong Records
        /// </summary>
        internal static void DeleteWrongRecords()
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            //delete based on date length.
            sqlStatement.Append("Delete FROM HleInterface WHERE LEN(HleDate) <10");

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                command.CommandText = sqlStatement.ToString();
                command.Connection = connection;
                DataSet dataSet = new DataSet();
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

       


      


    }
}
