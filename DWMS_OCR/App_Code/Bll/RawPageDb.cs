using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.RawPageTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class RawPageDb
    {
        private RawPageTableAdapter _RawPageTableAdapter = null;

        protected RawPageTableAdapter Adapter
        {
            get
            {
                if (_RawPageTableAdapter == null)
                    _RawPageTableAdapter = new RawPageTableAdapter();

                return _RawPageTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get all the raw pages.
        /// </summary>
        /// <returns>RawPage table</returns>
        public RawPage.RawPageDataTable GetRawPages()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the raw page by id.
        /// </summary>
        /// <param name="id">Raw page id</param>
        /// <returns>RawPage table</returns>
        public RawPage.RawPageDataTable GetRawPageById(int id)
        {
            return Adapter.GetDataById(id);
        }

        /// <summary>
        /// Get the raw page by raw file id.
        /// </summary>
        /// <param name="rawFileId">Raw file id</param>
        /// <returns>RawPage table</returns>
        public RawPage.RawPageDataTable GetRawPageByRawFileId(int rawFileId)
        {
            return Adapter.GetDataByRawFileId(rawFileId);
        }

        /// <summary>
        /// Get the raw page by doc id.
        /// </summary>
        /// <param name="docId">Doc id</param>
        /// <returns>RawPage table</returns>
        public RawPage.RawPageDataTable GetRawPageByDocId(int docId)
        {
            return Adapter.GetDataByDocId(docId);
        }

        /// <summary>
        /// Count the pages for the set.
        /// </summary>
        /// <param name="docSetId">SEt id</param>
        /// <returns>Number of pages of the set</returns>
        public int CountPagesByDocSetId(int docSetId)
        {
            return RawPageDs.CountOcrPagesBySet(docSetId);
        }

        /// <summary>
        /// Get the text of the document.
        /// </summary>
        /// <param name="docId">Document id</param>
        /// <returns>Document contents</returns>
        public string GetDocContents(int docId)
        {
            StringBuilder docContent = new StringBuilder();

            RawPage.RawPageDataTable rawPageTable = GetRawPageByDocId(docId);

            foreach(RawPage.RawPageRow rawPage in rawPageTable)
            {
                docContent.Append(rawPage.OcrText + Environment.NewLine);
            }

            return docContent.ToString();
        }
        
        /// <summary>
        /// Check if the OCR failed for the page.
        /// </summary>
        /// <param name="id">Raw page id</param>
        /// <returns>True if OCR failed.  False if otherwise</returns>
        public bool IsOcrFailed(int id)
        {
            bool result = false;

            RawPage.RawPageDataTable dt = GetRawPageById(id);

            if (dt.Rows.Count > 0)
            {
                RawPage.RawPageRow dr = dt[0];
                result = dr.OcrFailed;
            }

            return result;
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert raw page.
        /// </summary>
        /// <param name="rawFileId">Raw file id</param>
        /// <param name="rawPageNo">Raw page number</param>
        /// <param name="pageData">Page byte data</param>
        /// <param name="ocrText">OCR text</param>
        /// <param name="imagePageData">Thumbnail byte data</param>
        /// <param name="searchablePdfData">Searchable pdf byte data</param>
        /// <param name="isOcr">Ocr flag</param>
        /// <returns>Raw page id</returns>
        public int Insert(int rawFileId, int rawPageNo, byte[] pageData, string ocrText, byte[] imagePageData, byte[] searchablePdfData, bool isOcr)
        {
            RawPage.RawPageDataTable dt = new RawPage.RawPageDataTable();
            RawPage.RawPageRow r = dt.NewRawPageRow();

            r.RawFileId = rawFileId;
            r.RawPageNo = rawPageNo;
            r.PageData = pageData;
            r.OcrText = ocrText;
            r.DocPageNo = 0;
            r.ImagePageData = imagePageData;
            r.SearchablePdf = searchablePdfData;
            r.IsOcr = isOcr;

            dt.AddRawPageRow(r);
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
        /// <param name="templateCode"></param>
        /// <param name="templateDescription"></param>
        /// <param name="subject"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public bool Update(int id, int docId, int docPageNo)
        {
            RawPage.RawPageDataTable dt = GetRawPageById(id);

            if (dt.Rows.Count == 0) return false;

            RawPage.RawPageRow r = dt[0];

            r.DocId = docId;
            r.DocPageNo = docPageNo;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update the raw page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="imagePageData"></param>
        /// <param name="searchablePdfData"></param>
        /// <param name="isOcr"></param>
        /// <returns></returns>
        public bool Update(int id, string ocrText, byte[] imagePageData, byte[] searchablePdfData, bool isOcr)
        {
            RawPage.RawPageDataTable dt = GetRawPageById(id);

            if (dt.Rows.Count == 0) return false;

            RawPage.RawPageRow r = dt[0];

            r.OcrText = ocrText;
            r.ImagePageData = imagePageData;
            r.SearchablePdf = searchablePdfData;
            r.IsOcr = isOcr;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update the raw page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="imagePageData"></param>
        /// <param name="searchablePdfData"></param>
        /// <param name="isOcr"></param>
        /// <returns></returns>
        public bool Update(int id, string ocrText, bool isOcr)
        {
            RawPage.RawPageDataTable dt = GetRawPageById(id);

            if (dt.Rows.Count == 0) return false;

            RawPage.RawPageRow r = dt[0];

            r.OcrText = ocrText;
            r.IsOcr = isOcr;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update the raw page with autorotated value
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ocrText"></param>
        /// <param name="isOcr"></param>
        /// <param name="autoRotated"></param>
        /// <returns></returns>
        public bool Update(int id, string ocrText, bool isOcr, byte autoRotated)
        {
            RawPage.RawPageDataTable dt = GetRawPageById(id);

            if (dt.Rows.Count == 0) return false;

            RawPage.RawPageRow r = dt[0];

            r.OcrText = ocrText;
            r.IsOcr = isOcr;
            r.AutoRotated = autoRotated;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update set IsOcr
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isOcr"></param>
        /// <returns></returns>
        public bool Update(int id, bool isOcr)
        {
            RawPage.RawPageDataTable dt = GetRawPageById(id);

            if (dt.Rows.Count == 0) return false;

            RawPage.RawPageRow r = dt[0];

            r.IsOcr = isOcr;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update set OcrFailed
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ocrFailed"></param>
        /// <returns></returns>
        public bool UpdateOcrFailed(int id, bool ocrFailed)
        {
            RawPage.RawPageDataTable dt = GetRawPageById(id);

            if (dt.Rows.Count == 0) return false;

            RawPage.RawPageRow r = dt[0];

            r.OcrFailed = ocrFailed;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        public bool UpdateSetDocIdNull(int id)
        {
            RawPage.RawPageDataTable dt = GetRawPageById(id);

            if (dt.Rows.Count == 0) return false;

            RawPage.RawPageRow r = dt[0];

            r.SetDocIdNull();

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        #endregion

        #region Delete Methods
        /// <summary>
        /// Delete by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Delete(int id)
        {
            return Adapter.Delete(id) > 0;
        }

        /// <summary>
        /// Delete by raw file id
        /// </summary>
        /// <param name="rawFileId"></param>
        /// <returns></returns>
        public bool DeleteRawPagesByRawFileId(int rawFileId)
        {
            return Adapter.DeleteRawPagesByRawFileId(rawFileId) > 0;
        }
        #endregion
    }
}
