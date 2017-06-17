using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.SetDocRefTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class SetDocRefDb
    {
        private SetDocRefTableAdapter _SetDocRefTableAdapter = null;

        protected SetDocRefTableAdapter Adapter
        {
            get
            {
                if (_SetDocRefTableAdapter == null)
                    _SetDocRefTableAdapter = new SetDocRefTableAdapter();

                return _SetDocRefTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SetDocRef.SetDocRefDataTable GetSetDocRefs()
        {
            return Adapter.GetData();
        }

        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert method
        /// </summary>
        /// <param name="AppSetDocRefRefId"></param>
        /// <param name="docType"></param>
        /// <param name="originalSetId"></param>
        /// <param name="status"></param>
        /// <param name="referenceNumber"></param>
        /// <param name="nric"></param>
        /// <returns></returns>
        public int Insert(int docId, int docPersonalId)
        {
            SetDocRef.SetDocRefDataTable dt = new SetDocRef.SetDocRefDataTable();
            SetDocRef.SetDocRefRow r = dt.NewSetDocRefRow();

            r.DocId = docId;
            r.DocPersonalId = docPersonalId;

            dt.AddSetDocRefRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }
        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        #endregion
    }
}
