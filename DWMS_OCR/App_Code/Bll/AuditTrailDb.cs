using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.AuditTrailTableAdapters;
using DWMS_OCR.App_Code.Dal;
using DWMS_OCR.App_Code.Helper;
using System.Data;
using System.Data.SqlClient;

namespace DWMS_OCR.App_Code.Bll
{
    class AuditTrailDb
    {
        static string connString = Retrieve.GetConnectionString().ConnectionString;

        private AuditTrailTableAdapter _AuditTrailTableAdapter = null;

        protected AuditTrailTableAdapter Adapter
        {
            get
            {
                if (_AuditTrailTableAdapter == null)
                    _AuditTrailTableAdapter = new AuditTrailTableAdapter();

                return _AuditTrailTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get records by table
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="recordId"></param>
        /// <returns></returns>
        private DataTable GetRecordByTableAndRecordId(TableNameEnum tableName, string recordId)
        {
            SqlCommand command = new SqlCommand();
            string sql = null;

            using (SqlConnection connection = new SqlConnection(connString))
            {
                if (Validation.IsNaturalNumber(recordId))
                {
                    sql = "SELECT * FROM [" + tableName.ToString() + "] WHERE Id = @Id";
                    command.Parameters.Add("@Id", SqlDbType.Int);
                    command.Parameters["@Id"].Value = int.Parse(recordId);
                }
                else
                {
                    if (tableName.Equals(TableNameEnum.aspnet_Roles))
                    {
                        sql = "SELECT * FROM [" + tableName + "] WHERE roleId = @Id";
                    }
                    else
                    {
                        sql = "SELECT * FROM [" + tableName + "] WHERE UserId = @Id";
                    }
                    command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier);
                    command.Parameters["@Id"].Value = new Guid(recordId);
                }

                command.CommandText = sql;
                command.Connection = connection;
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dataSet = new DataSet();
                connection.Open();
                adapter.Fill(dataSet);
                return dataSet.Tables[0];
            }
        }

        /// <summary>
        /// Get table columns
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private DataTable GetTableColumns(TableNameEnum tableName)
        {
            SqlCommand command = new SqlCommand();
            string sql = "SELECT column_name FROM information_schema.columns WHERE table_name = '" + tableName.ToString() + "'";

            using (SqlConnection connection = new SqlConnection(connString))
            {
                command.CommandText = sql;
                command.Connection = connection;
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dataSet = new DataSet();
                connection.Open();
                adapter.Fill(dataSet);
                return dataSet.Tables[0];
            }
        }        
        #endregion

        #region Insert Methods
        /// <summary>
        /// Record the transaction
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="recordId"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public Guid? Record(TableNameEnum tableName, string recordId, OperationTypeEnum operation)
        {
            if (!Validation.IsNaturalNumber(recordId) && !Validation.IsGuid(recordId))
                return null;

            // Get record by table name and record ID
            DataTable record = GetRecordByTableAndRecordId(tableName, recordId);
            if (record.Rows.Count == 0)
                return null;

            // Get table columns
            DataTable columns = GetTableColumns(tableName);
            if (columns.Rows.Count == 0)
                return null;

            // Get audit trail parameters
            Guid? userId = null;
            string userRole = "Unknown";

            ProfileDb profileDb = new ProfileDb();
            Guid? systemGuid = profileDb.GetSystemGuid();

            if (systemGuid.HasValue)
            {
                userId = systemGuid.Value;
                userRole = "System Administrator";
            }

            // Get the hostname
            string myHost = System.Net.Dns.GetHostName();

            // Get the IP from the host name
            string ip = System.Net.Dns.GetHostEntry(myHost).AddressList[0].ToString();
            string systemInfo = "NA";

            DateTime auditDate = DateTime.Now;
            Guid operationId = Guid.NewGuid();
            int count = 0;

            // Inser column name and values
            foreach (DataRow r in columns.Rows)
            {
                string columnName = r["column_name"].ToString();
                string columnValue = record.Rows[0][columnName].ToString();

                count = count + Insert(auditDate, operation, operationId, tableName,
                    recordId, columnName, columnValue, userId, userRole, ip, systemInfo);
            }

            return operationId;
        }

        /// <summary>
        /// Insert transaction
        /// </summary>
        /// <param name="auditDate"></param>
        /// <param name="operation"></param>
        /// <param name="operationId"></param>
        /// <param name="tableName"></param>
        /// <param name="recordId"></param>
        /// <param name="columnName"></param>
        /// <param name="columnValue"></param>
        /// <param name="userId"></param>
        /// <param name="userRole"></param>
        /// <param name="ip"></param>
        /// <param name="systemInfo"></param>
        /// <returns></returns>
        private int Insert(DateTime auditDate, OperationTypeEnum operation, Guid operationId,
            TableNameEnum tableName, string recordId, string columnName, string columnValue,
            Guid? userId, string userRole, string ip, string systemInfo)
        {
            // Insert

            if (userId == null)
            {
                return 0;
            }

            AuditTrail.AuditTrailDataTable auditTrails = new AuditTrail.AuditTrailDataTable();
            AuditTrail.AuditTrailRow auditTrail = auditTrails.NewAuditTrailRow();

            auditTrail.AuditDate = auditDate;
            auditTrail.Operation = operation.ToString();
            auditTrail.OperationId = operationId;
            auditTrail.TableName = tableName.ToString();
            auditTrail.RecordId = recordId;
            auditTrail.ColumnName = columnName;
            auditTrail.ColumnValue = columnValue;
            auditTrail.UserId = userId.Value;
            //auditTrail.FullName = NameManager.GetFullName(userId.Value);
            auditTrail.UserRole = userRole;
            auditTrail.IP = ip;
            auditTrail.SystemInfo = systemInfo;

            auditTrails.AddAuditTrailRow(auditTrail);
            int rowsAffected = Adapter.Update(auditTrails);

            return rowsAffected;
        }
        #endregion
    }
}
