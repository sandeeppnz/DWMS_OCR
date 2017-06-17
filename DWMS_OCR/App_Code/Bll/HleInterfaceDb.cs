using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.HleInterfaceTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class HleInterfaceDb
    {
        private HleInterfaceTableAdapter _HleInterfaceTableAdapter = null;

        protected HleInterfaceTableAdapter Adapter
        {
            get
            {
                if (_HleInterfaceTableAdapter == null)
                    _HleInterfaceTableAdapter = new HleInterfaceTableAdapter();

                return _HleInterfaceTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public HleInterface.HleInterfaceDataTable GetHleInterfaces()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public HleInterface.HleInterfaceDataTable GetHleInterfaceByHleNumber(string hleNumber)
        {
            return Adapter.GetDataByHleNumber(hleNumber);
        }

        /// <summary>
        /// Get Hle Status By Ref No From DocApp
        /// </summary>
        /// <param name="refNo"></param>
        /// <returns></returns>
        public static string GetHleStatusByRefNo(string refNo)
        {
            return HleInterfceDs.GetHleStatusByRefNo(refNo);
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public HleInterface.HleInterfaceDataTable GetHleInterfaceByNric(string nric)
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
