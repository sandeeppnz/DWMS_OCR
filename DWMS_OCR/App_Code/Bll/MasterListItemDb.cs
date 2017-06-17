using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.MasterListItemTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class MasterListItemDb
    {
        private MasterListItemTableAdapter _MasterListItemTableAdapter = null;

        protected MasterListItemTableAdapter Adapter
        {
            get
            {
                if (_MasterListItemTableAdapter == null)
                    _MasterListItemTableAdapter = new MasterListItemTableAdapter();

                return _MasterListItemTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public MasterListItem.MasterListItemDataTable GetMasterListItems()
        {
            return Adapter.GetData();
        }

        public MasterListItem.MasterListItemDataTable GetMasterListItemByMasterListId(int masterListId)
        {
            return Adapter.GetDataByMasterListId(masterListId);
        }

        public string GetMasterListItemName(int masterListId, string item)
        {
            string result = string.Empty;

            MasterListItem.MasterListItemDataTable dt = GetMasterListItemByMasterListId(masterListId);
            
            foreach(MasterListItem.MasterListItemRow dr in dt)
            {
                #region Modified By Edward 06/03/2014 MyHDBPage Becomes MyHDBPage_Common_Panel Always
                //if (dr.Name.ToUpper().Trim().Contains(item.ToUpper().Trim()))   
                //    result = dr.Name;
                if (dr.Name.ToUpper().Trim().Equals(item.ToUpper().Trim()))
                    result = dr.Name;
                #endregion
            }

            return result;
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
