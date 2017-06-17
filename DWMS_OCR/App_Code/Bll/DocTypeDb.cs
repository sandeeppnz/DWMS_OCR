using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.DocTypeTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class DocTypeDb
    {
        private DocTypeTableAdapter _DocTypeTableAdapter = null;

        protected DocTypeTableAdapter Adapter
        {
            get
            {
                if (_DocTypeTableAdapter == null)
                    _DocTypeTableAdapter = new DocTypeTableAdapter();

                return _DocTypeTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get all the document types.
        /// </summary>
        /// <returns>The document type table</returns>
        public DocType.DocTypeDataTable GetDocTypes()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get document type by document id and/or document sub id
        /// </summary>
        /// <param name="docId">Doc id</param>
        /// <param name="docSubId">Doc sub id</param>
        /// <returns>DocType table</returns>
        public DocType.DocTypeDataTable GetDocType(string docId, string docSubId)
        {
            if (String.IsNullOrEmpty(docSubId) || docSubId.Trim() == "00")
                return Adapter.GetDataByDocId(docId);//return Adapter.GetDataByDocId(docId);
            else
                return Adapter.GetDataByDocIdAndDocSubId(docId, docSubId);
        }


        /// <summary>
        /// Get DocType by code
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DocType.DocTypeDataTable GetDocTypeByCode(string code)
        {
            return Adapter.GetDocTypeByCode(code);
        }


        #endregion
    }
}
