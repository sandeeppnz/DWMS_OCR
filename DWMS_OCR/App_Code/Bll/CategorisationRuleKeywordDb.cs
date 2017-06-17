using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.CategorisationRuleKeywordTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class CategorisationRuleKeywordDb
    {
        private CategorisationRuleKeywordTableAdapter _CategorisationRuleKeywordTableAdapter = null;

        protected CategorisationRuleKeywordTableAdapter Adapter
        {
            get
            {
                if (_CategorisationRuleKeywordTableAdapter == null)
                    _CategorisationRuleKeywordTableAdapter = new CategorisationRuleKeywordTableAdapter();

                return _CategorisationRuleKeywordTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public CategorisationRuleKeyword.CategorisationRuleKeywordDataTable GetCategorisationRuleKeywords()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get Categorisation Rule keyword by rule id
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CategorisationRuleKeyword.CategorisationRuleKeywordDataTable GetCategorisationRuleKeyword(int ruleId)
        {
            return Adapter.GetDataByRuleId(ruleId);
        }

        /// <summary>
        /// Get the categorisation rule keyword rows
        /// </summary>
        /// <param name="ruleId"></param>
        /// <returns></returns>
        public CategorisationRuleKeyword.CategorisationRuleKeywordRow[] GetCategorisationRuleKeywordsRows(int ruleId)
        {
            CategorisationRuleKeyword.CategorisationRuleKeywordRow[] drs = null;

            CategorisationRuleKeyword.CategorisationRuleKeywordDataTable dt = GetCategorisationRuleKeyword(ruleId);

            drs = new CategorisationRuleKeyword.CategorisationRuleKeywordRow[dt.Rows.Count];
            int count = 0;
            foreach (CategorisationRuleKeyword.CategorisationRuleKeywordRow dr in dt.Rows)
            {
                drs[count] = dr;
                count++;
            }

            return drs;
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
