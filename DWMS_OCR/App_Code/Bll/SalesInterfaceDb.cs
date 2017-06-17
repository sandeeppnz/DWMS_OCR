using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.SalesInterfaceTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class SalesInterfaceDb
    {
        private SalesInterfaceTableAdapter _SalesInterfaceTableAdapter = null;

        protected SalesInterfaceTableAdapter Adapter
        {
            get
            {
                if (_SalesInterfaceTableAdapter == null)
                    _SalesInterfaceTableAdapter = new SalesInterfaceTableAdapter();

                return _SalesInterfaceTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SalesInterface.SalesInterfaceDataTable GetSalesInterfaces()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SalesInterface.SalesInterfaceDataTable GetSalesInterfaceByRefNo(string refNo)
        {
            return Adapter.GetDataByRegistrationNo(refNo);
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SalesInterface.SalesInterfaceDataTable GetSalesInterfaceByNric(string nric)
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
