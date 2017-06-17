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
    class DocSetDs
    {
        #region Retrieve Methods
        private static string GetConnectionString()
        {
            ConnectionStringSettings cts = Retrieve.GetConnectionString();
            return cts.ConnectionString;
        }

        /// <summary>
        /// Get Doc App Id of latest set for NRIC
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static DataTable GetLatestSetForNric(string nric)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT TOP 1 DocSet.* FROM DocSet WHERE DocAppId IN ");
            sqlStatement.Append("(SELECT Id FROM DocApp WHERE RefNo IN (SELECT HleNumber FROM HleInterface WHERE Nric=@nric)) ");
            sqlStatement.Append("ORDER BY VerificationDateIn DESC");

            command.Parameters.Add("@nric", SqlDbType.VarChar);
            command.Parameters["@nric"].Value = nric;

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



        public static DataTable GetDocSetsByDocAppId(int docAppId)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append(" SELECT DISTINCT DocSet.Id AS DocSetId ");
            sqlStatement.Append(" FROM   DocApp INNER JOIN SetApp ON DocApp.Id = SetApp.DocAppId INNER JOIN AppPersonal ON DocApp.Id = AppPersonal.DocAppId INNER JOIN AppDocRef ON AppPersonal.Id = AppDocRef.AppPersonalId INNER JOIN Doc ON AppDocRef.DocId = Doc.Id INNER JOIN DocSet ON SetApp.DocSetId = DocSet.Id AND Doc.DocSetId = DocSet.Id ");
            sqlStatement.Append(" WHERE  (DocApp.Id= @DocAppId) ");

            command.Parameters.Add("@DocAppId", SqlDbType.Int);
            command.Parameters["@DocAppId"].Value = docAppId;

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

        public static DataTable GetVerifiedReadyDocSets(string status, string docSetStatus)//, string exclusion)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();
            sqlStatement.Append(" SELECT AcknowledgeNumber, Block, Channel, ConvertedToSampleDoc, DateAssigned, DepartmentId, ExpediteReason, ExpediteRemark, ExpediteRequestDate, ");
            sqlStatement.Append(" ExpediteRequester, Floor, DocSet.Id, ImportedBy, ImportedOn, IsBeingProcessed, IsUrgent, ProcessingEndDate, ProcessingStartDate, ReadyForOcr, ");
            sqlStatement.Append(" Remark, SectionId, SendToCDBAttemptCount, SendToCDBStatus, SetNo, SkipCategorization, Status, StreetId, Unit, VerificationDateIn, VerificationDateOut, ");
            sqlStatement.Append(" VerificationStaffUserId, WebServXmlContent ");
            sqlStatement.Append(" FROM DocSet");
            sqlStatement.Append(" WHERE (SendToCDBStatus = @status) AND (Status = @docSetStatus)");
            sqlStatement.Append(" AND (VerificationStaffUserId <> '74b1336b-8342-4652-b1e3-bd60059ff14e')");
            //'74b1336b-8342-4652-b1e3-bd60059ff14e' = SYSTEM

            command.Parameters.Add("@status", SqlDbType.VarChar);
            command.Parameters["@status"].Value = status;

            command.Parameters.Add("@docSetStatus", SqlDbType.VarChar);
            command.Parameters["@docSetStatus"].Value = docSetStatus;

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



        public static DataTable GetDocAppAndDocData(int docSetId, int IsVerified, string status, string status1, string imageCondition, string docTypes, string sendToCDBStatus)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            //sqlStatement.Append(" SELECT DISTINCT sa.docappid as DocAppId ");
            sqlStatement.Append(" SELECT DISTINCT sa.DocAppId, ap.Nric, ap.IdType, ap.Folder, ap.Name, d.Id as DocId, ap.CustomerSourceId, ap.CustomerType, ap.PersonalType, ap.OrderNo, d.DocTypeCode");
            sqlStatement.Append(" FROM docset ds "); 
            sqlStatement.Append(" JOIN setapp sa ON ds.id=sa.docsetid "); 
            sqlStatement.Append(" JOIN AppPersonal ap ON sa.DocAppid=ap.DocAppId ");
            sqlStatement.Append(" JOIN AppDocRef adr ON ap.id=adr.AppPersonalId ");
            sqlStatement.Append(" JOIN Doc d ON  adr.DocId=d.Id ");
            sqlStatement.Append(" WHERE (d.status=@status OR d.status=@status1) and d.ImageCondition=@imageCondition and d.Doctypecode <>@docTypes and d.SendtoCDBStatus <>@sendToCDBStatus");
            sqlStatement.Append(" and d.DocSetId=@docSetId and (d.IsVerified=@IsVerified OR d.IsVerified IS NULL) ORDER BY sa.docappid ");


            command.Parameters.Add("@docSetId", SqlDbType.Int);
            command.Parameters["@docSetId"].Value = docSetId;

            command.Parameters.Add("@status", SqlDbType.VarChar);
            command.Parameters["@status"].Value = status;
            command.Parameters.Add("@status1", SqlDbType.VarChar);
            command.Parameters["@status1"].Value = status1;

            command.Parameters.Add("@imageCondition", SqlDbType.VarChar);
            command.Parameters["@imageCondition"].Value = imageCondition;

            command.Parameters.Add("@docTypes", SqlDbType.VarChar);
            command.Parameters["@docTypes"].Value = docTypes;

            command.Parameters.Add("@IsVerified", SqlDbType.VarChar);
            command.Parameters["@IsVerified"].Value = IsVerified;

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



        public static DataTable GetDocSetData(int docAppId, string status, string sendToCDBStatus)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();
            sqlStatement.Append(" SELECT DISTINCT DocApp.Status, DocApp.Id, DocApp.RefNo, DocApp.RefType, DocApp.CompletenessStaffUserId, DocApp.SendToCDBStatus, DocApp.SendToCDBAttemptCount, DocSet.Id AS DocSetId ");
            sqlStatement.Append(" FROM   DocApp INNER JOIN SetApp ON DocApp.Id = SetApp.DocAppId INNER JOIN DocSet ON SetApp.DocSetId = DocSet.Id ");
            sqlStatement.Append(" WHERE  (DocApp.Id = @docAppId) AND (DocApp.Status=@status) AND (DocApp.SendToCDBStatus=@sendToCDBStatus) ");

            command.Parameters.Add("@docAppId", SqlDbType.Int);
            command.Parameters["@docAppId"].Value = docAppId;

            command.Parameters.Add("@status", SqlDbType.VarChar);
            command.Parameters["@status"].Value = status;

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
