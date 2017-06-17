using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.DepartmentTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class DepartmentDb
    {
        private DepartmentTableAdapter _DepartmentTableAdapter = null;

        protected DepartmentTableAdapter Adapter
        {
            get
            {
                if (_DepartmentTableAdapter == null)
                    _DepartmentTableAdapter = new DepartmentTableAdapter();

                return _DepartmentTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public Department.DepartmentDataTable GetDepartments()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document set by id
        /// </summary>
        /// <returns></returns>
        public Department.DepartmentDataTable GetDepartmentById(int id)
        {
            return Adapter.GetDataById(id);
        }

        public Department.DepartmentDataTable GetDepartmentByCode(DepartmentCodeEnum code)
        {
            return Adapter.GetDataByCode(code.ToString());
        }

        public string GetDepartmentMailingList(DepartmentCodeEnum department)
        {
            string email = string.Empty;

            Department.DepartmentDataTable dt = GetDepartmentByCode(department);

            if (dt.Rows.Count > 0)
            {
                Department.DepartmentRow dr = dt[0];
                email = dr.MailingList;
            }

            return email;
        }
        #endregion

        #region Insert Methods
        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        #endregion
    }
}
