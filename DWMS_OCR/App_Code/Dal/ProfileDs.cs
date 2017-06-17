using System;
using System.Collections.Generic;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Configuration;
//using System.Web.UI.WebControls;
using System.Text;
//using System.Web.Security;
using DWMS_OCR.App_Code.Bll;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Dal
{
    public class ProfileDs
    {
        private static string GetConnectionString()
        {
            ConnectionStringSettings cts = Retrieve.GetConnectionString();
            return cts.ConnectionString;
        }


        #region Retrieve Methods

        /// <summary>
        /// Get the profile info including the operation and group details
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="selList"></param>
        /// <returns></returns>
        public static DataTable GetProfileInfo(Guid userId)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT Profile.Name,Designation,Section.ID as SectionID, Section.Code as SectionCode ,Section.Name as SectionName, Section.BusinessCode as BusinessCode, ");
            sqlStatement.Append("aspnet_Membership.IsApproved, Department.ID AS DepartmentID, Department.Code AS DepartmentCode, Department.Name AS DepartmentName FROM Profile ");
            sqlStatement.Append(" INNER JOIN aspnet_Membership ON aspnet_Membership.UserId= Profile.UserId ");
            sqlStatement.Append(" INNER JOIN Section ON Profile.Section=Section.Id ");
            sqlStatement.Append(" INNER JOIN Department ON Section.Department= Department.Id WHERE Profile.UserId=@UserId");

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
                return dataSet.Tables[0];
            }
        }

        public static int GetCountByEmailSetId(string OIC, int setId)
        {
            SqlCommand command = new SqlCommand();
            StringBuilder sqlStatement = new StringBuilder();

            sqlStatement.Append("SELECT * FROM Profile ");
            sqlStatement.Append("WHERE Email LIKE @OIC + '%' ");
            sqlStatement.Append("AND Section = (SELECT SectionId FROM DocSet WHERE Id = @setId)");

            command.Parameters.Add("@OIC", SqlDbType.VarChar);
            command.Parameters["@OIC"].Value = OIC;
            command.Parameters.Add("@setId", SqlDbType.Int);
            command.Parameters["@setId"].Value = setId;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                command.CommandText = sqlStatement.ToString();
                command.Connection = connection;
                DataSet dataSet = new DataSet();
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                connection.Open();
                adapter.Fill(dataSet);
                return dataSet.Tables[0].Rows.Count;
            }
        }

        #endregion
    }
}
