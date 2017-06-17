using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Dal
{
    class DocAppDs
    {
        #region Retrieve Methods
        private static string GetConnectionString()
        {
            ConnectionStringSettings cts = Retrieve.GetConnectionString();
            return cts.ConnectionString;
        }




        public static DataTable GetDocAppsReadyToSendToCDB(string status)//, string exclusion)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();
            sqlStatement.Append(" SELECT DocApp.Id, DocApp.RefNo, DocApp.RefType, DocApp.CompletenessStaffUserId, DocApp.SendToCDBAttemptCount ");
            sqlStatement.Append(" FROM  DocApp ");
            sqlStatement.Append(" WHERE (DocApp.SendToCDBStatus=@status) AND (CompletenessStaffUserId <> '74b1336b-8342-4652-b1e3-bd60059ff14e' OR CompletenessStaffUserId IS NULL) ");
            //'74b1336b-8342-4652-b1e3-bd60059ff14e' = SYSTEM
            //sqlStatement.Append(" WHERE (DocApp.SendToCDBStatus=@status) AND (Status=@completenessStatus) AND (Profile.Name <>  @exclusion) ");

            command.Parameters.Add("@status", SqlDbType.VarChar);
            command.Parameters["@status"].Value = status;
            //command.Parameters.Add("@completenessStatus", SqlDbType.VarChar);
            //command.Parameters["@completenessStatus"].Value = completenessStatus;
            //command.Parameters.Add("@exclusion", SqlDbType.VarChar);
            //command.Parameters["@exclusion"].Value = exclusion;

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



        public static DataTable GetDocAppAndDocData(int docSetId, string status, string imageCondition, string docTypes, string sendToCDBStatus)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append(" SELECT DISTINCT sa.DocAppId, ap.Nric, ap.IdType, ap.Folder, ap.Name, d.Id as DocId, ap.CustomerSourceId, ap.CustomerType");
            sqlStatement.Append(" FROM docset ds "); 
            sqlStatement.Append(" JOIN setapp sa ON ds.id=sa.docsetid "); 
            sqlStatement.Append(" JOIN AppPersonal ap ON sa.DocAppid=ap.DocAppId ");
            sqlStatement.Append(" JOIN AppDocRef adr ON ap.id=adr.AppPersonalId ");
            sqlStatement.Append(" JOIN Doc d ON  adr.DocId=d.Id ");
            sqlStatement.Append(" WHERE d.status=@status and d.ImageCondition=@imageCondition and d.Doctypecode <>@docTypes and d.SendtoCDBStatus <>@sendToCDBStatus");
            sqlStatement.Append(" and d.DocSetId=@docSetId ORDER BY sa.docappid ");


            command.Parameters.Add("@docSetId", SqlDbType.Int);
            command.Parameters["@docSetId"].Value = docSetId;

            command.Parameters.Add("@status", SqlDbType.VarChar);
            command.Parameters["@status"].Value = status;

            command.Parameters.Add("@imageCondition", SqlDbType.VarChar);
            command.Parameters["@imageCondition"].Value = imageCondition;

            command.Parameters.Add("@docTypes", SqlDbType.VarChar);
            command.Parameters["@docTypes"].Value = docTypes;


            command.Parameters.Add("@sendToCDBStatus", SqlDbType.VarChar);
            command.Parameters["@sendToCDBStatus"].Value = sendToCDBStatus;

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


        #endregion


    }

}
