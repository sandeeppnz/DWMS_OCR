using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.SampleDocTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class SampleDocDb
    {
        private SampleDocTableAdapter _SampleDocTableAdapter = null;

        protected SampleDocTableAdapter Adapter
        {
            get
            {
                if (_SampleDocTableAdapter == null)
                    _SampleDocTableAdapter = new SampleDocTableAdapter();

                return _SampleDocTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SampleDoc.SampleDocDataTable GetSampleDocs()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the sample document
        /// </summary>
        /// <returns></returns>
        public SampleDoc.SampleDocDataTable GetSampleDocById(int id)
        {
            return Adapter.GetDataById(id);
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SampleDoc.SampleDocDataTable GetSampleDocByPageId(int samplePageId)
        {
            return Adapter.GetDataByPageId(samplePageId);
        }

        /// <summary>
        /// Get the sample document
        /// </summary>
        /// <returns></returns>
        public SampleDoc.SampleDocDataTable GetSampleDocByCode(string docTypeCode)
        {
            return Adapter.GetDataByDocTypeCode(docTypeCode);
        }

        /// <summary>
        /// Check if the sample document has ben OCR'ed
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsSampleDocOcr(int id)
        {
            bool result = false;

            SampleDoc.SampleDocDataTable dt = GetSampleDocById(id);

            if (dt.Rows.Count > 0)
            {
                SampleDoc.SampleDocRow dr = dt[0];
                result = dr.IsOcr;
            }

            return result;
        }

        /// <summary>
        /// Get the sample doc id
        /// </summary>
        /// <param name="docTypeCode"></param>
        /// <returns></returns>
        public int GetSampleDocId(string docTypeCode)
        {
            int result = -1;

            SampleDoc.SampleDocDataTable dt = GetSampleDocByCode(docTypeCode);

            if (dt.Rows.Count > 0)
            {
                SampleDoc.SampleDocRow dr = dt[0];
                result = dr.Id;
            }

            return result;
        }

        /// <summary>
        /// Get the sample document count for the document type.
        /// </summary>
        /// <param name="docTypeCode">document type code</param>
        /// <returns>The number of documents for the document type</returns>
        public int GetSampleDocCount(string docTypeCode)
        {
            return GetSampleDocByCode(docTypeCode).Rows.Count;
        }

        /// <summary>
        /// Check if the number of sample documents for the document type
        /// exceeds the maximum allowed
        /// </summary>
        /// <param name="docTypeCode"></param>
        /// <returns></returns>
        public bool HasMaximumSampleDocument(string docTypeCode)
        {
            // Get the document count for the document type
            int docCount = GetSampleDocCount(docTypeCode);

            // Get the maximum sample documents for each document type
            ParameterDb parameterDb = new ParameterDb();
            int maxLimit = parameterDb.GetMaximumSampleDocsLimit();
            
            return docCount >= maxLimit;
        }

        /// <summary>
        /// Get the sample doc type code by id
        /// </summary>
        /// <param name="docTypeCode"></param>
        /// <returns></returns>
        public string GetSampleDocCode(int id)
        {
            string result = string.Empty;

            SampleDoc.SampleDocDataTable dt = GetSampleDocById(id);

            if (dt.Rows.Count > 0)
            {
                SampleDoc.SampleDocRow dr = dt[0];
                result = dr.DocTypeCode;
            }

            return result;
        }

        public SampleDoc.SampleDocDataTable GetDataWithHighestMatchCount()
        {
            return Adapter.GetDataWithHighestMatchCount();
        }

        public SampleDoc.SampleDocDataTable GetSampleDocByCodeOrderByMatchCount(string docTypeCode)
        {
            return Adapter.GetDataByDocTypeCodeOrderByMatchCount(docTypeCode);
        }

        /// <summary>
        /// Get the sample documents for the given document type sorted by the number of match count
        /// in the RelevanceRanking table.
        /// </summary>
        /// <param name="docTypeCode"></param>
        /// <returns></returns>
        public SampleDoc.SampleDocDataTable GetSampleDocWithWithoutMatchByCodeOrderByMatchCount(string docTypeCode)
        {
            return Adapter.GetDataWithWithoutMatchesByDocTypeCodeMatchCount(docTypeCode);
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="docTypeCode"></param>
        /// <param name="fileName"></param>
        /// <param name="fileData"></param>
        /// <param name="isOcr"></param>
        /// <returns></returns>
        public int Insert(string docTypeCode, string fileName, byte[] fileData, bool isOcr)
        {
            SampleDoc.SampleDocDataTable dt = new SampleDoc.SampleDocDataTable();
            SampleDoc.SampleDocRow r = dt.NewSampleDocRow();

            r.DocTypeCode = docTypeCode;
            r.FileName = fileName;
            r.FileData = fileData;
            r.IsOcr = isOcr;
            r.DateIn = DateTime.Now;

            dt.AddSampleDocRow(r);
            Adapter.Update(dt);
            int rowAffected = r.Id;
            return rowAffected;
        }
        #endregion

        #region Update Methods
        /// <summary>
        /// Update
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isOcr"></param>
        /// <returns></returns>
        public bool Update(int id, bool isOcr)
        {
            SampleDoc.SampleDocDataTable dt = GetSampleDocById(id);

            if (dt.Rows.Count == 0) return false;

            SampleDoc.SampleDocRow r = dt[0];

            r.IsOcr = isOcr;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update file name
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool UpdateFileName(int id, string fileName)
        {
            SampleDoc.SampleDocDataTable dt = GetSampleDocById(id);

            if (dt.Rows.Count == 0) return false;

            SampleDoc.SampleDocRow r = dt[0];

            r.FileName = fileName;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }
        #endregion

        #region Delete Methods
        /// <summary>
        /// Delete sample document by id.
        /// </summary>
        /// <param name="id">sample document id</param>
        /// <returns>True if deleting was successful.  False if otherwise.</returns>
        public bool Delete(int id)
        {
            return Adapter.Delete(id) > 0;
        }
        #endregion
    }
}
