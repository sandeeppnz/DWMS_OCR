using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.AppPersonalSalaryTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class AppPersonalSalaryDb
    {
        private AppPersonalSalaryTableAdapter _AppPersonalSalaryTableAdapter = null;

        protected AppPersonalSalaryTableAdapter Adapter
        {
            get
            {
                if (_AppPersonalSalaryTableAdapter == null)
                    _AppPersonalSalaryTableAdapter = new AppPersonalSalaryTableAdapter();

                return _AppPersonalSalaryTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public AppPersonalSalary.AppPersonalSalaryDataTable GetAppPersonalSalarys()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get data by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AppPersonalSalary.AppPersonalSalaryDataTable GetAppPersonalSalaryById(int id)
        {
            return Adapter.GetDataById(id);
        }

        /// <summary>
        /// Get Salary info
        /// </summary>
        /// <param name="appPersonalId"></param>
        /// <returns></returns>
        public AppPersonalSalary.AppPersonalSalaryDataTable GetAppPersonalSalaryByAppPersonalId(int appPersonalId)
        {
            return Adapter.GetDataByAppPersonalId(appPersonalId);
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert method
        /// </summary>
        /// <param name="AppAppPersonalSalaryRefId"></param>
        /// <param name="docType"></param>
        /// <param name="originalSetId"></param>
        /// <param name="status"></param>
        /// <param name="referenceNumber"></param>
        /// <param name="nric"></param>
        /// <returns></returns>
        public int Insert(int appPersonalId, string month1Name, string month1Value, string month2Name, string month2Value,
            string month3Name, string month3Value, string month4Name, string month4Value, string month5Name, string month5Value,
            string month6Name, string month6Value, string month7Name, string month7Value, string month8Name, string month8Value,
            string month9Name, string month9Value, string month10Name, string month10Value, string month11Name, string month11Value,
            string month12Name, string month12Value)
        {
            AppPersonalSalary.AppPersonalSalaryDataTable dt = new AppPersonalSalary.AppPersonalSalaryDataTable();
            AppPersonalSalary.AppPersonalSalaryRow r = dt.NewAppPersonalSalaryRow();

            r.AppPersonalId = appPersonalId;
            r.Month1Name = month1Name;
            r.Month1Value = month1Value;
            r.Month2Name = month2Name;
            r.Month2Value = month2Value;
            r.Month3Name = month3Name;
            r.Month3Value = month3Value;
            r.Month4Name = month4Name;
            r.Month4Value = month4Value;
            r.Month5Name = month5Name;
            r.Month5Value = month5Value;
            r.Month6Name = month6Name;
            r.Month6Value = month6Value;
            r.Month7Name = month7Name;
            r.Month7Value = month7Value;
            r.Month8Name = month8Name;
            r.Month8Value = month8Value;
            r.Month9Name = month9Name;
            r.Month9Value = month9Value;
            r.Month10Name = month10Name;
            r.Month10Value = month10Value;
            r.Month11Name = month11Name;
            r.Month11Value = month11Value;
            r.Month12Name = month12Name;
            r.Month12Value = month12Value;

            dt.AddAppPersonalSalaryRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }
        #endregion

        #region Update Methods
        /// <summary>
        /// Update AppPersonalSalary
        /// </summary>
        /// <param name="id"></param>
        /// <param name="month1Name"></param>
        /// <param name="month1Value"></param>
        /// <param name="month2Name"></param>
        /// <param name="month2Value"></param>
        /// <param name="month3Name"></param>
        /// <param name="month3Value"></param>
        /// <param name="month4Name"></param>
        /// <param name="month4Value"></param>
        /// <param name="month5Name"></param>
        /// <param name="month5Value"></param>
        /// <param name="month6Name"></param>
        /// <param name="month6Value"></param>
        /// <param name="month7Name"></param>
        /// <param name="month7Value"></param>
        /// <param name="month8Name"></param>
        /// <param name="month8Value"></param>
        /// <param name="month9Name"></param>
        /// <param name="month9Value"></param>
        /// <param name="month10Name"></param>
        /// <param name="month10Value"></param>
        /// <param name="month11Name"></param>
        /// <param name="month11Value"></param>
        /// <param name="month12Name"></param>
        /// <param name="month12Value"></param>
        /// <returns></returns>
        public bool Update(int id, string month1Name, string month1Value, string month2Name, string month2Value,
            string month3Name, string month3Value, string month4Name, string month4Value, string month5Name, string month5Value,
            string month6Name, string month6Value, string month7Name, string month7Value, string month8Name, string month8Value,
            string month9Name, string month9Value, string month10Name, string month10Value, string month11Name, string month11Value,
            string month12Name, string month12Value)
        {
            AppPersonalSalary.AppPersonalSalaryDataTable appPersonalSalarys = GetAppPersonalSalaryById(id);

            if (appPersonalSalarys.Count == 0) return false;

            AppPersonalSalary.AppPersonalSalaryRow appPersonalSalaryRow = appPersonalSalarys[0];

            appPersonalSalaryRow.Month1Name = (String.IsNullOrEmpty(month1Name) ? " " : month1Name);
            appPersonalSalaryRow.Month2Name = (String.IsNullOrEmpty(month2Name) ? " " : month2Name);
            appPersonalSalaryRow.Month3Name = (String.IsNullOrEmpty(month3Name) ? " " : month3Name);
            appPersonalSalaryRow.Month4Name = (String.IsNullOrEmpty(month4Name) ? " " : month4Name);
            appPersonalSalaryRow.Month5Name = (String.IsNullOrEmpty(month5Name) ? " " : month5Name);
            appPersonalSalaryRow.Month6Name = (String.IsNullOrEmpty(month6Name) ? " " : month6Name);
            appPersonalSalaryRow.Month7Name = (String.IsNullOrEmpty(month7Name) ? " " : month7Name);
            appPersonalSalaryRow.Month8Name = (String.IsNullOrEmpty(month8Name) ? " " : month8Name);
            appPersonalSalaryRow.Month9Name = (String.IsNullOrEmpty(month9Name) ? " " : month9Name);
            appPersonalSalaryRow.Month10Name = (String.IsNullOrEmpty(month11Name) ? " " : month10Name);
            appPersonalSalaryRow.Month11Name = (String.IsNullOrEmpty(month11Name) ? " " : month11Name);
            appPersonalSalaryRow.Month12Name = (String.IsNullOrEmpty(month12Name) ? " " : month12Name);

            appPersonalSalaryRow.Month1Value = (String.IsNullOrEmpty(month1Value) ? " " : month1Value);
            appPersonalSalaryRow.Month2Value = (String.IsNullOrEmpty(month2Value) ? " " : month2Value);
            appPersonalSalaryRow.Month3Value = (String.IsNullOrEmpty(month3Value) ? " " : month3Value);
            appPersonalSalaryRow.Month4Value = (String.IsNullOrEmpty(month4Value) ? " " : month4Value);
            appPersonalSalaryRow.Month5Value = (String.IsNullOrEmpty(month5Value) ? " " : month5Value);
            appPersonalSalaryRow.Month6Value = (String.IsNullOrEmpty(month6Value) ? " " : month6Value);
            appPersonalSalaryRow.Month7Value = (String.IsNullOrEmpty(month7Value) ? " " : month7Value);
            appPersonalSalaryRow.Month8Value = (String.IsNullOrEmpty(month8Value) ? " " : month8Value);
            appPersonalSalaryRow.Month9Value = (String.IsNullOrEmpty(month9Value) ? " " : month9Value);
            appPersonalSalaryRow.Month10Value = (String.IsNullOrEmpty(month10Value) ? " " : month10Value);
            appPersonalSalaryRow.Month11Value = (String.IsNullOrEmpty(month11Value) ? " " : month11Value);
            appPersonalSalaryRow.Month12Value = (String.IsNullOrEmpty(month12Value) ? " " : month12Value);

            int affected = Adapter.Update(appPersonalSalarys);
            return affected > 0;
        }
        #endregion

        #region Delete Methods
        #endregion
    }
}
