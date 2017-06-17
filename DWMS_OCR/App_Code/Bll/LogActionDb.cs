using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.LogActionTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class LogActionDb
    {
        private LogActionTableAdapter _LogActionTableAdapter = null;

        protected LogActionTableAdapter Adapter
        {
            get
            {
                if (_LogActionTableAdapter == null)
                    _LogActionTableAdapter = new LogActionTableAdapter();

                return _LogActionTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public LogAction.LogActionDataTable GetLogActions()
        {
            return Adapter.GetData();
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert method
        /// </summary>
        /// <param name="AppLogActionRefId"></param>
        /// <param name="docType"></param>
        /// <param name="originalSetId"></param>
        /// <param name="status"></param>
        /// <param name="referenceNumber"></param>
        /// <param name="nric"></param>
        /// <returns></returns>
        public int Insert(Guid userId, string action, string actionReplaceValue1, string actionReplaceValue2, string actionReplaceValue3, string actionReplaceValue4, LogTypeEnum logType, int typeId)
        {
            LogAction.LogActionDataTable logAction = new LogAction.LogActionDataTable();
            LogAction.LogActionRow r = logAction.NewLogActionRow();

            string act = action.ToString();

            //replace SEMICOLON            
            act = act.Replace("SEMICOLON", ";");
            act = act.Replace("COLON", ":");
            act = act.Replace("EQUALSSIGN", "=");
            act = act.Replace("PERIOD", ".");

            if (!string.IsNullOrEmpty(actionReplaceValue1))
                act = act.Replace(LogActionEnum.REPLACE1.ToString(), actionReplaceValue1);

            if (!string.IsNullOrEmpty(actionReplaceValue2))
                act = act.Replace(LogActionEnum.REPLACE2.ToString(), actionReplaceValue2);

            if (!string.IsNullOrEmpty(actionReplaceValue3))
                act = act.Replace(LogActionEnum.REPLACE3.ToString(), actionReplaceValue3);

            if (!string.IsNullOrEmpty(actionReplaceValue4))
                act = act.Replace(LogActionEnum.REPLACE4.ToString(), actionReplaceValue4);

            r.UserId = userId;
            r.Action = act.Replace('_', ' ');
            r.DocType = logType.ToString();
            r.TypeId = typeId;
            r.LogDate = DateTime.Now;

            logAction.AddLogActionRow(r);
            Adapter.Update(logAction);
            int id = r.Id;

            return id;
        }
        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        #endregion
    }
}
