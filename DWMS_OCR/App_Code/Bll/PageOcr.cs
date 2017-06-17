using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using DWMS_OCR.App_Code.Helper;
using DWMS_OCR.App_Code.Dal;
using NHunspell;
using System.Diagnostics;

namespace DWMS_OCR.App_Code.Bll
{
    class PageOcr
    {
        #region Object Properties
        Hunspell spellChecker;

        private int id;
        private string ocrText = string.Empty;
        private string ocrTextLower = string.Empty;
        private string docType = DocTypeEnum.Unidentified.ToString();
        private string nric = string.Empty;
        private string refNumber = string.Empty;
        private bool isDocStartPage = false; // Tells if the page is the start of a document

        private PageOcr prevPage = null;
        private PageOcr nextPage = null;
        private int rawPageNo;

        private int docSetId;

        private AppPersonal.AppPersonalDataTable appPersonalTable;
        private DocType.DocTypeDataTable docTypeDt;
        private ArrayList categorizationSampleDocs;
        private ArrayList topCategorizationSampleDocs;

        private int MINIMUM_ENGLISH_WORD_COUNT = 1;
        private decimal MINIMUM_ENGLISH_WORD_PERCENTAGE = 0.01m;
        private int KEYWORD_CHECK_SCOPE = 1;
        private decimal MINIMUM_SCORE = 0.001m;
        private int MINIMUM_WORD_LENGTH = 1;

        private bool isBlank;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string OcrText
        {
            set { ocrText = value; }
            get { return ocrText; }
        }

        public string OcrTextLower
        {
            set { ocrTextLower = value; }
            get { return ocrTextLower; }
        }

        public string DocumentType
        {
            set { docType = value; }
            get { return docType; }
        }

        public string RefNumber
        {
            set { refNumber = value; }
            get { return refNumber; }
        }

        public bool IsDocStartPage
        {
            set { isDocStartPage = value; }
            get { return isDocStartPage; }
        }

        public string Nric
        {
            set { nric = value; }
            get { return nric; }
        }

        public PageOcr PrevPage
        {
            get { return prevPage; }
            set { prevPage = value; }
        }

        public PageOcr NextPage
        {
            get { return nextPage; }
            set { nextPage = value; }
        }

        public int RawPageNo
        {
            get { return rawPageNo; }
            set { rawPageNo = value; }
        }

        public bool IsBlank
        {
            get { return isBlank; }
            set { isBlank = value; }
        }

        public PageOcr(int id, string ocrText, int rawPageNo, int docSetId,
            ArrayList categorizationSampleDocs, ArrayList topCategorizationSampleDocs,
            AppPersonal.AppPersonalDataTable appPersonalTable,
            DocType.DocTypeDataTable docTypeDt, Hunspell spellChecker,
            int minimumEnglishWordCount, decimal minimumEnglishWordPercentage, int keywordCheckScope,
            decimal minimumScore, int minimumWordLength)
        {
            this.spellChecker = spellChecker;

            Id = id;
            OcrText = ocrText;
            OcrTextLower = ocrText.ToLower();
            RawPageNo = rawPageNo;
            this.docSetId = docSetId;
            this.categorizationSampleDocs = categorizationSampleDocs;
            this.topCategorizationSampleDocs = topCategorizationSampleDocs;
            this.appPersonalTable = appPersonalTable;
            this.docTypeDt = docTypeDt;

            this.MINIMUM_ENGLISH_WORD_COUNT = minimumEnglishWordCount;
            this.MINIMUM_ENGLISH_WORD_PERCENTAGE = minimumEnglishWordPercentage;
            this.KEYWORD_CHECK_SCOPE = keywordCheckScope;
            this.MINIMUM_SCORE = minimumScore;
            this.MINIMUM_WORD_LENGTH = minimumWordLength;

            // Set the doc type
            SetDocumentType();

            // Set the HLE number (for HLE Documents)
            SetHleNumber();

            // Set the NRIC
            SetNric();
        }
        #endregion

