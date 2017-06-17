using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.SersInterfaceTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class SersInterfaceDb
    {
        private SersInterfaceTableAdapter _SersInterfaceTableAdapter = null;

        protected SersInterfaceTableAdapter Adapter
        {
            get
            {
                if (_SersInterfaceTableAdapter == null)
                    _SersInterfaceTableAdapter = new SersInterfaceTableAdapter();

                return _SersInterfaceTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SersInterface.SersInterfaceDataTable GetSersInterfaces()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SersInterface.SersInterfaceDataTable GetSersInterfaceByRefNo(string refNumber)
        {
            return Adapter.GetDataBySchAcc(refNumber);
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SersInterface.SersInterfaceDataTable GetSersInterfaceByNric(string nric)
        {
            return Adapter.GetDataByNric(nric);
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
