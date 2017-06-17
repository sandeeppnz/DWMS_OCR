using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.EmailTemplateTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class EmailTemplateDb
    {
        private EmailTemplateTableAdapter _EmailTemplateTableAdapter = null;

        protected EmailTemplateTableAdapter Adapter
        {
            get
            {
                if (_EmailTemplateTableAdapter == null)
                    _EmailTemplateTableAdapter = new EmailTemplateTableAdapter();

                return _EmailTemplateTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public EmailTemplate.EmailTemplateDataTable GetEmailTemplates()
        {
            return Adapter.GetData();
        }

        public EmailTemplate.EmailTemplateDataTable GetEmailTemplateByCode(string code)
        {
            return Adapter.GetDataByCode(code);
        }
        #endregion
    }
}
