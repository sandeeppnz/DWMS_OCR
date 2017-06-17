using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.SectionTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class SectionDb
    {
        private SectionTableAdapter _SectionTableAdapter = null;

        protected SectionTableAdapter Adapter
        {
            get
            {
                if (_SectionTableAdapter == null)
                    _SectionTableAdapter = new SectionTableAdapter();

                return _SectionTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public Section.SectionDataTable GetSections()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document set by id
        /// </summary>
        /// <returns></returns>
        public Section.SectionDataTable GetSectionById(int id)
        {
            return Adapter.GetDataById(id);
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
