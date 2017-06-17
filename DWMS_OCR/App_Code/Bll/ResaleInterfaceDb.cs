using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.ResaleInterfaceTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class ResaleInterfaceDb
    {
        private ResaleInterfaceTableAdapter _ResaleInterfaceTableAdapter = null;

        protected ResaleInterfaceTableAdapter Adapter
        {
            get
            {
                if (_ResaleInterfaceTableAdapter == null)
                    _ResaleInterfaceTableAdapter = new ResaleInterfaceTableAdapter();

                return _ResaleInterfaceTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public ResaleInterface.ResaleInterfaceDataTable GetResaleInterfaces()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public ResaleInterface.ResaleInterfaceDataTable GetResaleInterfaceByCaseNo(string caseNumber)
        {
            return Adapter.GetDataByCaseNo(caseNumber);
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public ResaleInterface.ResaleInterfaceDataTable GetResaleInterfaceByNric(string nric)
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
