using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.SamplePageTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class SamplePageDb
    {
        private SamplePageTableAdapter _SamplePageTableAdapter = null;

        protected SamplePageTableAdapter Adapter
        {
            get
            {
                if (_SamplePageTableAdapter == null)
                    _SamplePageTableAdapter = new SamplePageTableAdapter();

                return _SamplePageTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SamplePage.SamplePageDataTable GetSamplePages()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public SamplePage.SamplePageDataTable GetSamplePageById(int id)
        {
            return Adapter.GetDataById(id);
        }

        public bool SamplePageExists(int id)
        {
            return GetSamplePageById(id).Rows.Count > 0;
        }

        /// <summary>
        /// Get the sample page by sample document id.
        /// </summary>
        /// <param name="sampleDocId"></param>
        /// <returns></returns>
        public SamplePage.SamplePageDataTable GetSamplePageBySampleDocId(int sampleDocId)
        {
            return Adapter.GetDataBySampleDocId(sampleDocId);
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="sampleDocId"></param>
        /// <param name="ocrText"></param>
        /// <param name="isOcr"></param>
        /// <returns></returns>
        public int Insert(int sampleDocId, string ocrText, bool isOcr)
        {
            SamplePage.SamplePageDataTable dt = new SamplePage.SamplePageDataTable();
            SamplePage.SamplePageRow r = dt.NewSamplePageRow();

            r.SampleDocId = sampleDocId;
            r.OcrText = ocrText;
            r.IsOcr = isOcr;

            dt.AddSamplePageRow(r);
            Adapter.Update(dt);
            int rowAffected = r.Id;
            return rowAffected;
        }
        #endregion

        #region Update Methods
        /// <summary>
        /// Update the sample page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="imagePageData"></param>
        /// <param name="searchablePdfData"></param>
        /// <param name="isOcr"></param>
        /// <returns></returns>
        public bool Update(int id, string ocrText, bool isOcr)
        {
            SamplePage.SamplePageDataTable dt = GetSamplePageById(id);

            if (dt.Rows.Count == 0) return false;

            SamplePage.SamplePageRow r = dt[0];

            r.OcrText = ocrText;
            r.IsOcr = isOcr;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update sample page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isOcr"></param>
        /// <returns></returns>
        public bool Update(int id, bool isOcr)
        {
            SamplePage.SamplePageDataTable dt = GetSamplePageById(id);

            if (dt.Rows.Count == 0) return false;

            SamplePage.SamplePageRow r = dt[0];

            r.IsOcr = isOcr;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }
        #endregion

        #region Delete Methods
        #endregion
    }
}
