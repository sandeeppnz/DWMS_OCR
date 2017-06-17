using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.MasterListTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class MasterListDb
    {
        private MasterListTableAdapter _MasterListTableAdapter = null;

        protected MasterListTableAdapter Adapter
        {
            get
            {
                if (_MasterListTableAdapter == null)
                    _MasterListTableAdapter = new MasterListTableAdapter();

                return _MasterListTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public MasterList.MasterListDataTable GetMasterLists()
        {
            return Adapter.GetData();
        }

        public MasterList.MasterListDataTable GetMasterListByName(string name)
        {
            return Adapter.GetDataByName(name);
        }

        public int GetMasterListIdByName(string name)
        {
            int id = -1;

            MasterList.MasterListDataTable dt = GetMasterListByName(name);

            if (dt.Rows.Count > 0)
            {
                MasterList.MasterListRow dr = dt[0];
                id = dr.Id;
            }

            return id;
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
