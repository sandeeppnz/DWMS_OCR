using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.RelevanceRankingTableAdapters;
using DWMS_OCR.App_Code.Dal;
using System.Data;
using System.IO;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Bll
{
    class RelevanceRankingDb
    {
        private RelevanceRankingTableAdapter _RelevanceRankingTableAdapter = null;

        protected RelevanceRankingTableAdapter Adapter
        {
            get
            {
                if (_RelevanceRankingTableAdapter == null)
                    _RelevanceRankingTableAdapter = new RelevanceRankingTableAdapter();

                return _RelevanceRankingTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public RelevanceRanking.RelevanceRankingDataTable GetRelevanceRankings()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get data by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RelevanceRanking.RelevanceRankingDataTable GetRelevanceRankingById(int id)
        {
            return Adapter.GetDataById(id);
        }

        /// <summary>
        /// Get data by raw page id
        /// </summary>
        /// <param name="rawPageId"></param>
        /// <returns></returns>
        public RelevanceRanking.RelevanceRankingDataTable GetRelevanceRankingByRawPageId(int rawPageId)
        {
            return Adapter.GetDataByRawPageId(rawPageId);
        }

        /// <summary>
        /// Get all the sample documents with ranks.  Rank is computed by dividing the 
        /// No of matches in Relevance Ranking  with the difference between 
        /// the DateIn of the SampleDoc and the current date
        /// </summary>
        /// <param name="docTypeCode">document type code</param>
        /// <returns>Sample document table</returns>
        public DataTable GetAllSampleDocWithRanks(string docTypeCode)
        {
            return RelevanceRankingDs.GetAllSampleDocWithRanks(docTypeCode);
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="sampleDocId"></param>
        /// <param name="categorizationDate"></param>
        /// <param name="isMatch"></param>
        /// <param name="rawPageId"></param>
        /// <returns></returns>
        public int Insert(int sampleDocId, DateTime categorizationDate, bool isMatch, int rawPageId)
        {
            RelevanceRanking.RelevanceRankingDataTable dt = new RelevanceRanking.RelevanceRankingDataTable();
            RelevanceRanking.RelevanceRankingRow r = dt.NewRelevanceRankingRow();

            r.SampleDocId = sampleDocId;
            r.CategorizationDate = categorizationDate;
            r.IsMatch = isMatch;
            r.RawPageId = rawPageId;

            dt.AddRelevanceRankingRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }
        #endregion

        #region Update Methods
        /// <summary>
        /// Update
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isMatch"></param>
        /// <returns></returns>
        public bool Update(int id, bool isMatch)
        {
            RelevanceRanking.RelevanceRankingDataTable dt = GetRelevanceRankingById(id);

            if (dt.Rows.Count == 0) return false;

            RelevanceRanking.RelevanceRankingRow r = dt[0];

            r.IsMatch = isMatch;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }
        #endregion

        #region Delete Methods
        /// <summary>
        /// Delete the least sample document for each document type.
        /// </summary>
        /// <param name="docTypeCode">Document type code</param>
        /// <returns>True if the deleting was successful.  False if otherwise</returns>
        public bool DeleteLeastSampleDocument(string docTypeCode)
        {
            bool result = false;

            SampleDocDb sampleDocDb = new SampleDocDb();

            // Get learning period
            ParameterDb parameterDb = new ParameterDb();
            int MAXIMUM_SAMPLE_DOCUMENT_COUNT = parameterDb.GetMaximumSampleDocsLimit();

            // Check if the sample document count for the document type 
            // is greater than the maximum setting value
            if (sampleDocDb.HasMaximumSampleDocument(docTypeCode))
            {
                // Get all the sample documents for the document type sorted by rank
                DataTable dt = GetAllSampleDocWithRanks(docTypeCode);

                if (dt.Rows.Count > 0)
                {
                    // Delete sample documents that exceed the maximum allowable
                    if (dt.Rows.Count > MAXIMUM_SAMPLE_DOCUMENT_COUNT)
                    {
                        #region If the sample document is greater than the maximum allowable, delete sampled docs by rank
                        int cnt = 1;
                        foreach (DataRow dr in dt.Rows)
                        {
                            // Remove the sample documents beyond the MAX sample document count
                            if (cnt > MAXIMUM_SAMPLE_DOCUMENT_COUNT)
                            {
                                // Delete the row that exceeds the limit
                                int id = int.Parse(dr["Id"].ToString());

                                // Delete the sample document
                                result = sampleDocDb.Delete(id);

                                try
                                {
                                    if (result)
                                    {
                                        // Delete the sample document file if it exists
                                        //Directory.Delete(Util.GetSampleDocDirPath(id));
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }

                            cnt++;
                        }
                        #endregion
                    }
                }
            }         

            return true;
        }
        #endregion
    }
}
