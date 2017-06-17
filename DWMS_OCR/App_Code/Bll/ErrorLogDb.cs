using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.ErrorLogTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class ErrorLogDb
    {
        private ErrorLogTableAdapter _ErrorLogTableAdapter = null;

        protected ErrorLogTableAdapter Adapter
        {
            get
            {
                if (_ErrorLogTableAdapter == null)
                    _ErrorLogTableAdapter = new ErrorLogTableAdapter();

                return _ErrorLogTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public ErrorLog.ErrorLogDataTable GetErrorLogs()
        {
            return Adapter.GetData();
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="message"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public int Insert(string functionName, string message, DateTime date)
        {
            ErrorLog.ErrorLogDataTable dt = new ErrorLog.ErrorLogDataTable();
            ErrorLog.ErrorLogRow r = dt.NewErrorLogRow();

            r.FunctionName = functionName;
            r.Message = message;
            r.Date = date;

            dt.AddErrorLogRow(r);
            Adapter.Update(dt);
            int rowAffected = r.Id;
            return rowAffected;
        }

        public int Insert(string functionName, string message)
        {
            ErrorLog.ErrorLogDataTable dt = new ErrorLog.ErrorLogDataTable();
            ErrorLog.ErrorLogRow r = dt.NewErrorLogRow();

            r.FunctionName = functionName;
            r.Message = message;
            r.Date = DateTime.Now;

            dt.AddErrorLogRow(r);
            Adapter.Update(dt);
            int rowAffected = r.Id;
            return rowAffected;
        }

        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        #endregion
    }
}
