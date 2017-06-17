using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.SetAppTableAdapters;
using DWMS_OCR.App_Code.Dal;
using System.Collections;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Bll
{
    class SetAppDb
    {
        private SetAppTableAdapter _SetAppTableAdapter = null;

        protected SetAppTableAdapter Adapter
        {
            get
            {
                if (_SetAppTableAdapter == null)
                    _SetAppTableAdapter = new SetAppTableAdapter();

                return _SetAppTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SetApp.SetAppDataTable GetSetApps()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SetApp.SetAppDataTable GetSetAppByDocSetId(int docSetId)
        {
            return Adapter.GetDataByDocSetId(docSetId);
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert method
        /// </summary>
        /// <param name="AppSetAppRefId"></param>
        /// <param name="docType"></param>
        /// <param name="originalSetId"></param>
        /// <param name="status"></param>
        /// <param name="referenceNumber"></param>
        /// <param name="nric"></param>
        /// <returns></returns>
        public int Insert(int docSetId, int docAppId)
        {
            SetApp.SetAppDataTable dt = new SetApp.SetAppDataTable();
            SetApp.SetAppRow r = dt.NewSetAppRow();

            r.DocSetId = docSetId;
            r.DocAppId = docAppId;

            dt.AddSetAppRow(r);
            Adapter.Update(dt);
            int id = r.Id;

            if (id > 0)
            {
                // Save the personal records
                AppPersonalDb appPersonalDb = new AppPersonalDb();
                appPersonalDb.SavePersonalRecords(docSetId, docAppId);
            }

            return id;
        }

        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="docSetId"></param>
        /// <param name="refNo"></param>
        /// <returns></returns>
        public int Insert(int docSetId, string refNo)
        {
            DocAppDb docAppDb = new DocAppDb();
            int docAppId = docAppDb.GetIdByRefNo(refNo);

            //if (docAppId <= 0)
            //    docAppId = docAppDb.Insert(refNo, Util.GetReferenceType(refNo), null, null, AppStatusEnum.Pending_Documents.ToString(), null);                

            return (docAppId <= 0 ? -1 : Insert(docSetId, docAppId));
        }
        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        public bool DeleteBySetId(int docSetId)
        {
            return Adapter.DeleteByDocSetId(docSetId) > 0;
        }
        #endregion
    }
}
