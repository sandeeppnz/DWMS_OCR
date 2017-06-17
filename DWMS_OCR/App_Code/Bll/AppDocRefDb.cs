using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.AppDocRefTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class AppDocRefDb
    {
        private AppDocRefTableAdapter _AppDocRefTableAdapter = null;

        protected AppDocRefTableAdapter Adapter
        {
            get
            {
                if (_AppDocRefTableAdapter == null)
                    _AppDocRefTableAdapter = new AppDocRefTableAdapter();

                return _AppDocRefTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public AppDocRef.AppDocRefDataTable GetAppDocRefs()
        {
            return Adapter.GetData();
        }

        public AppDocRef.AppDocRefDataTable GetAppDocRefByDocId(int docId)
        {
            return Adapter.GetDataByDocId(docId);
        }

        public AppDocRef.AppDocRefDataTable GetAppDocRefByDocIdAppPersonalId(int docId, int appPersonalId)
        {
            return Adapter.GetDataByAppPersonalIdAndDocId(appPersonalId, docId);
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert method
        /// </summary>
        /// <param name="AppAppDocRefRefId"></param>
        /// <param name="docType"></param>
        /// <param name="originalSetId"></param>
        /// <param name="status"></param>
        /// <param name="referenceNumber"></param>
        /// <param name="nric"></param>
        /// <returns></returns>
        public int Insert(int docId, int appPersonalId)
        {
            if (GetAppDocRefByDocIdAppPersonalId(docId, appPersonalId).Rows.Count <= 0)
            {
                AppDocRef.AppDocRefDataTable dt = new AppDocRef.AppDocRefDataTable();
                AppDocRef.AppDocRefRow r = dt.NewAppDocRefRow();

                r.DocId = docId;
                r.AppPersonalId = appPersonalId;

                dt.AddAppDocRefRow(r);
                Adapter.Update(dt);
                int id = r.Id;
                return id;
            }
            else
                return -1;
        }
        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        #endregion
    }
}