        #region Private Methods
        #region Determine Document Type
        /// <summary>
        /// Set the document type of the page
        /// </summary>
        private void SetDocumentType()
        {
            try
            {
                isBlank = true;

                int ocrTextLen = OcrText.Trim().Replace(Environment.NewLine, "").Replace(" ", "").Length;

                ArrayList englishWords = new ArrayList();

                bool isValidForRelevanceRanking = CategorizationHelpers.IsValidTextForRelevanceRanking(OcrText.Trim(), spellChecker, MINIMUM_WORD_LENGTH,
                    MINIMUM_ENGLISH_WORD_COUNT, MINIMUM_ENGLISH_WORD_PERCENTAGE, ref englishWords);

                // Get the OcrFailed value.  If it is true, set IsBlank to false so as not to group with blank pages
                RawPageDb rawPageDb = new RawPageDb();
                if (rawPageDb.IsOcrFailed(id) || (ocrTextLen > Constants.MIN_STR_LENGTH) ||
                    (isValidForRelevanceRanking && ocrTextLen < Constants.MIN_STR_LENGTH))
                    isBlank = false;

                if (isValidForRelevanceRanking)
                {
                    isBlank = false;

                    DocumentType = GetDocTypeByRelevanceRanking(englishWords);
                }

                if (String.IsNullOrEmpty(DocumentType))
                {
                    DocumentType = DocTypeEnum.Unidentified.ToString();
                }
            }
            catch (Exception e)
            {
                // Log the warning message
                string warningSummary = string.Format("Warning (PageOcr.SetDocumentType): SetId={0}, RawPageId={1}, Message={2}, StackTrace={3}",
                    docSetId.ToString(), id.ToString(), e.Message, e.StackTrace);

                Util.DWMSLog(string.Empty, warningSummary, EventLogEntryType.Warning);

                // Set the document type as Unidentified
                DocumentType = DocTypeEnum.Unidentified.ToString();
            }
        }

        /// <summary>
        /// Get the document type by relevance ranking
        /// </summary>
        /// <param name="sourceArr"></param>
        /// <returns></returns>
        private string GetDocTypeByRelevanceRanking(ArrayList sourceArr)
        {
            SamplePageDb samplePageDb = new SamplePageDb();

            if (this.categorizationSampleDocs.Count == 0)
            {
                return null;
            }

            // Declare a dictionary to store Sample Page ID and score pairs
            Dictionary<int, decimal> dir = new Dictionary<int, decimal>();

            foreach (CategorizationSamplePageDetails r in this.topCategorizationSampleDocs)
            {
                decimal score = ComputeRelevanceScore(sourceArr, r.EnglishWords);
                dir.Add(r.Id, score);
            }

            // Sort dictionary by matching score from high to low
            var sortedDir = (from entry in dir orderby entry.Value descending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);
            int? samplePageId = null;

            SampleDocDb sampleDocDb = new SampleDocDb();
            SampleDoc.SampleDocDataTable sampleDocTable;

            // Check for each of the top N samples if keyword condition is met
            HashSet<string> uniqueDocTypes = new HashSet<string>();

            foreach (var item in sortedDir)
            {
                if (item.Value < MINIMUM_SCORE)
                {
                    break;
                }

                string docTypeCode = null;
                sampleDocTable = sampleDocDb.GetSampleDocByPageId(item.Key);

                if (sampleDocTable.Count > 0)
                {
                    docTypeCode = sampleDocTable[0].DocTypeCode;
                }

                bool containsRuleMet = CheckRule(true, OcrText, docTypeCode);
                bool notContainsRuleMet = CheckRule(false, OcrText, docTypeCode);

                if (!notContainsRuleMet)
                {
                    if (docTypeCode != null)
                        uniqueDocTypes.Add(docTypeCode);
                }

                if (uniqueDocTypes.Count > KEYWORD_CHECK_SCOPE)
                {
                    break;
                }

                if (!notContainsRuleMet)
                {
                    if (containsRuleMet)
                    {
                        samplePageId = item.Key;                        
                        break;
                    }
                    else
                    {
                        if (samplePageId == null)
                        {
                            samplePageId = item.Key;
                        }
                    }
                }
            }

            if (samplePageId == null)
            {
                samplePageId = ((Dictionary<int, decimal>)sortedDir).First().Key;
            }

            if (((Dictionary<int, decimal>)sortedDir)[samplePageId.Value] < MINIMUM_SCORE)
            {
                return null;
            }

            sampleDocTable = sampleDocDb.GetSampleDocByPageId(samplePageId.Value);

            if (sampleDocTable.Count == 0)
            {
                return null;
            }

            // Get the sample doc type here
            // For all the sample pages of the doc type, get the rank and use the highest ranked sample page
            // Declare a dictionary to store Sample Page ID and score pairs
            string docTypeCode2 = sampleDocTable[0].DocTypeCode;
            ArrayList samplePageDetails = new ArrayList();

            // Find the sample pages for the document type
            foreach(CategorizationSampleDoc r in this.categorizationSampleDocs)
            {
                if (r.DocTypeCode.ToUpper().Equals(docTypeCode2.ToUpper()))
                {
                    samplePageDetails = r.SampleDocDetails;
                    break;
                }
            }

            Dictionary<int, decimal> dir2 = new Dictionary<int, decimal>();

            foreach (CategorizationSamplePageDetails r in samplePageDetails)
            {
                decimal score = ComputeRelevanceScore(sourceArr, r.EnglishWords);
                dir2.Add(r.Id, score);
            }            

            // Sort dictionary by matching score from high to low
            Dictionary<int, decimal> sortedDir2 = (from entry in dir2 orderby entry.Value descending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);

            //foreach (KeyValuePair<int, decimal> dir3 in sortedDir2)
            //{
            //    Util.DWMSLog("", String.Format("{0} PID {1}: {2}", docTypeCode2, dir3.Key, dir3.Value), EventLogEntryType.Error);
            //}

            int samplePageId2 = sortedDir2.First().Key;

            sampleDocTable = sampleDocDb.GetSampleDocByPageId(samplePageId.Value);

            if (sampleDocTable.Count == 0)
            {
                return null;
            }

            // Insert a RelevanceRanking record for the page
            RelevanceRankingDb relevanceRankingDb = new RelevanceRankingDb();
            relevanceRankingDb.Insert(sampleDocTable[0].Id, DateTime.Now, false, Id);

            return sampleDocTable[0].DocTypeCode;
        }

