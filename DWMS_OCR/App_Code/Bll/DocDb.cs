using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.DocTableAdapters;
using DWMS_OCR.App_Code.Dal;
using System.Data;

namespace DWMS_OCR.App_Code.Bll
{
    class DocDb
    {
        private DocTableAdapter _DocTableAdapter = null;

        protected DocTableAdapter Adapter
        {
            get
            {
                if (_DocTableAdapter == null)
                    _DocTableAdapter = new DocTableAdapter();

                return _DocTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public Doc.DocDataTable GetDocs()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public Doc.DocDataTable GetDocBySetId(int setId)
        {
            return Adapter.GetDataBySetId(setId);
        }

        /// <summary>
        /// Retrieve the documents by docSetId
        /// </summary>
        /// <returns></returns>
        public Doc.DocDataTable GetDocByDocSetId(int id)
        {
            return Adapter.GetDocByDocSetId(id);
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public Doc.DocDataTable GetDocById(int id)
        {
            return Adapter.GetDataById(id);
        }

        public int? GetDocSetIdByDocId(int id)
        {
            Doc.DocDataTable dt = Adapter.GetDataById(id);
            if (dt.Rows.Count > 0)
            {
                Doc.DocRow r = dt[0];
                return r.DocSetId;
            }
            return null;
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public Doc.DocDataTable GetDocByCmDocId(string CmDocID)
        {
            return Adapter.GetDataByCmDocID(CmDocID);
        }

        public int? GetDocIdByCmDocId(string CmDocID)
        {
            Doc.DocDataTable dt = Adapter.GetDataByCmDocID(CmDocID);
            if (dt.Rows.Count > 0)
            {
                Doc.DocRow r = dt[0];
                return r.Id;
            }
            return null;
        }

        public int? GetDocSetIdByCmDocId(string CmDocID)
        {
            Doc.DocDataTable dt = Adapter.GetDataByCmDocID(CmDocID);
            if (dt.Rows.Count > 0)
            {
                Doc.DocRow r = dt[0];
                return r.DocSetId;
            }
            return null;
        }

        /// <summary>
        /// Get the doc folder
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetDocFolder(int id)
        {
            string result = string.Empty;

            result = DocDs.GetDocPersonalFolder(id);

            if (String.IsNullOrEmpty(result))
                result = DocDs.GetAppPersonalFolder(id);

            return result;
        }

        public Doc.DocDataTable GetDocByRawPageId(int rawPageId)
        {
            return Adapter.GetDataByRawPageId(rawPageId);
        }

        public DataTable GetDistinctOrigSetIdForNullSetId()
        {
            return DocDs.GetDistinctOrigSetIdForNullSetId();
        }

        public DataTable GetDocDetails(int docId)
        {
            return DocDs.GetDocDetails(docId);
        }


        public DataTable GetMetaDataDetails(int docId, string docTypeCode)
        {
            return DocDs.GetMetaDataDetails(docId,docTypeCode);
        }


        public DataTable GetModifiedDocDetails(int docAppId, string docStatus, string docStatus1, string imageCondition, string toAvoidDocType, string docSentToCDBStatus, string docsentToCDBAccept, string docSetSentToCDBStatus)
        {
            return DocDs.GetCompletedDocDetails(docAppId, docStatus, docStatus1, imageCondition, toAvoidDocType, docSentToCDBStatus, docsentToCDBAccept, docSetSentToCDBStatus);

        }


        public DataTable GetCompletedDocDetails(int docAppId, string docStatus, string docStatus1, string imageCondition, string toAvoidDocType, string sentToCDBStatus, string sentToCDBAccept)
        {
            return DocDs.GetCompletedDocDetails(docAppId, docStatus, docStatus1, imageCondition, toAvoidDocType, sentToCDBStatus, sentToCDBAccept);

        }


        public DataTable GetDocNotSentToCDB(int docSetId, SendToCDBStatusEnum toAvoidSentToCDBStatus)
        {
            return DocDs.GetDocsNotSentToCDB(docSetId, toAvoidSentToCDBStatus.ToString());

        }


        
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert method
        /// </summary>
        /// <param name="docSetId"></param>
        /// <param name="docType"></param>
        /// <param name="originalSetId"></param>
        /// <param name="status"></param>
        /// <param name="referenceNumber"></param>
        /// <param name="nric"></param>
        /// <returns></returns>
        public int Insert(int docSetId, string docType, int originalSetId, string status, string customerIdSubFromSource)
        {
            Doc.DocDataTable dt = new Doc.DocDataTable();
            Doc.DocRow r = dt.NewDocRow();

            r.DocTypeCode = docType;
            r.OriginalSetId = originalSetId;
            r.DocSetId = docSetId;
            r.Status = status;
            r.ImageCondition = "NA";
            r.DocumentCondition = "NA";
            r.SendToCDBStatus = SendToCDBStatusEnum.Ready.ToString();
            r.CustomerIdSubFromSource = customerIdSubFromSource;
            

            dt.AddDocRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }
        #endregion

        #region Update Methods

        /// <summary>
        /// Update the folder of the document
        /// </summary>
        /// <param name="id"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public bool UpdateConvertedToSampleDocFlag(int id, bool isConverted)
        {
            Doc.DocDataTable dt = GetDocById(id);

            if (dt.Rows.Count == 0) return false;

            Doc.DocRow r = dt[0];

            r.ConvertedToSampleDoc = isConverted;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update isVerified status
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isVerified"></param>
        /// <returns></returns>
        public bool UpdateIsVerified(int id, bool isVerified)
        {
            Doc.DocDataTable dt = GetDocById(id);

            if (dt.Rows.Count == 0) return false;

            Doc.DocRow r = dt[0];

            r.IsVerified = isVerified;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        public bool UpdateCmDocumentId(int id, string cmDocumentId)
        {
            Doc.DocDataTable dt = GetDocById(id);

            if (dt.Rows.Count == 0) return false;

            Doc.DocRow r = dt[0];

            r.CmDocumentId = cmDocumentId;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }


        public bool UpdateSentToCDBStatus(int id, SendToCDBStatusEnum status, SendToCDBStatusEnum accept)
        {
            Doc.DocDataTable dt = GetDocById(id);

            if (dt.Rows.Count == 0) return false;

            Doc.DocRow r = dt[0];

            r.SendToCDBStatus = status.ToString();
            if (!r.IsSendToCDBAcceptNull() && r.SendToCDBAccept != SendToCDBStatusEnum.Sent.ToString())
                r.SendToCDBAccept = accept.ToString();
            if (r.IsSendToCDBAcceptNull())
                r.SendToCDBAccept = accept.ToString();

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        public bool UpdateSentToCDBAccept(int id, SendToCDBStatusEnum status)
        {
            Doc.DocDataTable dt = GetDocById(id);

            if (dt.Rows.Count == 0) return false;

            Doc.DocRow r = dt[0];

            r.SendToCDBAccept = status.ToString();

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }




        /// <summary>
        /// Update DocChannel and CmDocumentId
        /// </summary>
        /// <param name="id"></param>
        /// <param name="docChannel"></param>
        /// <param name="cmDocumentId"></param>
        /// <returns></returns>
        public bool UpdateDocChannelCmDocumentIdAndDescriptionFromWebServices(int id, string docChannel, string cmDocumentId, string docDescription)
        {
            Doc.DocDataTable dt = GetDocById(id);

            if (dt.Rows.Count == 0) return false;

            Doc.DocRow r = dt[0];

            if (!string.IsNullOrEmpty(cmDocumentId))
            {
                r.OriginalCmDocumentId = cmDocumentId;
                r.CmDocumentId = cmDocumentId;
            }

            if (!string.IsNullOrEmpty(docChannel))
                r.DocChannel = docChannel;

            if (!string.IsNullOrEmpty(docDescription))
                r.Description = docDescription.Length > 50 ? docDescription.Substring(0, 50) : docDescription;


            if (!string.IsNullOrEmpty(cmDocumentId))
                r.CmDocumentId = cmDocumentId;



            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        #endregion

        #region Delete Methods
        public bool Delete(int id)
        {
            return Adapter.Delete(id) > 0;
        }

        public bool DeleteByDocSetId(int docSetId)
        {
            return Adapter.DeleteByDocSetId(docSetId) > 0;
        }

        public bool DeleteByOriginalSetIdSetIdNull(int originalDocSetId)
        {
            return Adapter.DeleteByOriginalSetIdSetIdNull(originalDocSetId) > 0;
        }
        #endregion
    }
}
