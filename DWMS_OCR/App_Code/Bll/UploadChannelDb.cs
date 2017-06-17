using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.UploadChannelTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class UploadChannelDb
    {
        private UploadChannelTableAdapter _UploadChannelTableAdapter = null;

        protected UploadChannelTableAdapter Adapter
        {
            get
            {
                if (_UploadChannelTableAdapter == null)
                    _UploadChannelTableAdapter = new UploadChannelTableAdapter();

                return _UploadChannelTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public UploadChannel.UploadChannelDataTable GetUploadChannels()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document set by id
        /// </summary>
        /// <returns></returns>
        public UploadChannel.UploadChannelDataTable GetUploadChannelByName(string name)
        {
            return Adapter.GetDataByName(name);
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