        /// <summary>
        /// Compute the score of the matching of the raw page to the sample page
        /// </summary>
        /// <param name="sourceArr"></param>
        /// <param name="sampleArr"></param>
        /// <returns></returns>
        private decimal ComputeRelevanceScore(ArrayList sourceArr, ArrayList sampleArr)
        {
            decimal score = 0;

            decimal sourceWordCount = 0;

            foreach (string word in sourceArr)
            {
                if (word.Length >= MINIMUM_WORD_LENGTH)
                    sourceWordCount++;
            }

            if (sourceWordCount == 0)
                return score;

            decimal sampleWordCount = 0;

            foreach (string word in sampleArr)
            {
                if (word.Length >= MINIMUM_WORD_LENGTH)
                    sampleWordCount++;
            }

            if (sampleWordCount == 0)
                return score;

            var hash = new HashSet<string>((String[])sampleArr.ToArray(typeof(string)));

            decimal matchCount = 0;

            foreach (string word in sourceArr)
            {
                if (word.Length >= MINIMUM_WORD_LENGTH && hash.Contains(word))
                    matchCount++;
            }

            score = (matchCount / sourceWordCount) * (matchCount / sampleWordCount);
            return score;
        }

        /// <summary>
        /// Check the keywork rule
        /// </summary>
        /// <param name="isInclusive"></param>
        /// <param name="ocrText"></param>
        /// <param name="docTypeCode"></param>
        /// <returns></returns>
        private bool CheckRule(bool isInclusive, string ocrText, string docTypeCode)
        {
            if (string.IsNullOrEmpty(ocrText))
                return false;

            if (docTypeCode == null || string.IsNullOrEmpty(docTypeCode))
                return false;

            string ocrTextLower = ocrText.ToLower();
            bool result = false;

            List<string[]> containsKeyWordsList = new List<string[]>();

            // Get the rules and keywords for the document type
            CategorisationRuleDb catRuleDb = new CategorisationRuleDb();
            CategorisationRuleKeywordDb catRuleKeywordDb = new CategorisationRuleKeywordDb();

            CategorisationRule.CategorisationRuleRow catRule = catRuleDb.GetCategorisationRulesRow(docTypeCode);

            if (catRule != null)
            {
                CategorisationRuleKeyword.CategorisationRuleKeywordRow[] catRuleKeywordRows =
                    catRuleKeywordDb.GetCategorisationRuleKeywordsRows(catRule.Id);

                foreach (CategorisationRuleKeyword.CategorisationRuleKeywordRow catRuleKeyword in catRuleKeywordRows)
                {
                    if (catRuleKeyword.ContainsFilter == isInclusive)
                    {
                        string keyword = catRuleKeyword.Keyword;

                        // Split the keyword if it contains the "&&" logical operator
                        if (keyword.Contains(Constants.KeywordSeperator))
                        {
                            string[] keywordsArray = keyword.Split(new string[] { Constants.KeywordSeperator }, StringSplitOptions.RemoveEmptyEntries);

                            containsKeyWordsList.Add(keywordsArray); // Add the keyword to the contains word list
                        }
                        else
                        {
                            string[] temp = new string[] { keyword };

                            containsKeyWordsList.Add(temp); // Add the keyword to the contains word list
                        }

                    }
                }

                // If the contains or not contains list has data, 
                // proceed to check with the contents
                if (containsKeyWordsList.Count > 0)
                {
                    bool resultForContains = false;

                    // Evaluate the conditions for the contains filters
                    foreach (string[] keywordsArray in containsKeyWordsList)
                    {
                        bool temp = true;

                        foreach (string keyword in keywordsArray)
                        {
                            if (keyword.StartsWith("[") && keyword.EndsWith("]"))
                            {
                                if (keyword.Contains(KeywordVariableEnum.HLE_Number.ToString()))
                                    temp = temp && CategorizationHelpers.HasVariable(ocrTextLower,
                                        (KeywordVariableEnum)Enum.Parse(typeof(KeywordVariableEnum),
                                        keyword.Replace("[", string.Empty).Replace("]", string.Empty))); // Check if HLE Number is found
                                else if (keyword.Contains(KeywordVariableEnum.NRIC.ToString()))
                                    temp = temp && CategorizationHelpers.HasVariable(ocrTextLower,
                                        (KeywordVariableEnum)Enum.Parse(typeof(KeywordVariableEnum),
                                        keyword.Replace("[", string.Empty).Replace("]", string.Empty))); // Check if NRIC is found
                                else
                                    temp = temp && CategorizationHelpers.HasKeyword(ocrTextLower, keyword); // Check if the keyword is found
                            }
                            else
                                temp = temp && CategorizationHelpers.HasKeyword(ocrTextLower, keyword); // Check if the keyword is found
                        }

                        resultForContains = resultForContains || temp;
                    }

                    result = resultForContains;
                }
            }

            return result;
        }
        #endregion

