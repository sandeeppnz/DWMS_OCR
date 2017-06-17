using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Dal
{
    class RawPageDs
    {
        private static string GetConnectionString()
        {
            ConnectionStringSettings cts = Retrieve.GetConnectionString();
            return cts.ConnectionString;
        }

        /// <summary>
        /// Get RawPage and respective docApp Details
        /// </summary>
        /// <param name="docSetId"></param>
        /// <returns></returns>
        public static int CountOcrPagesBySet(int docSetId)
        {
            int pageCount = 0;
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT COUNT(*) AS PageCount ");
            sqlStatement.Append("FROM RawPage INNER JOIN RawFile ON RawPage.RawFileId = RawFile.Id ");
            sqlStatement.Append("WHERE RawFile.DocSetId = @docSetId");

            command.Parameters.Add("@docSetId", SqlDbType.Int);
            command.Parameters["@docSetId"].Value = docSetId;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                command.CommandText = sqlStatement.ToString();
                command.Connection = connection;
                DataSet dataSet = new DataSet();
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                connection.Open();
                adapter.Fill(dataSet);

                if (dataSet.Tables[0].Rows.Count > 0)
                {
                    pageCount = int.Parse(dataSet.Tables[0].Rows[0]["PageCount"].ToString());
                }
            }

            return pageCount;
        }
    }
}
