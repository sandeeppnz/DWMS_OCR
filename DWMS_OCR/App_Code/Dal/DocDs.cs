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
    class DocDs
    {
        #region Retrieve Methods
        private static string GetConnectionString()
        {
            ConnectionStringSettings cts = Retrieve.GetConnectionString();
            return cts.ConnectionString;
        }

        /// <summary>
        /// Get Doc folder
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string GetDocPersonalFolder(int id)
        {
            string result = string.Empty;

            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT DocPersonal.Folder FROM DocPersonal ");
            sqlStatement.Append("INNER JOIN SetDocRef ON DocPersonal.Id = SetDocRef.DocPersonalId ");
            sqlStatement.Append("WHERE SetDocRef.DocId=@docId");

            command.Parameters.Add("@docId", SqlDbType.Int);
            command.Parameters["@docId"].Value = id;

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
                    result = dataSet.Tables[0].Rows[0]["Folder"].ToString();
                }
            }

            return result;
        }

        /// <summary>
        /// Get Doc folder
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string GetAppPersonalFolder(int id)
        {
            string result = string.Empty;

            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT AppPersonal.Folder FROM AppPersonal ");
            sqlStatement.Append("INNER JOIN AppDocRef ON AppPersonal.Id = AppDocRef.AppPersonalId ");
            sqlStatement.Append("WHERE AppDocRef.DocId=@docId");

            command.Parameters.Add("@docId", SqlDbType.Int);
            command.Parameters["@docId"].Value = id;

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
                    result = dataSet.Tables[0].Rows[0]["Folder"].ToString();
                }
            }

            return result;
        }

        public static DataTable GetDistinctOrigSetIdForNullSetId()
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT DISTINCT OriginalSetId FROM Doc WHERE DocSetId IS NULL");

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


        public static DataTable GetDocDetails(int docId)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT DISTINCT DocType.DocumentId AS DocumentId, DocType.DocumentSubId AS DocIdSub, Doc.Description AS DocDescription, Doc.DocChannel, Doc.CustomerIdSubFromSource AS CustomerIdSubFromSource, DocType.Code AS DocTypeCode, Doc.ImageCondition, Doc.CmDocumentId, Doc.Id AS DocId, DocSet.VerificationDateIn, Doc.ImageAccepted ");
            sqlStatement.Append("FROM Doc INNER JOIN DocType ON Doc.DocTypeCode = DocType.Code INNER JOIN AppDocRef ON Doc.Id = AppDocRef.DocId INNER JOIN AppPersonal ON AppDocRef.AppPersonalId = AppPersonal.Id INNER JOIN DocSet ON Doc.DocSetId = DocSet.Id ");
            sqlStatement.Append("WHERE (Doc.Id = @DocId)" );

            command.Parameters.Add("@DocId", SqlDbType.Int);
            command.Parameters["@DocId"].Value = docId;

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




        public static DataTable GetCompletedDocDetails(int docAppId, string docStatus, string docStatus1, string imageCondition, string toAvoidDocType, string docSentToCDBStatus, string docsentToCDBAccept, string docSetSentToCDBStatus)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            //sqlStatement.Append(" SELECT DISTINCT d.status,d.SendtoCDBStatus, sa.DocAppId, d.DocSetId, ap.Nric, ap.IdType, ap.Folder, ap.Name, d.Id as DocId, ap.CustomerSourceId, ap.CustomerType ");
            //sqlStatement.Append(" FROM docset ds JOIN setapp sa ON ds.id=sa.docsetid JOIN AppPersonal ap ON sa.DocAppid=ap.DocAppId JOIN AppDocRef adr ON ap.id=adr.AppPersonalId JOIN Doc d ON  adr.DocId=d.Id ");
            //sqlStatement.Append(" WHERE d.status=@docStatus AND d.ImageCondition=@imageCondition AND d.Doctypecode <>@toAvoidDocType AND d.SendtoCDBStatus <>@toAvoidSentToCDBStatus AND d.DocSetId=@docSetId ");
            //sqlStatement.Append(" ORDER By ap.Nric ");


            //sqlStatement.Append(" SELECT DISTINCT d.Status, d.SendToCDBStatus, sa.DocAppId, ap.Nric, ap.IdType, ap.Folder, ap.Name, d.Id AS DocId, ap.CustomerSourceId, ap.CustomerType ");
            //sqlStatement.Append(" FROM         DocSet AS ds INNER JOIN SetApp AS sa ON ds.Id = sa.DocSetId INNER JOIN AppPersonal AS ap ON sa.DocAppId = ap.DocAppId INNER JOIN AppDocRef AS adr ON ap.Id = adr.AppPersonalId INNER JOIN Doc AS d ON adr.DocId = d.Id ");
            //sqlStatement.Append(" WHERE d.status=@docStatus AND d.ImageCondition=@imageCondition AND d.Doctypecode <>@toAvoidDocType AND d.SendtoCDBStatus <>@toAvoidSentToCDBStatus AND sa.DocAppId=@docAppId ");
            //sqlStatement.Append(" ORDER By ap.Nric ");

            //Process 2 (Modified Verified Docs) is using this
            sqlStatement.Append(" SELECT DISTINCT d.Status, d.SendToCDBStatus, sa.DocAppId, ap.Nric, ap.IdType, ap.Folder, ap.Name, d.Id AS DocId, ap.CustomerSourceId, ap.CustomerType, d.DocSetId ");
            sqlStatement.Append(" FROM   DocSet AS ds INNER JOIN SetApp AS sa ON ds.Id = sa.DocSetId INNER JOIN AppPersonal AS ap ON sa.DocAppId = ap.DocAppId INNER JOIN AppDocRef AS adr ON ap.Id = adr.AppPersonalId INNER JOIN Doc AS d ON adr.DocId = d.Id ");
            sqlStatement.Append(" WHERE  ((d.Status=@docStatus) OR (d.Status=@docStatus1)) AND (d.ImageCondition=@imageCondition) AND (d.DocTypeCode<>@toAvoidDocType) AND (d.SendToCDBStatus=@DocSentToCDBStatus) AND (d.SendToCDBAccept <> @DocSentToCDBAccept) AND (sa.DocAppId=@DocAppId) AND (ds.SendToCDBStatus=@DocSetSendToCDBStatus) ");
            sqlStatement.Append(" ORDER By ap.Nric ");




            //command.Parameters.Add("@docSetId", SqlDbType.Int);
            //command.Parameters["@docSetId"].Value = docSetId;

            command.Parameters.Add("@DocAppId", SqlDbType.Int);
            command.Parameters["@DocAppId"].Value = docAppId;


            command.Parameters.Add("@DocStatus", SqlDbType.VarChar);
            command.Parameters["@DocStatus"].Value = docStatus;

            command.Parameters.Add("@DocStatus1", SqlDbType.VarChar);
            command.Parameters["@DocStatus1"].Value = docStatus1;

            command.Parameters.Add("@imageCondition", SqlDbType.VarChar);
            command.Parameters["@imageCondition"].Value = imageCondition;

            command.Parameters.Add("@toAvoidDocType", SqlDbType.VarChar);
            command.Parameters["@toAvoidDocType"].Value = toAvoidDocType;

            command.Parameters.Add("@DocSentToCDBStatus", SqlDbType.VarChar);
            command.Parameters["@DocSentToCDBStatus"].Value = docSentToCDBStatus;

            command.Parameters.Add("@DocSentToCDBAccept", SqlDbType.VarChar);
            command.Parameters["@DocSentToCDBAccept"].Value = docsentToCDBAccept;

            command.Parameters.Add("@DocSetSendToCDBStatus", SqlDbType.VarChar);
            command.Parameters["@DocSetSendToCDBStatus"].Value = docSetSentToCDBStatus;



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


        public static DataTable GetDocsNotSentToCDB(int docSetId, string toAvoidSentToCDBStatus)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append(" SELECT DocSetId, SendToCDBStatus, Id ");
            sqlStatement.Append(" FROM   Doc");
            sqlStatement.Append(" WHERE  (SendToCDBStatus <> @toAvoidSentToCDBStatus) AND (DocSetId = @docSetId) ");

            command.Parameters.Add("@docSetId", SqlDbType.Int);
            command.Parameters["@docSetId"].Value = docSetId;

            command.Parameters.Add("@toAvoidSentToCDBStatus", SqlDbType.VarChar);
            command.Parameters["@toAvoidSentToCDBStatus"].Value = toAvoidSentToCDBStatus;

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



        public static DataTable GetCompletedDocDetails(int docAppId, string docStatus, string docStatus1, string imageCondition, string toAvoidDocType, string sentToCDBStatus, string sentToCDBAccept)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            //Process 3
            //sqlStatement.Append(" SELECT DISTINCT d.Status, d.SendToCDBStatus, sa.DocAppId, ap.Nric, ap.IdType, ap.Folder, ap.Name, d.Id AS DocId, ap.CustomerSourceId, ap.CustomerType, d.CmDocumentId ");
            //sqlStatement.Append(" FROM   DocSet AS ds INNER JOIN SetApp AS sa ON ds.Id = sa.DocSetId INNER JOIN AppPersonal AS ap ON sa.DocAppId = ap.DocAppId INNER JOIN AppDocRef AS adr ON ap.Id = adr.AppPersonalId INNER JOIN Doc AS d ON adr.DocId = d.Id ");
            //sqlStatement.Append(" WHERE  (d.Status=@docStatus) AND (d.ImageCondition =@imageCondition) AND (d.DocTypeCode <>@toAvoidDocType) AND (d.SendToCDBStatus=@sentToCDBStatus) AND ((d.CmDocumentId IS NOT NULL) OR (d.CmDocumentId ='')) AND sa.DocAppId=@docAppId ");
            //sqlStatement.Append(" ORDER By ap.Nric ");

            sqlStatement.Append(" SELECT DISTINCT d.Status, d.SendToCDBStatus, d.SendToCDBAccept, sa.DocAppId, ap.Nric, ap.IdType, ap.Folder, ap.Name, d.Id AS DocId, ap.CustomerSourceId, ap.CustomerType, d.CmDocumentId, ap.PersonalType, ap.OrderNo, d.DocTypeCode ");
            sqlStatement.Append(" FROM   DocSet AS ds INNER JOIN SetApp AS sa ON ds.Id = sa.DocSetId INNER JOIN AppPersonal AS ap ON sa.DocAppId = ap.DocAppId INNER JOIN AppDocRef AS adr ON ap.Id = adr.AppPersonalId INNER JOIN Doc AS d ON adr.DocId = d.Id ");
            sqlStatement.Append(" WHERE  ((d.Status=@docStatus) OR (d.Status=@docStatus1)) AND (d.ImageCondition =@imageCondition) AND (d.DocTypeCode <>@toAvoidDocType) AND (d.SendToCDBStatus=@sentToCDBStatus) AND (d.SendToCDBAccept <> @sentToCDBAccept) AND (sa.DocAppId=@docAppId) AND (d.IsVerified<>1 OR d.IsVerified IS NULL) ");
            sqlStatement.Append(" ORDER By ap.Nric ");



            //command.Parameters.Add("@docSetId", SqlDbType.Int);
            //command.Parameters["@docSetId"].Value = docSetId;

            command.Parameters.Add("@docAppId", SqlDbType.Int);
            command.Parameters["@docAppId"].Value = docAppId;

            command.Parameters.Add("@docStatus", SqlDbType.VarChar);
            command.Parameters["@docStatus"].Value = docStatus;

            command.Parameters.Add("@docStatus1", SqlDbType.VarChar);
            command.Parameters["@docStatus1"].Value = docStatus1;

            command.Parameters.Add("@imageCondition", SqlDbType.VarChar);
            command.Parameters["@imageCondition"].Value = imageCondition;

            command.Parameters.Add("@toAvoidDocType", SqlDbType.VarChar);
            command.Parameters["@toAvoidDocType"].Value = toAvoidDocType;

            command.Parameters.Add("@sentToCDBStatus", SqlDbType.VarChar);
            command.Parameters["@sentToCDBStatus"].Value = sentToCDBStatus;

            command.Parameters.Add("@sentToCDBAccept", SqlDbType.VarChar);
            command.Parameters["@sentToCDBAccept"].Value = sentToCDBAccept;

           
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


        public static DataTable GetMetaDataDetails(int docId, string docTypeCode)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT  MetaData.FieldValue, MetaData.FieldName, DocType.Code AS DocTypeCode ");
            sqlStatement.Append("FROM    MetaData INNER JOIN Doc ON MetaData.Doc = Doc.Id INNER JOIN DocType ON Doc.DocTypeCode = DocType.Code ");
            sqlStatement.Append("WHERE   (MetaData.Doc = @DocId) AND (DocType.Code = @DocTypeCode) ");

            command.Parameters.Add("@DocId", SqlDbType.Int);
            command.Parameters["@DocId"].Value = docId;


            command.Parameters.Add("@DocTypeCode", SqlDbType.VarChar);
            command.Parameters["@DocTypeCode"].Value = docTypeCode;



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