        #region Determine NRIC
        /// <summary>
        /// Set the NRIC
        /// </summary>
        private void SetNric()
        {
            try
            {
                if (!DocumentType.ToUpper().Equals(DocTypeEnum.HLE.ToString().ToUpper()) &&
                        !DocumentType.ToUpper().Equals(DocTypeEnum.Resale.ToString().ToUpper()) &&
                        !DocumentType.ToUpper().Equals(DocTypeEnum.Sales.ToString().ToUpper()) &&
                        !DocumentType.ToUpper().Equals(DocTypeEnum.SERS.ToString().ToUpper()))
                {
                    string nricTemp = string.Empty;

                    // Get the lines and words of the OCR text
                    string[] lines = OcrTextLower.Split(Constants.NewLineSeperators, StringSplitOptions.RemoveEmptyEntries);
                    ArrayList wordsList = new ArrayList();

                    foreach (string line in lines)
                    {
                        //string[] words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string[] words = line.Split(Constants.OcrTextLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string word in words)
                        {
                            wordsList.Add(word);
                        }
                    }

                    // Get the NRIC using the match to existing NRIC
                    bool hasNric = CheckMatchUsingNric(wordsList, out nricTemp);

                    // Get the NRIC using the match to existing name
                    if (!hasNric)
                    {
                        hasNric = CheckMatchUsingName(wordsList, out nricTemp);
                    }

                    //// Get the NRIC from the word
                    //if (!hasNric)
                    //{
                    //    nricTemp = CategorizationHelpers.GetNricFromText(OcrTextLower);
                    //}

                    // Assign the NRIC if found
                    if (!String.IsNullOrEmpty(nricTemp))
                        Nric = nricTemp;
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Check the match using NRIC
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="wordList"></param>
        /// <param name="applicantListDt"></param>
        /// <returns></returns>
        private bool CheckMatchUsingNric(ArrayList wordList, out string nric)
        {
            bool result = false;
            nric = string.Empty;

            foreach (AppPersonal.AppPersonalRow applicant in appPersonalTable)
            {
                foreach (string word in wordList)
                {
                    // Check the match using NRIC
                    if (LevenshteinDistance.IsMatch(applicant.Nric.ToLower(), word, 2))
                    {
                        result = true;
                        nric = applicant.Nric.ToUpper();
                        break;
                    }
                }

                if (result)
                    break;
            }

            return result;
        }

        /// <summary>
        /// Check the match using Name
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="wordList"></param>
        /// <param name="applicantListDt"></param>
        /// <returns></returns>
        private bool CheckMatchUsingName(ArrayList wordList, out string nric)
        {
            bool result = false;
            nric = string.Empty;

            foreach (AppPersonal.AppPersonalRow applicant in appPersonalTable)
            {
                if (applicant.IsNameNull())
                    continue;

                // Split the name of the current applicant in the list
                string[] nameSplit = applicant.Name.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                int matchCount = 0;
                foreach (string partialName in nameSplit)
                {
                    // Compare each partialName to the words
                    foreach (string word in wordList)
                    {
                        // Check the match using patial name
                        if (LevenshteinDistance.IsMatch(partialName, word, 1))
                        {
                            matchCount++;
                            break;
                        }
                    }

                    if (matchCount == 2)
                    {
                        result = true;
                        nric = applicant.Nric.ToUpper();
                        break;
                    }
                }

                if (result)
                    break;
            }

            return result;
        }
        #endregion

        /// <summary>
        /// Set the HLE number
        /// </summary>
        private void SetHleNumber()
        {
            RefNumber = CategorizationHelpers.GetHleNumberFromOcr(this.ocrText).ToUpper();
        }
        #endregion
    }
}
