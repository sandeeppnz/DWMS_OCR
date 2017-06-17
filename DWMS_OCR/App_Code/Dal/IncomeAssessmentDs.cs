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
    class IncomeAssessmentDs
    {
        private static string GetConnectionString()
        {
            ConnectionStringSettings cts = Retrieve.GetConnectionString();
            return cts.ConnectionString;
        }

        public static DataTable GetDocAppByStatus(string status, string LeasStatus)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();
            sqlStatement.Append(" SELECT a.Id AS DocAppId, a.RefNo, a.AssessmentStaffUserId, ISNULL(a.SentToLeasAttemptCount,0) AS SentToLeasAttemptCount FROM DocApp a ");
            sqlStatement.Append(" WHERE a.AssessmentStatus = @status AND a.SentToLEASStatus = @SentToLEASStatus");            

            command.Parameters.Add("@status", SqlDbType.VarChar);
            command.Parameters["@status"].Value = status;

            command.Parameters.Add("@SentToLEASStatus", SqlDbType.VarChar);
            command.Parameters["@SentToLEASStatus"].Value = LeasStatus;

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

        public static DataTable GetAppPersonalByDocAppId(int docAppId)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();
            sqlStatement.Append(" SELECT a.Id, a.Nric, a.Name, a.DocAppId, a.MonthsToLeas FROM AppPersonal a  ");
            sqlStatement.Append(" WHERE a.DocAppId = @DocAppId AND (LTRIM(RTRIM(a.Nric)) <> '' AND a.Nric IS NOT NULL) ");
            sqlStatement.Append(" AND LTRIM(RTRIM(UPPER(a.Folder))) <> 'OTHERS'  ");  

            command.Parameters.Add("@DocAppId", SqlDbType.VarChar);
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

        public static DataTable GetDataForIncomeAssessment(int docAppId, int appPersonalId)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT DISTINCT   ");
            sqlStatement.Append("Income.Id , ");
            sqlStatement.Append("DateName( month , DateAdd( month , Income.IncomeMonth , 0 ) - 1 ) + ' ' + CAST(Income.IncomeYear AS VARCHAR)  AS MonthYear,  ");
            sqlStatement.Append("Income.IncomeMonth, ");
            sqlStatement.Append("Income.IncomeYear, SUM(IncomeAmount) AS IncomeAmount ");
            sqlStatement.Append("FROM Income ");
            sqlStatement.Append("LEFT JOIN IncomeVersion ON Income.Id = IncomeVersion.IncomeId ");
            sqlStatement.Append("LEFT JOIN IncomeDetails ON IncomeVersion.Id = IncomeDetails.IncomeVersionID ");
            sqlStatement.Append("INNER JOIN AppPersonal ON Income.AppPersonalId = AppPersonal.Id ");
            sqlStatement.Append("WHERE AppPersonal.DocAppId = @docAppId AND AppPersonal.Id = @AppPersonalId");
            sqlStatement.Append(" GROUP BY Income.IncomeMonth, Income.IncomeYear, Income.Id ");
            sqlStatement.Append(" ORDER BY Income.IncomeYear ASC, Income.IncomeMonth ASC");

            command.Parameters.Add("@docAppId", SqlDbType.Int);
            command.Parameters["@docAppId"].Value = docAppId;

            command.Parameters.Add("@AppPersonalId", SqlDbType.Int);
            command.Parameters["@AppPersonalId"].Value = appPersonalId;

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



        public static DataTable GetDataForIncomeAssessment(int docAppId, string nric)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT DISTINCT   ");
            sqlStatement.Append("Income.Id , ");
            sqlStatement.Append("DateName( month , DateAdd( month , Income.IncomeMonth , 0 ) - 1 ) + ' ' + CAST(Income.IncomeYear AS VARCHAR)  AS MonthYear,  ");
            sqlStatement.Append("Income.IncomeMonth, ");
            sqlStatement.Append("Income.IncomeYear, ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND Allowance = 1) AS 'Allowance', ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND CPFIncome = 1) AS 'CPFIncome', ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND GrossIncome = 1) AS 'GrossIncome', ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND AHGIncome = 1) AS 'AHGIncome', ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND Overtime = 1) AS 'Overtime', ");
            sqlStatement.Append("Income.Currency, Income.CurrencyRate, AppPersonal.Nric, AppPersonal.DocAppId, Income.IncomeVersionId, ISNULL(AppPersonal.MonthsToLEAS, 0) AS MonthsToLEAS ");
            sqlStatement.Append("FROM Income ");
            sqlStatement.Append("LEFT JOIN IncomeVersion ON Income.Id = IncomeVersion.IncomeId ");
            sqlStatement.Append("LEFT JOIN IncomeDetails ON IncomeVersion.Id = IncomeDetails.IncomeVersionID ");
            sqlStatement.Append("LEFT JOIN AppPersonal ON Income.AppPersonalId = AppPersonal.Id ");
            sqlStatement.Append("LEFT JOIN AppDocRef ON AppDocRef.AppPersonalId = AppPersonal.Id  ");
            sqlStatement.Append("LEFT JOIN Doc ON AppDocRef.DocId = Doc.Id ");
            sqlStatement.Append("LEFT JOIN DocType ON Doc.DocTypeCode = Doctype.Code ");
            sqlStatement.Append("WHERE AppPersonal.DocAppId = @docAppId AND LTRIM(RTRIM(AppPersonal.Nric)) = LTRIM(RTRIM(@Nric))");
            //Added 22.10.2013
            sqlStatement.Append(" ORDER BY Income.IncomeYear DESC, Income.IncomeMonth DESC ");

            command.Parameters.Add("@docAppId", SqlDbType.Int);
            command.Parameters["@docAppId"].Value = docAppId;

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
        #region Added By Edward 10/3/2014
        /// <summary>
        /// Gets the Income Amount
        /// </summary>
        /// <param name="appPersonalId">AppPersonalId</param>
        /// <returns></returns>
        public static DataTable GetDataForIncomeAssessment(int appPersonalId)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT DISTINCT   ");
            sqlStatement.Append("Income.Id , ");
            sqlStatement.Append("DateName( month , DateAdd( month , Income.IncomeMonth , 0 ) - 1 ) + ' ' + CAST(Income.IncomeYear AS VARCHAR)  AS MonthYear,  ");
            sqlStatement.Append("Income.IncomeMonth, ");
            sqlStatement.Append("Income.IncomeYear, ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND Allowance = 1) AS 'Allowance', ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND CPFIncome = 1) AS 'CPFIncome', ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND GrossIncome = 1) AS 'GrossIncome', ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND AHGIncome = 1) AS 'AHGIncome', ");
            sqlStatement.Append("(SELECT SUM(IncomeAmount) FROM IncomeDetails WHERE IncomeDetails.IncomeVersionId = Income.IncomeVersionId AND Overtime = 1) AS 'Overtime', ");
            sqlStatement.Append("Income.Currency, Income.CurrencyRate, AppPersonal.Nric, AppPersonal.DocAppId, Income.IncomeVersionId, ISNULL(AppPersonal.MonthsToLEAS, 0) AS MonthsToLEAS ");
            sqlStatement.Append("FROM Income ");
            sqlStatement.Append("LEFT JOIN IncomeVersion ON Income.Id = IncomeVersion.IncomeId ");
            sqlStatement.Append("LEFT JOIN IncomeDetails ON IncomeVersion.Id = IncomeDetails.IncomeVersionID ");
            sqlStatement.Append("LEFT JOIN AppPersonal ON Income.AppPersonalId = AppPersonal.Id ");
            sqlStatement.Append("LEFT JOIN AppDocRef ON AppDocRef.AppPersonalId = AppPersonal.Id  ");
            sqlStatement.Append("LEFT JOIN Doc ON AppDocRef.DocId = Doc.Id ");
            sqlStatement.Append("LEFT JOIN DocType ON Doc.DocTypeCode = Doctype.Code ");
            sqlStatement.Append("WHERE AppPersonal.Id = @AppPersonalId ");
            //Added 22.10.2013
            sqlStatement.Append(" ORDER BY Income.IncomeYear DESC, Income.IncomeMonth DESC ");

            command.Parameters.Add("@AppPersonalId", SqlDbType.Int);
            command.Parameters["@AppPersonalId"].Value = appPersonalId;

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


        public static DataTable GetIncomeAmount(int docAppId, int appPersonalId, string TypeOfIncome)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            #region Commented
            //if (TypeOfIncome != "CA")
            //{
            //    sqlStatement.Append(" SELECT ISNULL(SUM(IncomeDetails.IncomeAmount),0) AS TotalAmount, ISNULL(SUM(IncomeDetails.IncomeAmount) /  ");
            //    sqlStatement.Append(" (SELECT COUNT( c.IncomeMonth) FROM Income c INNER JOIN AppPersonal d ON c.AppPersonalId = d.Id WHERE c.AppPersonalId = @AppPersonalId AND d.DocAppId = @DocAppId),0) AS AvgAmount   ");
            //    sqlStatement.Append(" FROM AppPersonal a INNER JOIN Income b ON a.id = b.AppPersonalId INNER JOIN IncomeVersion ON b.Id = IncomeVersion.IncomeId AND b.IncomeVersionId = IncomeVersion.Id ");
            //    sqlStatement.Append(" INNER JOIN IncomeDetails ON IncomeVersion.Id = IncomeDetails.IncomeVersionID WHERE a.DocAppId = @DocAppId AND a.Id = @AppPersonalId ");
            //    if (TypeOfIncome.ToUpper() == "GROSS")
            //        sqlStatement.Append(" AND IncomeDetails.GrossIncome = 1 ");
            //    else if (TypeOfIncome.ToUpper() == "ALLOWANCE")
            //        sqlStatement.Append(" AND IncomeDetails.Allowance = 1 ");
            //    else if (TypeOfIncome.ToUpper() == "OVERTIME")
            //        sqlStatement.Append(" AND IncomeDetails.Overtime = 1 ");
            //    else
            //        sqlStatement.Append(" AND IncomeDetails.GrossIncome = 1 ");
            //}
            //else
            //{
            //    sqlStatement.Append(" select ISNULL(Sum(CreditAssessmentAmount),0) AS TotalAmount from creditassessment a  ");
            //    sqlStatement.Append(" inner join apppersonal b on a.AppPersonalId = b.Id ");
            //    sqlStatement.Append(" inner join DocApp c on b.DocAppId = c.Id ");
            //    sqlStatement.Append(" inner join ( ");
            //    sqlStatement.Append("SELECT   DISTINCT IncomeItem, CASE WHEN Allowance = 1 THEN 'Allowance' WHEN GrossIncome = 1 THEN 'Gross Income' ");
            //    sqlStatement.Append("WHEN Overtime = 1 THEN 'Overtime' END AS IncomeType  ");
            //    sqlStatement.Append("FROM IncomeDetails a INNER JOIN IncomeVersion b ON a.IncomeVersionID = b.Id  ");
            //    sqlStatement.Append("INNER JOIN Income c ON c.IncomeVersionId = b.Id WHERE  c.AppPersonalId = @AppPersonalId ");
            //    sqlStatement.Append(" ) d ON a.IncomeItem = d.IncomeItem AND a.IncomeType = d.IncomeType");
            //    sqlStatement.Append(" where a.AppPersonalId = @AppPersonalId AND b.DocAppId = @DocAppId ");
            //}
            #endregion

            if (TypeOfIncome != "CA")
            {
                sqlStatement.Append("SELECT ISNULL(SUM(CreditAssessmentAmount),0) AS TotalAmount  ");
                sqlStatement.Append("FROM  CreditAssessment a  ");
                sqlStatement.Append("INNER JOIN AppPersonal b ON a.AppPersonalId = b.Id  ");
                sqlStatement.Append("INNER JOIN DocApp c ON b.DocAppId = c.Id ");
                sqlStatement.Append("WHERE a.AppPersonalId = @AppPersonalId AND b.DocAppId = @DocAppId ");
                if (TypeOfIncome.ToUpper() == "ALLOWANCE")
                    sqlStatement.Append(" AND a.IncomeType = 'Allowance' ");
                else if (TypeOfIncome.ToUpper() == "OVERTIME")
                    sqlStatement.Append(" AND a.IncomeType = 'Overtime' ");
            }
            else
            {
                sqlStatement.Append(" select ISNULL(Sum(CreditAssessmentAmount),0) AS TotalAmount from creditassessment a  ");
                sqlStatement.Append(" inner join apppersonal b on a.AppPersonalId = b.Id ");
                sqlStatement.Append(" inner join DocApp c on b.DocAppId = c.Id ");
                sqlStatement.Append(" inner join ( ");
                sqlStatement.Append("SELECT   DISTINCT IncomeItem, CASE WHEN Allowance = 1 THEN 'Allowance' WHEN GrossIncome = 1 THEN 'Gross Income' ");
                sqlStatement.Append("WHEN Overtime = 1 THEN 'Overtime' END AS IncomeType  ");
                sqlStatement.Append("FROM IncomeDetails a INNER JOIN IncomeVersion b ON a.IncomeVersionID = b.Id  ");
                sqlStatement.Append("INNER JOIN Income c ON c.IncomeVersionId = b.Id WHERE  c.AppPersonalId = @AppPersonalId ");
                sqlStatement.Append(" ) d ON a.IncomeItem = d.IncomeItem AND a.IncomeType = d.IncomeType");
                sqlStatement.Append(" where a.AppPersonalId = @AppPersonalId AND b.DocAppId = @DocAppId ");
            }

            command.Parameters.Add("@DocAppId", SqlDbType.Int);
            command.Parameters["@DocAppId"].Value = docAppId;
            command.Parameters.Add("@AppPersonalId", SqlDbType.Int);
            command.Parameters["@AppPersonalId"].Value = appPersonalId;

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

        public static DataTable GetDescendingMonthYear(int docAppId, string nric)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT DISTINCT   ");
            sqlStatement.Append("Income.Id , ");
            sqlStatement.Append("DateName( month , DateAdd( month , Income.IncomeMonth , 0 ) - 1 ) + ' ' + CAST(Income.IncomeYear AS VARCHAR)  AS MonthYear,  ");
            sqlStatement.Append("Income.IncomeMonth, ");
            sqlStatement.Append("Income.IncomeYear ");
            sqlStatement.Append("FROM Income ");
            sqlStatement.Append("LEFT JOIN IncomeVersion ON Income.Id = IncomeVersion.IncomeId ");
            sqlStatement.Append("LEFT JOIN IncomeDetails ON IncomeVersion.Id = IncomeDetails.IncomeVersionID ");
            sqlStatement.Append("INNER JOIN AppPersonal ON Income.AppPersonalId = AppPersonal.Id ");
            sqlStatement.Append("INNER JOIN AppDocRef ON AppDocRef.AppPersonalId = AppPersonal.Id  ");
            sqlStatement.Append("INNER JOIN Doc ON AppDocRef.DocId = Doc.Id ");
            sqlStatement.Append("INNER JOIN DocType ON Doc.DocTypeCode = Doctype.Code ");
            sqlStatement.Append("WHERE AppPersonal.DocAppId = @docAppId AND LTRIM(RTRIM(AppPersonal.Nric)) = LTRIM(RTRIM(@Nric)) ORDER BY Income.IncomeYear DESC, Income.IncomeMonth DESC");

            command.Parameters.Add("@docAppId", SqlDbType.Int);
            command.Parameters["@docAppId"].Value = docAppId;

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

        public static DataTable GetIncomeDetailsByIncomeIdAndIncomeItem(int IncomeId, string IncomeItem)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT  a.Id AS IncomeDetailsId, b.Id AS IncomeVersionId, c.Id AS IncomeId, a.IncomeItem, a.IncomeAmount, ");
            sqlStatement.Append("CASE WHEN Allowance = 1 THEN 'Allowance' WHEN GrossIncome = 1 THEN 'Gross Income' WHEN Overtime = 1 THEN 'Overtime' ");
            sqlStatement.Append("ELSE ' - ' END AS IncomeType , c.CurrencyRate   ");
            sqlStatement.Append("FROM IncomeDetails a INNER JOIN IncomeVersion b ON a.IncomeVersionID = b.Id ");
            sqlStatement.Append("INNER JOIN Income c ON c.IncomeVersionId = b.Id WHERE c.Id = @IncomeId and IncomeItem = @IncomeItem ");
            sqlStatement.Append("AND (Allowance = 1 OR GrossIncome = 1 OR Overtime = 1) ");

            command.Parameters.Add("@IncomeId", SqlDbType.Int);
            command.Parameters["@IncomeId"].Value = IncomeId;

            command.Parameters.Add("@IncomeItem", SqlDbType.VarChar);
            command.Parameters["@IncomeItem"].Value = IncomeItem;

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

        public static DataTable GetDistinctIncomeItemByAppPersonalId(int id)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT   DISTINCT IncomeItem, CASE WHEN Allowance = 1 THEN 'Allowance' WHEN GrossIncome = 1 THEN 'Gross Income' ");
            sqlStatement.Append("WHEN Overtime = 1 THEN 'Overtime' END AS IncomeType  ");
            sqlStatement.Append("FROM IncomeDetails a INNER JOIN IncomeVersion b ON a.IncomeVersionID = b.Id  ");
            sqlStatement.Append("INNER JOIN Income c ON c.IncomeVersionId = b.Id WHERE  c.AppPersonalId = @AppPersonalId ORDER BY IncomeItem");

            command.Parameters.Add("@AppPersonalId", SqlDbType.Int);
            command.Parameters["@AppPersonalId"].Value = id;

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


        public static string GetUserNameByAssessmentStaffId(Guid userId)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT DISTINCT aspnet_Users.UserName FROM DocApp INNER JOIN ");
            sqlStatement.Append("aspnet_Users ON DocApp.AssessmentStaffUserId = aspnet_Users.UserId  ");
            sqlStatement.Append("WHERE        (DocApp.AssessmentStaffUserId = @UserId)  ");

            command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier);
            command.Parameters["@UserId"].Value = userId;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                command.CommandText = sqlStatement.ToString();
                command.Connection = connection;
                DataSet dataSet = new DataSet();
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                connection.Open();
                adapter.Fill(dataSet);
                return dataSet.Tables[0].Rows[0][0].ToString();
            }
               
        }
    }
}
