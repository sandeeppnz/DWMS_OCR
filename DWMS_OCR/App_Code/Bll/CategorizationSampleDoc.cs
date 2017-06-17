using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class CategorizationSampleDoc
    {
        public string DocTypeCode { get; set; } // Document type code
        public ArrayList SampleDocDetails { get; set; } // Container of all the sample documents for the document type
        public ArrayList TopSampleDocDetails { get; set; } // Container of the top sample documents for the document type

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="docTypeCode"></param>
        public CategorizationSampleDoc(string docTypeCode)
        {
            DocTypeCode = docTypeCode;
            SampleDocDetails = new ArrayList();
            TopSampleDocDetails = new ArrayList();
        }

        /// <summary>
        /// Get all the sample pages for the document type.
        /// </summary>
        /// <param name="MINIMUM_WORD_LENGTH"></param>
        /// <param name="MINIMUM_ENGLISH_WORD_COUNT"></param>
        /// <param name="MINIMUM_ENGLISH_WORD_PERCENTAGE"></param>
        public void GetSamplePages(int MINIMUM_WORD_LENGTH, int MINIMUM_ENGLISH_WORD_COUNT, decimal MINIMUM_ENGLISH_WORD_PERCENTAGE)
        {
            SampleDocDb sampleDocDb = new SampleDocDb();
            SamplePageDb samplePageDb = new SamplePageDb();

            // Get the sample documents for the document type and sort them
            // by the number of matches in the relevance ranking.  The first row being the sample document with the most matches.
            SampleDoc.SampleDocDataTable sampleDocDt = sampleDocDb.GetSampleDocWithWithoutMatchByCodeOrderByMatchCount(DocTypeCode);

            int rank = 1;
            foreach (SampleDoc.SampleDocRow sampleDocDr in sampleDocDt)
            {
                // Get the sample pages for the sample document
                SamplePage.SamplePageDataTable samplePageDt = samplePageDb.GetSamplePageBySampleDocId(sampleDocDr.Id);

                foreach (SamplePage.SamplePageRow samplePage in samplePageDt)
                {
                    CategorizationSamplePageDetails samplePageForCategorization = new CategorizationSamplePageDetails();
                    ArrayList englishWords = new ArrayList();

                    // Check if the sample page has valid contents
                    if (CategorizationHelpers.IsValidTextForRelevanceRanking(samplePage.OcrText, MINIMUM_WORD_LENGTH, MINIMUM_ENGLISH_WORD_COUNT, MINIMUM_ENGLISH_WORD_PERCENTAGE, ref englishWords))
                    {
                        if (englishWords.Count > 0)
                        {
                            // Add the samplepage to the collection
                            samplePageForCategorization.Id = samplePage.Id;
                            samplePageForCategorization.EnglishWords = englishWords;
                            samplePageForCategorization.RelevanceScore = 0.0m;
                            samplePageForCategorization.Rank = rank++;

                            SampleDocDetails.Add(samplePageForCategorization);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the top sample pages for the document type
        /// </summary>
        public void GetTopSamplePages()
        {
            if (SampleDocDetails.Count > 0)
            {
                // Compute the number of sample pages that will be included in the list
                ParameterDb parameterDb = new ParameterDb();
                double samplePagesPercentage = parameterDb.GetTopSamplePagesPercentage();
                int topSamplePageCount = Convert.ToInt32((SampleDocDetails.Count * samplePagesPercentage));

                int cnt = 1;
                foreach (CategorizationSamplePageDetails sampleDocDetail in SampleDocDetails)
                {
                    if (cnt > topSamplePageCount)
                        break;

                    // Add the sample page to the top sample pages list
                    TopSampleDocDetails.Add(sampleDocDetail);

                    cnt++;
                }
            }
        }
    }

    class CategorizationSamplePageDetails
    {
        public int Id { get; set; }
        public ArrayList EnglishWords { get; set; }
        public decimal RelevanceScore { get; set; }
        public int Rank { get; set; }
    }
}
