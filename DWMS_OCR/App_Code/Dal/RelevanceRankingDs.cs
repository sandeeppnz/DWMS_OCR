using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DWMS_OCR.App_Code.Bll;
using System.IO;
using DWMS_OCR.App_Code.Helper;
using System.Data.SqlClient;
using System.Configuration;

namespace DWMS_OCR.App_Code.Dal
{
    class RelevanceRankingDs
    {
        /// <summary>
        /// Get the connection string.
        /// </summary>
        /// <returns>Connection string</returns>
        private static string GetConnectionString()
        {
            ConnectionStringSettings cts = Retrieve.GetConnectionString();
            return cts.ConnectionString;
        }

        /// <summary>
        /// Get the sample documents ordered by rank.
        /// </summary>
        /// <param name="docTypeCode">document type code</param>
        /// <returns>Sample document table</returns>
        public static DataTable GetAllSampleDocWithRanks(string docTypeCode)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            // Rank is computed by dividing the No of matches in Relevance Ranking  with the difference between 
            // the DateIn of the SampleDoc and the current date
            sqlStatement.Append("SELECT Id, ((MatchCount+1)/(Diff+1)) AS Rank FROM ");
            sqlStatement.Append("(SELECT Id, ABS(DATEDIFF(day, GETDATE(), DateIn)) AS Diff, ");
            sqlStatement.Append("(SELECT count(RelevanceRanking.Id) FROM RelevanceRanking WHERE SampleDocId = SampleDoc.Id AND IsMatch = 1) AS MatchCount ");
            sqlStatement.Append("FROM SampleDoc WHERE DocTypeCode = @docTypeCode) A ");
            sqlStatement.Append("ORDER BY Rank DESC, MatchCount DESC ");

            command.Parameters.Add("@docTypeCode", SqlDbType.VarChar);
            command.Parameters["@docTypeCode"].Value = docTypeCode;

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
    }
}
