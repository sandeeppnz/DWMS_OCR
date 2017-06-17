using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.MetaFieldTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class MetaFieldDb
    {
        private MetaFieldTableAdapter _MetaFieldTableAdapter = null;

        protected MetaFieldTableAdapter Adapter
        {
            get
            {
                if (_MetaFieldTableAdapter == null)
                    _MetaFieldTableAdapter = new MetaFieldTableAdapter();

                return _MetaFieldTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public MetaField.MetaFieldDataTable GetMetaFields()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public MetaField.MetaFieldDataTable GetMetaFieldByDocTypeCode(string docTypeCode)
        {
            return Adapter.GetDataByDocTypeCode(docTypeCode);
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
