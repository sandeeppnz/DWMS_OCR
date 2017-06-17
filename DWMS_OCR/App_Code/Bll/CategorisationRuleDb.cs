using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.CategorisationRuleTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class CategorisationRuleDb
    {
        private CategorisationRuleTableAdapter _CategorisationRuleTableAdapter = null;

        protected CategorisationRuleTableAdapter Adapter
        {
            get
            {
                if (_CategorisationRuleTableAdapter == null)
                    _CategorisationRuleTableAdapter = new CategorisationRuleTableAdapter();

                return _CategorisationRuleTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public CategorisationRule.CategorisationRuleDataTable GetCategorisationRules()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get Categorisation Rule by code
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CategorisationRule.CategorisationRuleDataTable GetCategorisationRule(string code)
        {
            return Adapter.GetDataByDocTypeCode(code);
        }

        /// <summary>
        /// Get the categoriation rule row of the document type
        /// </summary>
        /// <param name="docType"></param>
        /// <returns></returns>
        public CategorisationRule.CategorisationRuleRow GetCategorisationRulesRow(string docType)
        {
            CategorisationRule.CategorisationRuleRow dr = null;

            CategorisationRule.CategorisationRuleDataTable dt = GetCategorisationRule(docType);

            if (dt.Rows.Count > 0)
                dr = dt[0];

            return dr;
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
