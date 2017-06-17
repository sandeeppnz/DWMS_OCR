using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections;
using System.Web;
using DWMS_OCR.App_Code.Helper;
using System.Text.RegularExpressions;
using DWMS_OCR.App_Code.Dal;
using System.Data;
using NHunspell;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace DWMS_OCR.App_Code.Bll
{
    class CategorizationManagerForDoc
    {
        private int MINIMUM_ENGLISH_WORD_COUNT = 1;
        private decimal MINIMUM_ENGLISH_WORD_PERCENTAGE = 0.01m;
        private int KEYWORD_CHECK_SCOPE = 1;
        private decimal MINIMUM_SCORE = 0.001m;
        private int MINIMUM_WORD_LENGTH = 1;

        public CategorizationManagerForDoc()
        {
            ParameterDb parameterDb = new ParameterDb();
            this.MINIMUM_ENGLISH_WORD_COUNT = parameterDb.GetMinimumEnglishWordCount();
            this.MINIMUM_ENGLISH_WORD_PERCENTAGE = parameterDb.GetMinimumEnglishWordPercentage();
            this.KEYWORD_CHECK_SCOPE = parameterDb.GetKeywordCheckScope();
            this.MINIMUM_SCORE = parameterDb.GetMinimumSampleScore();
            this.MINIMUM_WORD_LENGTH = parameterDb.GetMinimumWordLength();
        }

        /// <summary>
        /// Start the categorization process
        /// </summary>
        /// <param name="dirPath"></param>
        public bool StartCategorization(int docSetId, ArrayList categorizationSampleDocs)
        {
            ParameterDb parameterDb = new ParameterDb();
            bool logging = parameterDb.Logging();
            bool detailLogging = parameterDb.DetailLogging();
            bool result = false;
            try
            {
                // Get the reference numbers of the set
                ArrayList refNoList = GetRefNos(docSetId);

                // Insert the AppPersonal records
                InsertAppPersonalRecords(docSetId, refNoList);

                // Get the AppPersonal records for the given set
                AppPersonalDb appPersonalDb = new AppPersonalDb();
                AppPersonal.AppPersonalDataTable appPersonalTable = GetAppPersonalTable(docSetId);

                // Get document types
                DocTypeDb docTypeDb = new DocTypeDb();
                DocType.DocTypeDataTable docTypeDt = docTypeDb.GetDocTypes();

                // Get the raw files of the set
                RawFileDb rawFileDb = new RawFileDb();
                RawFile.RawFileDataTable rawFileDt = rawFileDb.GetRawFilesByDocSetId(docSetId);
                bool verifiedSet = true;

                // Get the top categorization sample pages
                ArrayList topCategorizationSampleDocs = GetTopSamplePages(categorizationSampleDocs);

                foreach (RawFile.RawFileRow rawFile in rawFileDt)
                {
                    //this is the selection where to choose from each rawfile of specific SetId, based on the RawFile.SkipCategorization
                    if (!rawFile.SkipCategorization) // in RawFile table, SkipCategorization is FALSE ( Not Skipping Categorization )
                    {
                        if (logging) Util.DWMSLog("CategorizationManager.StartCategorization", "Webservice no skip categorisation", EventLogEntryType.Warning);
                        ArrayList pageList = GetPages(rawFile.Id, docSetId, appPersonalTable, categorizationSampleDocs, topCategorizationSampleDocs, docTypeDt); // Arraylist for the Page object
                        verifiedSet = false;

                        if (pageList.Count > 0)
                        {
                            ArrayList personalNameNrics = new ArrayList();
                            ArrayList docList = new ArrayList();

                            LinkAllPages(ref pageList); // Link all pages

                            MergeBlankPages(ref pageList); // Merge all the blank and unidentified pages

                            BreakLinksSimilarPages(ref pageList); // Break the pages and group according to Document Types

                            LinkSimilarHlePagesNotInSequence(ref pageList); // Link similar HLE pages that are not in sequence

                            LinkSimilarResalePagesNotInSequence(ref pageList); // Link similar Resale pages that are not in sequence

                            LinkSimilarSalesPagesNotInSequence(ref pageList); // Link similar Sales pages that are not in sequence

                            LinkSimilarSersPagesNotInSequence(ref pageList); // Link similar SERS pages that are not in sequence

                            CreateDocuments(ref pageList, ref docList); // Create the Document objects

                            CreatePersonalList(ref docList); // Create the meta data and personal list from the documents

                            SaveDocs(docSetId, ref docList, appPersonalTable, rawFile.FileName); // Save the docs

                        }

                    }
                    else
                    {// in RawFile table, SkipCategorization is TRUE (Skip Categorization), but still need to do small categorization
                        if (verifiedSet)
                        {
                            if (logging) Util.DWMSLog("CategorizationManager.StartCategorization", "Webservice Skip categorisation (verified)", EventLogEntryType.Warning);
                            verifiedSet = CategorizeDocFromWebService(docSetId, rawFile.FileName);
                        }
                        else
                        {
                            if (logging) Util.DWMSLog("CategorizationManager.StartCategorization", "Webservice Skip categorisation not verified", EventLogEntryType.Warning);
                            CategorizeDocFromWebService(docSetId, rawFile.FileName);
                        }
                    }
                }

                // Update the status of the set
                // If there is a verification officer assigned to the set, the status will be Pending Verification.
                // Else, it will New
                DocSetDb docSetDb = new DocSetDb();

                if (verifiedSet)
                {
                    docSetDb.UpdateSetStatus(docSetId, SetStatusEnum.Verified);
                    try
                    {
                        SendEmailToOic(docSetId);
                    }
                    catch (Exception e)
                    {
                        string errorSummary = string.Format("Send email exception: Message={0}, StackTrace={1}", e.Message, e.StackTrace);
                        Util.DWMSLog("CategorizationManager.StartCategorization", errorSummary, EventLogEntryType.Error);
                    }
                }
                else
                {
                    docSetDb.UpdateSetStatus(docSetId, (docSetDb.HasVerificationOfficerAssigned(docSetId) ? SetStatusEnum.Pending_Verification : SetStatusEnum.New));                    
                }
                
                ///////temp to close sets
                //DocSet.DocSetDataTable docSet = docSetDb.GetDocSetById(docSetId);
                //if (docSet.Rows.Count > 0)
                //{
                //    DocSet.DocSetRow docSetRow = docSet[0];
                //    if (docSetRow.Channel == "CDB")
                //    {
                //        docSetDb.UpdateSetStatus(docSetId, SetStatusEnum.Closed);
                //        Guid? importedBy; // SYSTEM Guid
                //        importedBy = Retrieve.GetSystemGuid();

                //        LogActionDb logActionDb = new LogActionDb();
                //        logActionDb.Insert(importedBy.Value, "Verified set closed", "System", string.Empty, string.Empty, string.Empty, LogTypeEnum.S, docSetId);
                //    }
                //}
                ///////

                // Update the application status
                DocAppDb docAppDb = new DocAppDb();
                docAppDb.UpdateSetApplicationStatus(docSetId);

                result = true;
            }
            catch (Exception e)
            {
                string errorSummary = string.Format("Error (CategorizationManager.StartCategorization): SetId={0}, Message={1}, StackTrace={2}",
                    docSetId.ToString(), e.Message, e.StackTrace);

                Util.DWMSLog("CategorizationManager.StartCategorization", errorSummary, EventLogEntryType.Error);

                result = false;
            }

            return result;
        }

        #region Miscellaneous
        /// <summary>
        /// Get the reference numbers from the OCR text
        /// </summary>
        /// <param name="docSetId"></param>
        /// <returns></returns>
        private ArrayList GetRefNos(int docSetId)
        {
            ArrayList refNoList = new ArrayList();
            ArrayList refNoFinalList = new ArrayList();
            DocAppDb docAppDb = new DocAppDb();
            SetAppDb setAppDb = new SetAppDb();
            RawFileDb rawFileDb = new RawFileDb();
            RawPageDb rawPageDb = new RawPageDb();

            SetApp.SetAppDataTable setAppDt = setAppDb.GetSetAppByDocSetId(docSetId);

            if (setAppDt.Rows.Count > 0)
            {
                foreach (SetApp.SetAppRow setAppDr in setAppDt.Rows)
                {
                    string refNo = docAppDb.GetDocAppRefNoById(setAppDr.DocAppId);

                    if (!String.IsNullOrEmpty(refNo))
                    {
                        if (!refNoList.Contains(refNo))
                            refNoList.Add(refNo);
                    }
                }
            }
            else
            {
                // Get the raw files of the set

                RawFile.RawFileDataTable rawFileDt = rawFileDb.GetRawFilesByDocSetId(docSetId);
                foreach (RawFile.RawFileRow rawFile in rawFileDt)
                {
                    RawPage.RawPageDataTable rawPageDt = rawPageDb.GetRawPageByRawFileId(rawFile.Id);

                    foreach (RawPage.RawPageRow rawPage in rawPageDt)
                    {
                        // Check the ocr text for reference numbers
                        string[] lines = rawPage.OcrText.Split(Constants.NewLineSeperators, StringSplitOptions.RemoveEmptyEntries);

                        ArrayList temp = CategorizationHelpers.GetReferenceNumbers(lines);

                        // Add the unique reference numbers to the list
                        foreach (string refNo in temp)
                        {
                            if (!refNoList.Contains(refNo))
                                refNoList.Add(refNo);
                        }
                    }
                }
            }

            // Check reference numbers if it exists in the DocApp table
            foreach (string refNo in refNoList)
            {
                if (docAppDb.DoesReferenceExists(refNo))
                    refNoFinalList.Add(refNo);
            }

            return refNoFinalList;
        }

        /// <summary>
        /// Insert the AppPersonal records
        /// </summary>
        /// <param name="docSetId"></param>
        /// <param name="refNoList"></param>
        private void InsertAppPersonalRecords(int docSetId, ArrayList refNoList)
        {
            SetAppDb setAppDb = new SetAppDb();

            if (refNoList.Count > 0)
            {
                // Delete the current association in the SetApp table
                setAppDb.DeleteBySetId(docSetId);
            }

            foreach (string refNo in refNoList)
            {
                // Insert association into SetApp table.  Then create the AppPersonal records.
                setAppDb.Insert(docSetId, refNo);
            }
        }

        /// <summary>
        /// Retrieve the Valid AppPersonal records for the set.
        /// Valid AppPersonal records are records with NRIC and PersonalType
        /// </summary>
        /// <param name="docSetId"></param>
        /// <returns></returns>
        public AppPersonal.AppPersonalDataTable GetAppPersonalTable(int docSetId)
        {
            // Get the Personal records from the interface file for the given set
            AppPersonalDb appPersonalDb = new AppPersonalDb();
            AppPersonal.AppPersonalDataTable appPersonalTable = appPersonalDb.GetAppPersonalsByDocSetId(docSetId);
            AppPersonal.AppPersonalDataTable finalAppPersonalTable = new AppPersonal.AppPersonalDataTable();

            // Eliminate those AppPersonal records without Personal Type
            foreach (AppPersonal.AppPersonalRow appPersonal in appPersonalTable)
            {
                if (!String.IsNullOrEmpty(appPersonal.Nric.Trim()) && !String.IsNullOrEmpty(appPersonal.PersonalType.Trim()))
                {
                    AppPersonal.AppPersonalRow finalRow = finalAppPersonalTable.NewAppPersonalRow();
                    finalRow.ItemArray = appPersonal.ItemArray;
                    finalAppPersonalTable.Rows.Add(finalRow);
                }
            }

            return finalAppPersonalTable;
        }

        private AppPersonal.AppPersonalDataTable GetAppPersonalByForMainForms(AppPersonal.AppPersonalDataTable currAppPersonal, string formType)
        {
            AppPersonal.AppPersonalDataTable result = new AppPersonal.AppPersonalDataTable();

            // Extract the distinct docappids from the apppersonals
            var distinctDocAppIds = (from row in currAppPersonal.AsEnumerable()
                                     select row.Field<int>("DocAppId")).Distinct();

            DocAppDb docAppDb = new DocAppDb();
            foreach (int docAppId in distinctDocAppIds)
            {
                DocApp.DocAppDataTable dt = docAppDb.GetDocAppById(docAppId);

                if (dt.Rows.Count > 0)
                {
                    DocApp.DocAppRow dr = dt[0];

                    // If the reftype for the docapp corresponds to the formtype of the document, use apppersonal of the docapp
                    if (dr.RefType.ToUpper().Equals(formType.ToUpper()))
                    {
                        string filter = "DocAppId = {0}";

                        AppPersonal.AppPersonalRow[] rows = (AppPersonal.AppPersonalRow[])currAppPersonal.Select(String.Format(filter, dr.Id));

                        foreach (AppPersonal.AppPersonalRow row in rows)
                        {
                            AppPersonal.AppPersonalRow newRow = result.NewAppPersonalRow();
                            newRow.ItemArray = row.ItemArray;
                            result.Rows.Add(newRow);
                        }
                    }
                }
            }

            return result;
        }

        private ArrayList GetTopSamplePages(ArrayList categorizationSampleDocs)
        {
            ArrayList list = new ArrayList();

            foreach (CategorizationSampleDoc categorizationSampleDoc in categorizationSampleDocs)
            {
                list.AddRange(categorizationSampleDoc.TopSampleDocDetails);
            }

            return list;
        }
        #endregion

        #region Create Pages
        /// <summary>
        /// Get the document pages by raw file id
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        private ArrayList GetPages(int rawFileId, int docSetId, AppPersonal.AppPersonalDataTable appPersonalTable,
            ArrayList categorizationSampleDocs, ArrayList topCategorizationSampleDocs, DocType.DocTypeDataTable docTypeDt)
        {
            string libAffPath = string.Empty;
            string libDicPath = string.Empty;

            Retrieve.GetHunspellResourcesPath(out libAffPath, out libDicPath);

            ArrayList pageList = new ArrayList();

            using (Hunspell spellChecker = new Hunspell(libAffPath, libDicPath))
            {
                RawPageDb rawPageDb = new RawPageDb();
                RawPage.RawPageDataTable rawPageDt = rawPageDb.GetRawPageByRawFileId(rawFileId);

                foreach (RawPage.RawPageRow rawPage in rawPageDt)
                {
                    // Create a PageOcr Object
                    PageOcr pageOcr = new PageOcr(rawPage.Id, rawPage.OcrText, rawPage.RawPageNo, docSetId,
                        categorizationSampleDocs, topCategorizationSampleDocs, appPersonalTable, docTypeDt,
                        spellChecker, MINIMUM_ENGLISH_WORD_COUNT, MINIMUM_ENGLISH_WORD_PERCENTAGE,
                        KEYWORD_CHECK_SCOPE, MINIMUM_SCORE, MINIMUM_WORD_LENGTH);

                    // Add the PageOcr object into the ArraList
                    pageList.Add(pageOcr);
                }
            }

            return pageList;
        }

        /// <summary>
        /// Link all the individual pages
        /// </summary>
        /// <param name="pageList"></param>
        private void LinkAllPages(ref ArrayList pageList)
        {
            // Link all pages
            for (int i = 0; i < pageList.Count; i++)
            {
                ((PageOcr)pageList[i]).NextPage = ((i == pageList.Count - 1) ? null : ((PageOcr)pageList[i + 1]));
                ((PageOcr)pageList[i]).PrevPage = ((i == 0) ? null : ((PageOcr)pageList[i - 1]));
            }
        }

        /// <summary>
        /// Merge all the blank and unidentified documents
        /// </summary>
        /// <param name="pageList"></param>
        private void MergeBlankPages(ref ArrayList pageList)
        {
            //List<int> pageIndexes = new List<int>();
            bool isStart = true;
            PageOcr prevPageOcr = null;

            for (int i = 0; i < pageList.Count; i++)
            {
                PageOcr currPageOcr = pageList[i] as PageOcr;

                // If the page has no previous page, set it as the document start page
                if (currPageOcr.IsBlank &&
                    currPageOcr.DocumentType.Equals(DocTypeEnum.Unidentified.ToString()))
                {
                    // Set the previous and next pages accordingly
                    if (currPageOcr.PrevPage != null)
                        currPageOcr.PrevPage.NextPage = null;

                    currPageOcr.PrevPage = null;

                    if (currPageOcr.NextPage != null)
                        currPageOcr.NextPage.PrevPage = null;

                    currPageOcr.NextPage = null;

                    //// Add the page index
                    //pageIndexes.Add(i);

                    if (isStart)
                    {
                        // Set the first blank page as the start of the document of blank pages
                        currPageOcr.IsDocStartPage = true;
                        isStart = false;
                    }
                    else
                    {
                        if (prevPageOcr != null)
                        {
                            // Link the previous page and current page
                            prevPageOcr.NextPage = currPageOcr;
                            currPageOcr.PrevPage = prevPageOcr;
                            currPageOcr.IsDocStartPage = false;
                        }
                    }

                    prevPageOcr = currPageOcr;
                }
            }
        }

        /// <summary>
        /// Break the links between pages to group according to document types
        /// </summary>
        /// <param name="pageList"></param>
        //private void BreakLinksSimilarPages(ref ArrayList pageList)
        //{
        //    for (int i = 0; i < pageList.Count; i++)
        //    {
        //        PageOcr currPageOcr = pageList[i] as PageOcr;

        //        // If the page has no previous page, set it as the document start page
        //        if (currPageOcr.PrevPage != null)
        //        {
        //            if (currPageOcr.DocumentType.Equals(DocTypeEnum.Unidentified.ToString()) &&
        //                currPageOcr.IsBlank) // Ignore blank pages as they have already been grouped in the preceeding functions
        //            {
        //                continue;
        //            }
        //            else if (currPageOcr.DocumentType.Equals(currPageOcr.PrevPage.DocumentType)) // If the previous page and current page have the same doc type
        //            {
        //                #region With Reference Numbers
        //                //if (currPageOcr.DocumentType.Equals(DocTypeEnum.HLE.ToString())) // For HLE documents, group them according to HLE number
        //                //{
        //                //    //if ((!currPageOcr.RefNumber.Equals(currPageOcr.PrevPage.RefNumber)) ||
        //                //    //    (!String.IsNullOrEmpty(currPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.PrevPage.RefNumber)))
        //                //    //{
        //                //    //    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
        //                //    //    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

        //                //    //    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
        //                //    //}
        //                //}
        //                //else if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Resale.ToString().ToUpper()))
        //                //{
        //                //    //if ((!currPageOcr.RefNumber.Equals(currPageOcr.PrevPage.RefNumber)) ||
        //                //    //    (!String.IsNullOrEmpty(currPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.PrevPage.RefNumber)))
        //                //    //{
        //                //    //    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
        //                //    //    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

        //                //    //    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
        //                //    //}
        //                //}
        //                //else if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Sales.ToString().ToUpper()))
        //                //{
        //                //    //if ((!currPageOcr.RefNumber.Equals(currPageOcr.PrevPage.RefNumber)) ||
        //                //    //    (!String.IsNullOrEmpty(currPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.PrevPage.RefNumber)))
        //                //    //{
        //                //    //    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
        //                //    //    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

        //                //    //    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
        //                //    //}
        //                //}
        //                #endregion

        //                if (currPageOcr.DocumentType.Equals(DocTypeEnum.HLE.ToString()) ||
        //                    currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Resale.ToString().ToUpper()) ||
        //                    currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Sales.ToString().ToUpper()) ||
        //                    currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.SERS.ToString().ToUpper()))
        //                {
        //                    continue;
        //                }
        //                else if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.PAYSLIP.ToString().ToUpper())) // Payslip will only be one page per document
        //                {
        //                    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
        //                    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

        //                    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
        //                }
        //                else if (!String.IsNullOrEmpty(currPageOcr.PrevPage.Nric) &&
        //                    !String.IsNullOrEmpty(currPageOcr.Nric) &&
        //                    !currPageOcr.Nric.ToLower().Equals(currPageOcr.PrevPage.Nric.ToLower())) // If the previous and current pages' NRIC is not empty and equal
        //                {
        //                    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
        //                    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

        //                    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
        //                }
        //            }
        //            else
        //            {
        //                if (currPageOcr.PrevPage != null)
        //                {
        //                    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
        //                    currPageOcr.PrevPage = null; // Break the link of this page to the previous page
        //                }

        //                currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
        //            }
        //        }
        //        else
        //        {
        //            if (currPageOcr.DocumentType.Equals(DocTypeEnum.Unidentified.ToString()) &&
        //                currPageOcr.IsBlank) // Ignore blank pages as they have already been grouped in the preceeding functions
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                bool startPage = true;

        //                if (i > 0)
        //                {
        //                    // Check if the previous document type, before the blank pages is same as the current document, 
        //                    // merge the two documents
        //                    for (int cnt = i - 1; cnt >= 0; cnt--)
        //                    {
        //                        PageOcr prevPageOcr = pageList[cnt] as PageOcr;

        //                        if (!prevPageOcr.IsBlank)
        //                        {
        //                            if (prevPageOcr.DocumentType.Equals(currPageOcr.DocumentType) &&
        //                                prevPageOcr.Nric.ToUpper().Equals(currPageOcr.Nric.ToUpper()))
        //                            {
        //                                prevPageOcr.NextPage = currPageOcr;
        //                                currPageOcr.PrevPage = prevPageOcr;
        //                                startPage = false;
        //                            }

        //                            break;
        //                        }
        //                    }
        //                }

        //                currPageOcr.IsDocStartPage = startPage; // Set the page as the start page of the document group
        //            }
        //        }
        //    }
        //}
        private void BreakLinksSimilarPages(ref ArrayList pageList)
        {
            ParameterDb parameterDb = new ParameterDb();
            bool logging = parameterDb.Logging();
            bool detailLogging = parameterDb.DetailLogging();
            for (int i = 0; i < pageList.Count; i++)
            {
                PageOcr currPageOcr = pageList[i] as PageOcr;
                PageOcr prePageOcr = null;
                if (i > 2) { prePageOcr = pageList[i - 1] as PageOcr; }
                try
                {
                    if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()) &&
                        currPageOcr.IsBlank) // Ignore blank pages as they have already been grouped in the preceeding functions
                    {
                        continue;
                    }
                    //else if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.HLE.ToString().ToUpper()) ||
                    //    currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Resale.ToString().ToUpper()) ||
                    //    currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Sales.ToString().ToUpper()) ||
                    //    currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.SERS.ToString().ToUpper()))
                    //{
                    //    continue;
                    //}
                    else
                    {
                        // If the page has no previous page, set it as the document start page
                        if (currPageOcr.PrevPage != null)
                        {
                            if (currPageOcr.DocumentType.ToUpper().Equals(currPageOcr.PrevPage.DocumentType.ToUpper()) || currPageOcr.PrevPage.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()) || currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper())) // If the previous page and current page have the same doc type
                            {
                                #region With Reference Numbers
                                //if (currPageOcr.DocumentType.Equals(DocTypeEnum.HLE.ToString())) // For HLE documents, group them according to HLE number
                                //{
                                //    //if ((!currPageOcr.RefNumber.Equals(currPageOcr.PrevPage.RefNumber)) ||
                                //    //    (!String.IsNullOrEmpty(currPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.PrevPage.RefNumber)))
                                //    //{
                                //    //    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
                                //    //    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

                                //    //    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
                                //    //}
                                //}
                                //else if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Resale.ToString().ToUpper()))
                                //{
                                //    //if ((!currPageOcr.RefNumber.Equals(currPageOcr.PrevPage.RefNumber)) ||
                                //    //    (!String.IsNullOrEmpty(currPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.PrevPage.RefNumber)))
                                //    //{
                                //    //    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
                                //    //    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

                                //    //    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
                                //    //}
                                //}
                                //else if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Sales.ToString().ToUpper()))
                                //{
                                //    //if ((!currPageOcr.RefNumber.Equals(currPageOcr.PrevPage.RefNumber)) ||
                                //    //    (!String.IsNullOrEmpty(currPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.PrevPage.RefNumber)))
                                //    //{
                                //    //    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
                                //    //    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

                                //    //    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
                                //    //}
                                //}
                                #endregion
                                if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization cont page {0}: {1}", currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);

                                //else if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.PAYSLIP.ToString().ToUpper())) // Payslip will only be one page per document
                                //{
                                //    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
                                //    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

                                //    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
                                //}
                                //if NRIC is not the same
                                if (!String.IsNullOrEmpty(currPageOcr.PrevPage.Nric) &&
                                    !String.IsNullOrEmpty(currPageOcr.Nric) &&
                                    !currPageOcr.Nric.ToLower().Equals(currPageOcr.PrevPage.Nric.ToLower())) // If the previous and current pages' NRIC is not empty and equal
                                {
                                    currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
                                    currPageOcr.PrevPage = null; // Break the link of this page to the previous page

                                    currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
                                    if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization break nric page {0}: {1}", currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);
                                }
                                //Merge unidentified when previous and after page is the same
                                else if (currPageOcr.NextPage != null)
                                {
                                    if (currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()) && !currPageOcr.IsBlank && currPageOcr.PrevPage.DocumentType.ToUpper().Equals(currPageOcr.NextPage.DocumentType.ToUpper()))
                                    {
                                        if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization update unidentified {0}: {1} -> {2}", currPageOcr.RawPageNo.ToString(), currPageOcr.DocumentType.ToString(), currPageOcr.NextPage.DocumentType.ToString()), EventLogEntryType.Warning);
                                        currPageOcr.DocumentType = currPageOcr.NextPage.DocumentType;
                                        currPageOcr.Nric = currPageOcr.NextPage.Nric.ToLower();
                                    }
                                }
                                //Merge unidentified
                                else if (currPageOcr.PrevPage.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()) && !currPageOcr.PrevPage.IsBlank)
                                {
                                    if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization update unidentified {0}: {1} <- {2}", currPageOcr.PrevPage.RawPageNo.ToString(), currPageOcr.PrevPage.DocumentType.ToString(), currPageOcr.DocumentType.ToString()), EventLogEntryType.Warning);
                                    currPageOcr.PrevPage.DocumentType = currPageOcr.DocumentType;
                                    currPageOcr.PrevPage.Nric = currPageOcr.Nric.ToLower();
                                }
                            }
                            else if (i > 2 && prePageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()) && !prePageOcr.IsBlank && prePageOcr.PrevPage != null)
                            {
                                if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization jump page {0}: {1}", currPageOcr.DocumentType.ToString(), prePageOcr.PrevPage.DocumentType.ToString()), EventLogEntryType.Warning);
                                if (currPageOcr.DocumentType.ToUpper().Equals(prePageOcr.PrevPage.DocumentType.ToUpper()) && currPageOcr.PrevPage.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper())) // If the previous page and current page have the same doc type
                                {
                                    if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization match {0}: {1}", currPageOcr.DocumentType.ToString(), prePageOcr.PrevPage.DocumentType.ToString()), EventLogEntryType.Warning);
                                    if (currPageOcr.DocumentType.Equals(DocTypeEnum.HLE.ToString()) ||
                                        currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Resale.ToString().ToUpper()) ||
                                        currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Sales.ToString().ToUpper()) ||
                                        currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.SERS.ToString().ToUpper()))
                                    {
                                        continue;
                                    }
                                    else if (!String.IsNullOrEmpty(prePageOcr.PrevPage.Nric) &&
                                        !String.IsNullOrEmpty(currPageOcr.Nric) &&
                                        !currPageOcr.Nric.ToLower().Equals(prePageOcr.PrevPage.Nric.ToLower())) // If the previous and current pages' NRIC is not empty and equal
                                    {
                                        prePageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
                                        prePageOcr.PrevPage = null; // Break the link of this page to the previous page

                                        currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
                                    }
                                    else
                                    {
                                        if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization try merge {0}: {1}", currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);
                                        prePageOcr.DocumentType = currPageOcr.DocumentType;
                                    }
                                }
                            }
                            else
                            {
                                if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization break page {0}: {1} <-> {2}: {3}", currPageOcr.PrevPage.DocumentType.ToString(), currPageOcr.PrevPage.RawPageNo.ToString(), currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);
                                currPageOcr.PrevPage.NextPage = null; // Break the link of the previous page to this page
                                currPageOcr.PrevPage = null; // Break the link of this page to the previous page

                                currPageOcr.IsDocStartPage = true; // Set the page as the start page of the document group
                            }
                        }
                        else
                        {
                            if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization start page {0}: {1}", currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);
                            bool startPage = true;

                            if (i > 0)
                            {
                                // Check if the previous document type, before the blank pages is same as the current document, 
                                // merge the two documents
                                for (int cnt = i - 1; cnt >= 0; cnt--)
                                {
                                    PageOcr prevPageOcr = pageList[cnt] as PageOcr;

                                    if (!prevPageOcr.IsBlank && !currPageOcr.IsBlank)
                                    {
                                        if (prevPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()) && !currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()))
                                        {
                                            if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization try merge {0}: {1} <- {2}: {3}", prevPageOcr.DocumentType.ToString(), prevPageOcr.RawPageNo.ToString(), currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);
                                            prevPageOcr.DocumentType = currPageOcr.DocumentType;
                                            prevPageOcr.Nric = currPageOcr.Nric.ToLower();
                                            prevPageOcr.NextPage = currPageOcr;
                                            currPageOcr.PrevPage = prevPageOcr;
                                            startPage = false;
                                            break;
                                        }
                                        if (!prevPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()) && currPageOcr.DocumentType.ToUpper().Equals(DocTypeEnum.Unidentified.ToString().ToUpper()))
                                        {
                                            if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization try merge {0}: {1} -> {2}: {3}", prevPageOcr.DocumentType.ToString(), prevPageOcr.RawPageNo.ToString(), currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);
                                            currPageOcr.DocumentType = prevPageOcr.DocumentType;
                                            currPageOcr.Nric = prevPageOcr.Nric.ToLower();
                                            prevPageOcr.NextPage = currPageOcr;
                                            currPageOcr.PrevPage = prevPageOcr;
                                            startPage = false;
                                            break;
                                        }
                                        else if (prevPageOcr.DocumentType.ToUpper().Equals(currPageOcr.DocumentType.ToUpper()) &&
                                                prevPageOcr.Nric.ToLower().Equals(currPageOcr.Nric.ToLower()))
                                        {
                                            if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization same type merge {0}: {1} & {2}: {3}", prevPageOcr.DocumentType.ToString(), prevPageOcr.RawPageNo.ToString(), currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);
                                            prevPageOcr.NextPage = currPageOcr;
                                            currPageOcr.PrevPage = prevPageOcr;
                                            startPage = false;
                                        }
                                        break;
                                    }
                                }
                            }
                            currPageOcr.IsDocStartPage = startPage; // Set the page as the start page of the document group
                        }
                    }
                }
                catch
                {
                    Util.DWMSLog("CategorizationManagerForDoc.BreakLinksSimilarPages", String.Format("Categorization try catch {0}: {1}", currPageOcr.RawPageNo.ToString(), currPageOcr.DocumentType.ToString()), EventLogEntryType.Error);
                }
            }
        }

        /// <summary>
        /// Link similar Hle pages that are not in sequence
        /// </summary>
        /// <param name="pageList"></param>
        private void LinkSimilarHlePagesNotInSequence(ref ArrayList pageList)
        {
            ParameterDb parameterDb = new ParameterDb();
            bool logging = parameterDb.Logging();
            bool detailLogging = parameterDb.DetailLogging();
            for (int i = 0; i < pageList.Count; i++)
            {
                PageOcr currPageOcr = pageList[i] as PageOcr;

                if (currPageOcr.DocumentType.Equals(DocTypeEnum.HLE.ToString()) && currPageOcr.NextPage == null)
                {
                    for (int y = i + 1; y < pageList.Count; y++)
                    {
                        PageOcr nextPageOcr = pageList[y] as PageOcr;

                        if (nextPageOcr.DocumentType.Equals(DocTypeEnum.HLE.ToString()) &&
                            nextPageOcr.IsDocStartPage)
                        {
                            //if ((nextPageOcr.RefNumber.Equals(currPageOcr.RefNumber)) ||
                            //    (String.IsNullOrEmpty(nextPageOcr.RefNumber) && !String.IsNullOrEmpty(currPageOcr.RefNumber)) ||
                            //    (String.IsNullOrEmpty(nextPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.RefNumber)))
                            //{
                            //if (currPageOcr.RefNumber.Equals(nextPageOcr.RefNumber))
                            //{
                            if (detailLogging) Util.DWMSLog("CategorizationManagerForDoc.LinkSimilarHlePagesNotInSequence", String.Format("Categorization same type merge {0}: {1} & {2}: {3}", currPageOcr.DocumentType.ToString(), currPageOcr.RawPageNo.ToString(), nextPageOcr.DocumentType.ToString(), nextPageOcr.RawPageNo.ToString()), EventLogEntryType.Warning);
                            currPageOcr.NextPage = nextPageOcr;
                            nextPageOcr.PrevPage = currPageOcr;
                            nextPageOcr.IsDocStartPage = false;

                            // break the inner loop to start the process again 
                            // starting with the last page traversed from the pageList
                            i = y;
                            break;
                            //}
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Link similar Resale pages that are not in sequence
        /// </summary>
        /// <param name="pageList"></param>
        private void LinkSimilarResalePagesNotInSequence(ref ArrayList pageList)
        {
            for (int i = 0; i < pageList.Count; i++)
            {
                PageOcr currPageOcr = pageList[i] as PageOcr;

                if (currPageOcr.DocumentType.Equals(DocTypeEnum.Resale.ToString()) && currPageOcr.NextPage == null)
                {
                    for (int y = i + 1; y < pageList.Count; y++)
                    {
                        PageOcr nextPageOcr = pageList[y] as PageOcr;

                        if (nextPageOcr.DocumentType.Equals(DocTypeEnum.Resale.ToString()) &&
                            nextPageOcr.IsDocStartPage)
                        {
                            //if ((nextPageOcr.RefNumber.Equals(currPageOcr.RefNumber)) ||
                            //    (String.IsNullOrEmpty(nextPageOcr.RefNumber) && !String.IsNullOrEmpty(currPageOcr.RefNumber)) ||
                            //    (String.IsNullOrEmpty(nextPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.RefNumber)))
                            //{
                            currPageOcr.NextPage = nextPageOcr;
                            nextPageOcr.PrevPage = currPageOcr;
                            nextPageOcr.IsDocStartPage = false;

                            // break the inner loop to start the process again 
                            // starting with the last page traversed from the pageList
                            i = y;
                            break;
                            //}
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Link similar Sales pages that are not in sequence
        /// </summary>
        /// <param name="pageList"></param>
        private void LinkSimilarSalesPagesNotInSequence(ref ArrayList pageList)
        {
            for (int i = 0; i < pageList.Count; i++)
            {
                PageOcr currPageOcr = pageList[i] as PageOcr;

                if (currPageOcr.DocumentType.Equals(DocTypeEnum.Sales.ToString()) && currPageOcr.NextPage == null)
                {
                    for (int y = i + 1; y < pageList.Count; y++)
                    {
                        PageOcr nextPageOcr = pageList[y] as PageOcr;

                        if (nextPageOcr.DocumentType.Equals(DocTypeEnum.Sales.ToString()) &&
                            nextPageOcr.IsDocStartPage)
                        {
                            //if ((nextPageOcr.RefNumber.Equals(currPageOcr.RefNumber)) ||
                            //    (String.IsNullOrEmpty(nextPageOcr.RefNumber) && !String.IsNullOrEmpty(currPageOcr.RefNumber)) ||
                            //    (String.IsNullOrEmpty(nextPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.RefNumber)))
                            //{
                            currPageOcr.NextPage = nextPageOcr;
                            nextPageOcr.PrevPage = currPageOcr;
                            nextPageOcr.IsDocStartPage = false;

                            // break the inner loop to start the process again 
                            // starting with the last page traversed from the pageList
                            i = y;
                            break;
                            //}
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Link similar Sers pages that are not in sequence
        /// </summary>
        /// <param name="pageList"></param>
        private void LinkSimilarSersPagesNotInSequence(ref ArrayList pageList)
        {
            for (int i = 0; i < pageList.Count; i++)
            {
                PageOcr currPageOcr = pageList[i] as PageOcr;

                if (currPageOcr.DocumentType.Equals(DocTypeEnum.SERS.ToString()) && currPageOcr.NextPage == null)
                {
                    for (int y = i + 1; y < pageList.Count; y++)
                    {
                        PageOcr nextPageOcr = pageList[y] as PageOcr;

                        if (nextPageOcr.DocumentType.Equals(DocTypeEnum.SERS.ToString()) &&
                            nextPageOcr.IsDocStartPage)
                        {
                            //if ((nextPageOcr.RefNumber.Equals(currPageOcr.RefNumber)) ||
                            //    (String.IsNullOrEmpty(nextPageOcr.RefNumber) && !String.IsNullOrEmpty(currPageOcr.RefNumber)) ||
                            //    (String.IsNullOrEmpty(nextPageOcr.RefNumber) && String.IsNullOrEmpty(currPageOcr.RefNumber)))
                            //{
                            currPageOcr.NextPage = nextPageOcr;
                            nextPageOcr.PrevPage = currPageOcr;
                            nextPageOcr.IsDocStartPage = false;

                            // break the inner loop to start the process again 
                            // starting with the last page traversed from the pageList
                            i = y;
                            break;
                            //}
                        }
                    }
                }
            }
        }
        #endregion

        #region Create Documents
        /// <summary>
        /// Create the documents object
        /// </summary>
        /// <param name="pageList"></param>
        /// <param name="docList"></param>
        private void CreateDocuments(ref ArrayList pageList, ref ArrayList docList)
        {
            // Create documents
            for (int i = 0; i < pageList.Count; i++)
            {
                PageOcr pageOcr = pageList[i] as PageOcr;

                if (pageOcr.PrevPage == null && pageOcr.IsDocStartPage)
                {
                    DocOcr docOcr = new DocOcr(pageOcr);
                    docList.Add(docOcr);
                }
            }
        }

        /// <summary>
        /// Create the meta data for each Document
        /// </summary>
        /// <param name="docList"></param>
        private void CreatePersonalList(ref ArrayList docList)
        {
            for (int i = 0; i < docList.Count; i++)
            {
                DocOcr doc = docList[i] as DocOcr;
                PageOcr currPage = doc.FirstPage;

                string docType = currPage.DocumentType;

                ArrayList personalList = new ArrayList();

                // Storage for all the NRICs for all the pages in the document
                ArrayList nricList = new ArrayList();

                // Insert the Personal info
                while (currPage != null)
                {
                    // If NRIC exists for the page, add it to the NRIC list
                    if (!String.IsNullOrEmpty(currPage.Nric))
                    {
                        if (!nricList.Contains(currPage.Nric))
                            nricList.Add(currPage.Nric);
                    }

                    currPage = currPage.NextPage;
                }

                GetPersonalFromNric(nricList, docType, ref personalList);

                // Assign the personal list to the doc
                doc.PersonalList = personalList;
            }
        }

        /// <summary>
        /// Save the docs
        /// </summary>
        /// <param name="docList"></param>
        private void SaveDocs(int docSetId, ref ArrayList docList, AppPersonal.AppPersonalDataTable appPersonalTable, string fileNameInRow)
        {
            // Save documents
            DocDb docDb = new DocDb();
            RawPageDb rawPageDb = new RawPageDb();
            DocTypeDb docTypeDb = new DocTypeDb();

            try
            {
                // Delete the documents of the set (if any)
                docDb.DeleteByDocSetId(docSetId);
            }
            catch (Exception)
            {
            }

            for (int i = 0; i < docList.Count; i++)
            {
                DocOcr doc = docList[i] as DocOcr;
                PageOcr currPage = doc.FirstPage;

                // Save the document into the Doc table
                int docId = docDb.Insert(docSetId, doc.DocumentType, docSetId, DocStatusEnum.New.ToString(), string.Empty);

                if (docId > 0)
                {
                    #region Add docChannel and originalCmDocumentId to DocTable although it's not categorized
                    CustomerWebServiceInfo[] customers = ParseWebServiceXml(docSetId);
                    if (customers.Length > 0)
                    {
                        foreach (CustomerWebServiceInfo customer in customers)
                        {
                            string nric = customer.Nric;
                            string refNo = customer.RefNo;
                            foreach (DocWebServiceInfo doc2 in customer.Documents)
                            {
                                string xmlDocId = doc2.DocId;
                                string xmlDocSubId = doc2.DocSubId;

                                //GetDocType from XML
                                string docType = DocTypeEnum.Unidentified.ToString();
                                DocType.DocTypeDataTable docTypeDt = docTypeDb.GetDocType(xmlDocId, xmlDocSubId);
                                if (docTypeDt.Rows.Count > 0)
                                {
                                    DocType.DocTypeRow docTypeDr = docTypeDt[0];
                                    docType = docTypeDr.Code;
                                }

                                //get docChannel, originalCmDocumentid and docDescription
                                bool isVerified = false;
                                string docChannel = string.Empty;
                                string originalCmDocumentId = string.Empty;
                                string docDescription = string.Empty;
                                foreach (FileWebServiceInfo file in doc2.Files)
                                {
                                    string fileName = file.Name;
                                    if (fileNameInRow.ToLower().Equals(fileName.ToLower()))
                                    {

                                        foreach (MetadataWebServiceInfo metadata in file.Metadata)
                                        {
                                            if (metadata.IsVerified)
                                                isVerified = true;
                                            docChannel = metadata.DocChannel;
                                            originalCmDocumentId = metadata.CmDocumentID;
                                        }
                                        docDescription = doc2.DocDescription;
                                    }
                                }
                                docDb.UpdateIsVerified(docId, isVerified);
                                docDb.UpdateDocChannelCmDocumentIdAndDescriptionFromWebServices(docId, docChannel, originalCmDocumentId, docDescription);
                            }
                        }
                    }
                    #endregion

                    // Personal Data container
                    ArrayList personalDataList = new ArrayList();

                    // Insert Personals
                    AddAppPersonalDocPersonalRecords(docSetId, docId, doc, ref personalDataList, appPersonalTable);

                    // Insert Metadata
                    AddMetaData(doc, currPage, docId, personalDataList);

                    int docPage = 1;
                    // Update the rawpage
                    while (currPage != null)
                    {
                        // Update the doc id of the raw page                        
                        rawPageDb.Update(currPage.Id, docId, docPage);

                        currPage = currPage.NextPage;
                        docPage++;
                    }
                }
            }
        }

        /// <summary>
        /// Get the interface data using nric
        /// </summary>
        /// <param name="nric"></param>
        /// <returns></returns>
        private void GetPersonalFromNric(ArrayList nricList, string docType, ref ArrayList personalList)
        {
            ArrayList personalCount = new ArrayList();

            foreach (string nric in nricList)
            {
                PersonalData personal = new PersonalData();
                personal.Nric = nric;
                personalCount.Add(personal);
            }

            // For marriage cert documents, 2 personal records will be created
            // For others, only 1
            int limit = 1;

            if (personalCount.Count <= 0)
            {
                for (int cnt = 0; cnt < limit; cnt++)
                {
                    PersonalData personal = new PersonalData();
                    personalList.Add(personal);
                }
            }
            else if (personalCount.Count < limit && personalCount.Count > 0)
            {
                for (int cnt = 0; cnt < personalCount.Count; cnt++)
                {
                    personalList.Add(personalCount[cnt]);
                }

                for (int cnt = personalCount.Count; cnt < limit; cnt++)
                {
                    PersonalData personal = new PersonalData();
                    personalList.Add(personal);
                }
            }
            else if (personalCount.Count > limit)
            {
                personalCount.RemoveRange(limit, personalCount.Count - limit);
                personalList.AddRange(personalCount);
            }
            else
            {
                personalList.AddRange(personalCount);
            }
        }

        /// <summary>
        /// Add the AppPersonal/DocPersonal records
        /// </summary>
        /// <param name="docSetId"></param>
        /// <param name="docId"></param>
        /// <param name="doc"></param>
        /// <param name="personalDataList"></param>
        /// <param name="appPersonalTable"></param>
        private void AddAppPersonalDocPersonalRecords(int docSetId, int docId, DocOcr doc,
            ref ArrayList personalDataList, AppPersonal.AppPersonalDataTable appPersonalTable)
        {
            AppPersonalDb appPersonalDb = new AppPersonalDb();
            AppDocRefDb appDocRefDb = new AppDocRefDb();
            DocPersonalDb docPersonalDb = new DocPersonalDb();
            SetDocRefDb setDocRefDb = new SetDocRefDb();

            #region Insert Personals
            // Insert the association for the doc and app personal for HLE, SALES, RESALE and SERS documents
            if (doc.DocumentType.Equals(DocTypeEnum.HLE.ToString()) ||
                doc.DocumentType.Equals(DocTypeEnum.Resale.ToString()) ||
                doc.DocumentType.Equals(DocTypeEnum.Sales.ToString()) ||
                doc.DocumentType.Equals(DocTypeEnum.SERS.ToString()))
            {
                #region Main Documents
                // Retrieve the personal records only for the reference type.  If the personal record for the reference type is not found,
                // no association will be established.
                //AppPersonal.AppPersonalDataTable personalTempTable = appPersonalDb.GetAppPersonalsByDocSetIdAndRefType(docSetId, doc.DocumentType.ToUpper());
                AppPersonal.AppPersonalDataTable personalTempTable = GetAppPersonalByForMainForms(appPersonalTable, doc.DocumentType);

                if (personalTempTable.Rows.Count > 0) // Insert AppPersonal reference
                {
                    foreach (AppPersonal.AppPersonalRow appPersonal in personalTempTable.Rows)
                    {
                        // Assign the AppPersonal to the document
                        appDocRefDb.Insert(docId, appPersonal.Id);

                        // Add the personal data
                        PersonalData personalData = new PersonalData(appPersonal);
                        personalDataList.Add(personalData);
                    }
                }
                else // Insert one DocPersonal reference
                {
                    // Insert the doc personal record for the doc
                    int docPersonalId = docPersonalDb.Insert(docSetId, string.Empty, string.Empty,
                        DocFolderEnum.Unidentified.ToString(), string.Empty);

                    // Insert the association of the doc and doc personal
                    setDocRefDb.Insert(docId, docPersonalId);

                    // Add the personal data
                    PersonalData personal = new PersonalData();
                    personalDataList.Add(personal);
                }
                #endregion
            }
            else
            {
                #region Other documents
                // Insert the personal data
                foreach (PersonalData personal in doc.PersonalList)
                {
                    AppPersonal.AppPersonalRow appPersonalRow = GetPersonalFromAppPersonal(appPersonalTable, personal.Nric,
                        DocFolderEnum.Unidentified.ToString(), string.Empty);

                    if (appPersonalRow != null)
                    {
                        // Insert the association of the doc and the app personal
                        appDocRefDb.Insert(docId, appPersonalRow.Id);

                        PersonalData personalData = new PersonalData(appPersonalRow);
                        personalDataList.Add(personalData);
                    }
                    else
                    {
                        // Insert the doc personal record for the doc
                        int docPersonalId = docPersonalDb.Insert(docSetId, personal.Nric, personal.Name,
                            DocFolderEnum.Unidentified.ToString(), personal.Relationship);

                        // Insert the association of the doc and doc personal
                        setDocRefDb.Insert(docId, docPersonalId);

                        // Add the personal data
                        personalDataList.Add(personal);
                    }
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// Get personal from apppersonal
        /// </summary>
        /// <param name="appPersonalTable"></param>
        /// <param name="nric"></param>
        /// <returns></returns>
        public AppPersonal.AppPersonalRow GetPersonalFromAppPersonal(AppPersonal.AppPersonalDataTable appPersonalTable,
            string nric, string folder, string relationship)
        {
            AppPersonal.AppPersonalRow appPersonalRow = null;

            //string sortFilter = "Nric='{0}' AND Folder='{1}' AND Relationship='{2}'";
            //string sortFilter = String.Format("Nric='{0}' AND Folder='{1}'", nric, folder);
            string sortFilter = String.Format("Nric='{0}'", nric);

            if (!String.IsNullOrEmpty(relationship))
                sortFilter += String.Format(" AND (Relationship IS NOT NULL AND Relationship='{0}')", relationship);

            AppPersonal.AppPersonalRow[] result = (AppPersonal.AppPersonalRow[])appPersonalTable.Select(sortFilter);

            if (result.Length > 0)
            {
                appPersonalRow = appPersonalTable.NewAppPersonalRow();
                appPersonalRow.ItemArray = result[0].ItemArray;
            }

            return appPersonalRow;
        }

        /// <summary>
        /// Add the meta data
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="currPage"></param>
        /// <param name="docId"></param>
        /// <param name="personalDataList"></param>
        public void AddMetaData(DocOcr doc, PageOcr currPage, int docId, ArrayList personalDataList)
        {
            MetaDataDb metaDataDb = new MetaDataDb();

            // Get the meta data for the document
            StringBuilder mergedOcrText = new StringBuilder();
            PageOcr tempCurrPage = currPage;
            while (tempCurrPage != null)
            {
                // Get the personals for each page;
                if (!tempCurrPage.DocumentType.Equals(DocTypeEnum.HLE.ToString()))
                {
                    mergedOcrText.Append(Environment.NewLine + tempCurrPage.OcrText); // Append the OCR text
                }

                tempCurrPage = tempCurrPage.NextPage;
            }

            // Get the meta data from the maintenance list
            MetaDataMaintenanceList metaMainList = new MetaDataMaintenanceList(currPage.DocumentType);
            doc.MetaDataMaintenance = metaMainList.MetaData;

            // Get the hard coded meta data
            MetaDataHardCoded metaHardCode = new MetaDataHardCoded(docId, currPage.DocumentType, mergedOcrText.ToString(),
                personalDataList);
            doc.MetaDataHardCode = metaHardCode.MetaData;

            // Insert hard code meta data
            foreach (MetaDataOcr metaData in doc.MetaDataHardCode)
            {
                metaDataDb.Insert(docId, metaData.FieldName, metaData.FieldValue, metaData.VerificationMandatory, metaData.CompletenessMandatory,
                    metaData.VerificationVisible, metaData.CompletenessVisible, metaData.IsFixed, metaData.MaximumLength, false);
            }

            // Insert meta data from maintenance list
            foreach (MetaDataOcr metaData in doc.MetaDataMaintenance)
            {
                metaDataDb.Insert(docId, metaData.FieldName, metaData.FieldValue, metaData.VerificationMandatory, metaData.CompletenessMandatory,
                        metaData.VerificationVisible, metaData.CompletenessVisible, metaData.IsFixed, metaData.MaximumLength, true);
            }
        }
        #endregion

        #region Categorize Document From Web Service
        private bool CategorizeDocFromWebService(int setId, string fileNameInRow)
        {
            RawFileDb rawFileDb = new RawFileDb();
            RawPageDb rawPageDb = new RawPageDb();
            DocDb docDb = new DocDb();
            DocSetDb docSetDb = new DocSetDb();
            DocPersonalDb docPersonalDb = new DocPersonalDb();
            AppPersonalDb appPersonalDb = new AppPersonalDb();
            SetDocRefDb setDocRefDb = new SetDocRefDb();
            AppDocRefDb appDocRefDb = new AppDocRefDb();
            SetAppDb setAppDb = new SetAppDb();
            DocTypeDb docTypeDb = new DocTypeDb();
            MetaDataDb metadataDb = new MetaDataDb();
            bool verifiedSet = true;

            CustomerWebServiceInfo[] customers = ParseWebServiceXml(setId);
            if (customers.Length > 0)
            {
                foreach (CustomerWebServiceInfo customer in customers)
                {
                    foreach (DocWebServiceInfo doc in customer.Documents)
                    {
                        foreach (FileWebServiceInfo file in doc.Files)
                        {
                            string fileName = file.Name;

                            #region Only Process The One with Specific FileName

                            if (fileNameInRow.ToLower().Equals(fileName.ToLower()))
                            {
                                string custid = customer.CustomerIdFromSource;
                                string name = customer.Name;
                                string nric = customer.Nric;
                                string idtype = customer.IdentityType;
                                string refNo = customer.RefNo;

                                string docId = doc.DocId;
                                string docSubId = doc.DocSubId;

                                // Get the document type
                                string docType = DocTypeEnum.Unidentified.ToString();
                                DocType.DocTypeDataTable docTypeDt = docTypeDb.GetDocType(docId, docSubId);

                                if (docTypeDt.Rows.Count > 0)
                                {
                                    DocType.DocTypeRow docTypeDr = docTypeDt[0];
                                    docType = docTypeDr.Code;
                                }

                                //2012-12-17
                                //Assuming there will be only one MetaDataWebServiceInfo which is the ImageInfoClass
                                DocStatusEnum docStatus = DocStatusEnum.New;

                                //get isVerified, docChannel, originalCmDocumentid and docDescription
                                bool isVerified = false;
                                string docChannel = string.Empty;
                                string originalCmDocumentId = string.Empty;
                                string docDescription = string.Empty;
                                string certNo = string.Empty;
                                string certDate = string.Empty;
                                string localForeign = string.Empty;
                                string marriageType = string.Empty;

                                int docPageNo = 1;

                                foreach (MetadataWebServiceInfo metadata in file.Metadata)
                                {
                                    if (metadata.IsVerified)
                                        isVerified = true;

                                    docChannel = metadata.DocChannel;
                                    originalCmDocumentId = metadata.CmDocumentID;

                                    certNo = metadata.CertNo;
                                    //2012-12-12
                                    //certDate = string.IsNullOrEmpty(metadata.CertDate) ? string.Empty : metadata.CertDate;
                                    certDate = metadata.CertDate;

                                    localForeign = metadata.LocalForeign;
                                    marriageType = metadata.MarriageType;
                                }

                                if (isVerified)
                                    docStatus = DocStatusEnum.Verified;
                                else
                                    verifiedSet = false;

                                int createdDocId = docDb.Insert(setId, docType, setId, docStatus.ToString(), doc.CustomerIdSubFromSource);
                                docDb.UpdateIsVerified(createdDocId, isVerified);
                                docDb.UpdateDocChannelCmDocumentIdAndDescriptionFromWebServices(createdDocId, docChannel, originalCmDocumentId, docDescription);

                                // Insert the AppDocRef or SetDocRef record
                                AppPersonal.AppPersonalDataTable appPersonalDt = appPersonalDb.GetAppPersonalByNricAndRefNo(nric, refNo);
                                if(appPersonalDt.Rows.Count <1)
                                    appPersonalDt = appPersonalDb.GetAppPersonalByCustomerSourceIdAndRefNo(custid, refNo); 

                                if (doc.DocId.Trim().Equals(MainFormDocumentIdEnum.D000094.ToString().Replace("D", "")) ||
                                    doc.DocId.Trim().Equals(MainFormDocumentIdEnum.D000095.ToString().Replace("D", "")) ||
                                    doc.DocId.Trim().Equals(MainFormDocumentIdEnum.D000096.ToString().Replace("D", "")) ||
                                    doc.DocId.Trim().Equals(MainFormDocumentIdEnum.D000097.ToString().Replace("D", "")))
                                { // for HLE, RESALE, SALE and SERS document types, attach the document to all the applicants, ocuupier etc...

                                    CategorizationManagerForDoc categorizationManagerForDoc = new CategorizationManagerForDoc();
                                    AppPersonal.AppPersonalDataTable appPersonalTable = categorizationManagerForDoc.GetAppPersonalTable(setId);

                                    if (appPersonalTable.Rows.Count > 0)
                                    {
                                        // Insert the association of the doc and app personal
                                        foreach (AppPersonal.AppPersonalRow appPersonal in appPersonalTable.Rows)
                                        {
                                            appDocRefDb.Insert(createdDocId, appPersonal.Id);
                                        }
                                    }
                                    else // if no AppPersonal records are found, attach the document to DocPersonal.
                                    {
                                        // Insert the doc personal record for the doc
                                        int docPersonalId = docPersonalDb.Insert(setId, nric, string.Empty,
                                            DocFolderEnum.Unidentified.ToString(), string.Empty);

                                        // Insert the association of the doc and doc personal
                                        setDocRefDb.Insert(createdDocId, docPersonalId);
                                    }
                                }
                                else if (appPersonalDt.Rows.Count > 0) // for all the other document types, attached to the AppPersonal nric(if exist)
                                {
                                    AppPersonal.AppPersonalRow appPersonalDr = appPersonalDt[0];
                                    appDocRefDb.Insert(createdDocId, appPersonalDr.Id);
                                }
                                else if (docStatus == DocStatusEnum.Verified) //create appPersonal under Others when doc is verified
                                {
                                    //AppPersonal.AppPersonalDataTable appPersonalAllDt = appPersonalDb.GetAppPersonalsByDocSetId(setId);\
                                    SetApp.SetAppDataTable setAppDt = setAppDb.GetSetAppByDocSetId(setId);
                                    if (setAppDt.Rows.Count > 0)
                                    {
                                        int appPersonalId = appPersonalDb.Insert(setAppDt[0].DocAppId, nric, name, "",
                                        "", "", "", DocFolderEnum.Others.ToString(),
                                        "", 0);
                                        appDocRefDb.Insert(createdDocId, appPersonalId);
                                    }
                                    //AppPersonal.AppPersonalDataTable appPersonalAllDt = appPersonalDb.GetAppPersonalsByDocSetId(setId);
                                    //if (appPersonalAllDt.Rows.Count > 0)
                                    //    foreach (AppPersonal.AppPersonalRow appPersonalAll in appPersonalAllDt.Rows)
                                    //        if (appPersonalAll.Folder == DocFolderEnum.Others.ToString())
                                    //        {
                                    //            int appPersonalId = appPersonalDb.Insert(appPersonalAllDt[0].DocAppId, nric, name, "",
                                    //                "", "", "", DocFolderEnum.Others.ToString(),
                                    //                "", 0);
                                    //            appDocRefDb.Insert(createdDocId, appPersonalAll.Id);
                                    //            break;
                                    //        }
                                }
                                else // if no AppPersonal records are found, attach the document to DocPersonal.
                                {
                                    int docPersonalId;
                                    docPersonalId = docPersonalDb.Insert(setId, nric, string.Empty,
                                        DocFolderEnum.Unidentified.ToString(), string.Empty);

                                    // Insert the association of the doc and doc personal
                                    setDocRefDb.Insert(createdDocId, docPersonalId);
                                }

                                // Update the raw pages.  Assign the newly created document to the raw pages.
                                RawFile.RawFileDataTable rawFileDt = rawFileDb.GetRawFilesBySetIdAndFileName(setId, fileName);

                                if (rawFileDt.Rows.Count > 0)
                                {
                                    RawFile.RawFileRow rawFileDr = rawFileDt[0];

                                    // Get the raw pages
                                    RawPage.RawPageDataTable rawPageDt = rawPageDb.GetRawPageByRawFileId(rawFileDr.Id);

                                    foreach (RawPage.RawPageRow rawPageDr in rawPageDt)
                                    {
                                        // Update the doc id and doc page no of the raw page
                                        rawPageDb.Update(rawPageDr.Id, createdDocId, docPageNo++);
                                    }
                                }

                                // Get the metadata for the document
                                MetaDataHardCoded metaDataHardCoded = new MetaDataHardCoded(createdDocId, docType);
                                metaDataHardCoded.CreateMetaDataForWebService(certNo, certDate, localForeign, marriageType, doc.DocStartDate, doc.DocEndDate, doc.IdentityNoSub);

                                // Insert hard code meta data
                                foreach (MetaDataOcr metaData in metaDataHardCoded.MetaData)
                                {
                                    metadataDb.Insert(createdDocId, metaData.FieldName, metaData.FieldValue, metaData.VerificationMandatory, metaData.CompletenessMandatory,
                                        metaData.VerificationVisible, metaData.CompletenessVisible, metaData.IsFixed, metaData.MaximumLength, false);
                                }
                            }
                            #endregion

                        } //foreach File Name
                    } //foreach Doc
                } //foreach Customer
            }
            return verifiedSet;
        }

        private bool SendEmailToOic(int setId)
        {
            //Get docapps based on the set id
            DocAppDb docAppDb = new DocAppDb();
            DocApp.DocAppDataTable docApps = docAppDb.GetDocAppByDocSetId(setId);

            DocSetDb docSetDb = new DocSetDb();
            DocSet.DocSetDataTable docSet = docSetDb.GetDocSetById(setId);

            if (docSet.Rows.Count > 0)
            {
                DocSet.DocSetRow docSetRow = docSet[0];
                string setNumber = docSetRow.SetNo;

                foreach (DocApp.DocAppRow docAppRow in docApps.Rows)
                {
                    string peOIC = string.Empty;
                    string caOIC = string.Empty;
                    string recipientEmail = string.Empty;
                    string subject = string.Empty;
                    string message = string.Empty;
                    //string PDFPath = string.Empty;
                    string pdfPath = string.Empty;
                    string ccEmail = "MyDocErrLog@hdb.gov.sg";

                    //Get the RefNo from dbo.DocApp
                    string refNo = docAppRow.RefNo.Trim();
                    string hleStatus = HleInterfaceDb.GetHleStatusByRefNo(refNo).ToString();
                    hleStatus = String.IsNullOrEmpty(hleStatus) ? "N/A" : hleStatus;

                    peOIC = docAppRow.IsPeOICNull() ? string.Empty : docAppRow.PeOIC.Trim(); // get PeOIC (null or not)
                    caOIC = docAppRow.IsCaseOICNull() ? string.Empty : docAppRow.CaseOIC.Trim(); // get CaseOIC (null or not)

                    //Util.DWMSLog("CategorizationManager.StartCategorization", hleStatus + " - " + peOIC, EventLogEntryType.Error);
                    #region old code which referes to interface tables (hleinterface, sersinterface, salesinterface and resaleinterface)
                    //switch (refType.ToUpper().Trim())
                    //{
                    //    case "HLE":
                    //        //check the HleInterface 
                    //        HleInterfaceDb hleDb = new HleInterfaceDb();
                    //        emailRecipient = hleDb.GetOICEmailRecipientByHleNumber(refNo);
                    //        break;
                    //    case "SERS":
                    //        //Sers the HleInterface 
                    //        //SersInterfaceDb sersDb = new SersInterfaceDb();
                    //        //emailRecipient = sersDb.GetOICEmailRecipientByHleNumber(refNo);

                    //        break;
                    //    case "SALES":
                    //        //Sales the HleInterface 
                    //        //SalesInterfaceDb salesDb = new SalesInterfaceDb();
                    //        //emailRecipient = salesDb.GetOICEmailRecipientByHleNumber(refNo);

                    //        break;
                    //    case "RESALE":
                    //        //re-sale the HleInterface 
                    //        ResaleInterfaceDb resaleDb = new ResaleInterfaceDb();
                    //        emailRecipient = resaleDb.GetOICEmailRecipientByHleNumber(refNo);

                    //        break;

                    //    default:
                    //        break;
                    //}
                    #endregion
                    //logic edited by calvin
                    if (!String.IsNullOrEmpty(peOIC.Trim()) && peOIC.Trim() != "-" && peOIC.Trim().ToUpper() != "COS")
                    {
                        bool peOICfoundInUserList = ProfileDb.GetCountByEmailSetId(peOIC, setId);
                        bool caOICfoundInUserList = ProfileDb.GetCountByEmailSetId(caOIC, setId);
                        if (!peOICfoundInUserList)
                        {
                            recipientEmail = peOIC.Trim() + "@" + Retrieve.GetEmailDomain();

                            subject = "Verified documents for " + docAppRow.RefType.Trim() + " " + refNo + " have been received";
                            message = "Please review the case, if necessary";

                            string errorMsg = string.Empty;
                            pdfPath = Util.GeneratePdfPathBySetId(setId, out errorMsg);
                            if (!String.IsNullOrEmpty(errorMsg)) message += " There is an error of attaching the files (" + errorMsg + ")<br/> Please contact DWMS Admin.";
                        }
                        else if (hleStatus.Equals(HleStatusEnum.Approved.ToString()) || hleStatus.Equals(HleStatusEnum.Cancelled.ToString()) || hleStatus.Equals(HleStatusEnum.Expired.ToString()))
                        {
                            if (!String.IsNullOrEmpty(caOIC.Trim()) && caOIC.Trim() != "-" && caOIC.Trim() != "COS" && caOIC.Trim() != "cos")
                            {
                                recipientEmail = caOIC.Trim() + "@" + Retrieve.GetEmailDomain();

                                subject = "Verified documents for " + docAppRow.RefType.Trim() + " " + refNo + " (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                                message = "Verified documents (Set No. <a href='" + Retrieve.GetDWMSDomain() + "Verification/View.aspx?id=" + setId + "' target=_blank>" + setNumber + "</a>) for " + docAppRow.RefType.Trim() + " <a href='" + Retrieve.GetDWMSDomain() + "Completeness/View.aspx?id=" + docAppRow.Id.ToString() + "' target=_blank>" + refNo + "</a> (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                                message = message + "<br><br>You may view the image in DWMS using the link above and review the case, if necessary.";
                            }
                        }
                        else if (hleStatus.Equals(HleStatusEnum.Pending_Pre_E.ToString()) || hleStatus.Equals(HleStatusEnum.Complete_Pre_E_Check.ToString()))
                        {
                            recipientEmail = peOIC.Trim() + "@" + Retrieve.GetEmailDomain();

                            subject = "Verified documents for " + docAppRow.RefType.Trim() + " " + refNo + " (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                            message = "Verified documents (Set No. <a href='" + Retrieve.GetDWMSDomain() + "Verification/View.aspx?id=" + setId + "' target=_blank>" + setNumber + "</a>) for " + docAppRow.RefType.Trim() + " <a href='" + Retrieve.GetDWMSDomain() + "Completeness/View.aspx?id=" + docAppRow.Id.ToString() + "' target=_blank>" + refNo + "</a> (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                            message = message + "<br><br>You may view the image in DWMS using the link above and review the case, if necessary.";
                        }
                        else if (caOICfoundInUserList && (hleStatus.Equals(HleStatusEnum.KIV_CA.ToString()) || hleStatus.Equals(HleStatusEnum.KIV_Pre_E.ToString())))
                        {
                            if (!String.IsNullOrEmpty(caOIC.Trim()) && caOIC.Trim() != "-" && caOIC.Trim() != "COS" && caOIC.Trim() != "cos")
                            {
                                recipientEmail = caOIC.Trim() + "@" + Retrieve.GetEmailDomain();

                                subject = "Verified documents for " + docAppRow.RefType.Trim() + " " + refNo + " (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                                message = "Verified documents (Set No. <a href='" + Retrieve.GetDWMSDomain() + "Verification/View.aspx?id=" + setId + "' target=_blank>" + setNumber + "</a>) for " + docAppRow.RefType.Trim() + " <a href='" + Retrieve.GetDWMSDomain() + "Completeness/View.aspx?id=" + docAppRow.Id.ToString() + "' target=_blank>" + refNo + "</a> (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                                message = message + "<br><br>You may view the image in DWMS using the link above and review the case, if necessary.";
                            }
                        }
                        else if (caOICfoundInUserList && hleStatus.Equals(HleStatusEnum.Rejected.ToString()))
                        {
                            if (!String.IsNullOrEmpty(caOIC.Trim()) && caOIC.Trim() != "-" && caOIC.Trim() != "COS" && caOIC.Trim() != "cos")
                            {
                                //recipientEmail = Retrieve.GetHDBCreditEmailAddress() + "@" + Retrieve.GetEmailDomain();
                                recipientEmail = caOIC.Trim() + "@" + Retrieve.GetEmailDomain();

                                subject = "Verified documents for " + docAppRow.RefType.Trim() + " " + refNo + " (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                                message = "Verified documents (Set No. <a href='" + Retrieve.GetDWMSDomain() + "Verification/View.aspx?id=" + setId + "' target=_blank>" + setNumber + "</a>) for " + docAppRow.RefType.Trim() + " <a href='" + Retrieve.GetDWMSDomain() + "Completeness/View.aspx?id=" + docAppRow.Id.ToString() + "' target=_blank>" + refNo + "</a> (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                                message = message + "<br><br>Please look into the case.";
                                //message = "Please look into the case. The files are attached.";

                                //string errorMsg = string.Empty;
                                //pdfPath = Util.GeneratePdfPathBySetId(setId, out errorMsg);
                                //if (!String.IsNullOrEmpty(errorMsg)) message += " There is an error of attaching the files (" + errorMsg + ")<br/> Please contact DWMS Admin.";
                            }
                        }
                        else
                        {
                            recipientEmail = "MyDocErrLog@hdb.gov.sg";
                            subject = "(Not in Loop)Verified documents for " + docAppRow.RefType.Trim() + " " + refNo + " (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received " + peOIC.Trim();
                            message = "(Not in Loop)Verified documents (Set No. <a href='" + Retrieve.GetDWMSDomain() + "Verification/View.aspx?id=" + setId + "' target=_blank>" + setNumber + "</a>) for " + docAppRow.RefType.Trim() + " <a href='" + Retrieve.GetDWMSDomain() + "Completeness/View.aspx?id=" + docAppRow.Id.ToString() + "' target=_blank>" + refNo + "</a> (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received " + peOIC.Trim();
                            message = message + "<br><br>You may view the image in DWMS using the link above and review the case, if necessary.";
                        }
                    }
                    if (!String.IsNullOrEmpty(recipientEmail.Trim()))
                    {
                        ParameterDb parameterDb = new ParameterDb();

                        try
                        {
                            bool emailSent = Util.SendMail(parameterDb.GetParameterValue(ParameterNameEnum.SystemName).Trim(), parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(),
                                recipientEmail, ccEmail, string.Empty, string.Empty, subject, message, pdfPath);
                        }
                        catch (Exception e)
                        {
                            string errorSummary = string.Format("Sending email exception: Message={0}, StackTrace={1}", e.Message, e.StackTrace);
                            Util.DWMSLog("CategorizationManagerForDoc.StartCategorization", errorSummary, EventLogEntryType.Error);
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Parse the Web Service XML file
        /// </summary>
        /// <param name="setId">Set id</param>
        /// <returns>CustomerWebServiceInfo array</returns>
        private CustomerWebServiceInfo[] ParseWebServiceXml(int setId)
        {
            ParameterDb parameterDb = new ParameterDb();
            bool logging = parameterDb.Logging();
            bool detailLogging = parameterDb.DetailLogging();

            //Intends to caputre all the information from the "set.xml" file

            CustomerWebServiceInfo[] customers = new CustomerWebServiceInfo[0];

            DocSetDb docSetDb = new DocSetDb();

            // Get the XML contents
            string xmlContents = docSetDb.GetWebServiceXmlContents(setId);

            if (!String.IsNullOrEmpty(xmlContents))
            {
                try
                {
                    // Load the XML file
                    XmlDocument summaryXmlDoc = new XmlDocument();
                    summaryXmlDoc.LoadXml(xmlContents);

                    xmlContents = summaryXmlDoc.OuterXml;

                    // Get the reference number
                    XmlNodeList customerNode = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.WebServiceSetXmlCustomerTagName);
                    string refNo = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.WebServiceSetXmlSetTagName)[0][Constants.WebServiceSetXmlRefNoTagName].InnerText.Trim();

                    ArrayList customerList = new ArrayList();
                    foreach (XmlNode customer in customerNode)
                    {
                        CustomerWebServiceInfo customerWebServiceInfo = new CustomerWebServiceInfo();
                        //if (detailLogging) Util.DWMSLog("DWMS_OCR_Service.ParseWebServiceXml", "ParseWebServiceXml checking", EventLogEntryType.Warning);

                        customerWebServiceInfo.RefNo = refNo;
                        //customerWebServiceInfo.CustomerIdFromSource = customer[Constants.WebServiceSetXmlCustIdTagName].InnerText.Trim();
                        XmlNode xmlNodeCustIdTagName = customer[Constants.WebServiceSetXmlCustIdTagName];
                        if (xmlNodeCustIdTagName != null)
                            customerWebServiceInfo.CustomerIdFromSource = customer[Constants.WebServiceSetXmlCustIdTagName].InnerText.Trim();
                        else
                            customerWebServiceInfo.CustomerIdFromSource = string.Empty;
                        XmlNode xmlNodeCustName = customer[Constants.WebServiceSetXmlCustNameTagName];
                        if (xmlNodeCustName != null)
                            customerWebServiceInfo.Name = customer[Constants.WebServiceSetXmlCustNameTagName].InnerText.Trim();
                        else
                            customerWebServiceInfo.Name = string.Empty;
                        //customerWebServiceInfo.Name = customer[Constants.WebServiceSetXmlCustNameTagName].InnerText.Trim();
                        customerWebServiceInfo.Nric = customer[Constants.WebServiceSetXmlNricTagName].InnerText.Trim();
                        //customerWebServiceInfo.IdentityType = customer[Constants.WebServiceSetXmlIdTypeTagName].InnerText.Trim();
                        XmlNode IdTypeTag = customer[Constants.WebServiceSetXmlIdTypeTagName];
                        if (IdTypeTag != null)
                            customerWebServiceInfo.IdentityType = customer[Constants.WebServiceSetXmlIdTypeTagName].InnerText.Trim();
                        else
                            customerWebServiceInfo.IdentityType = string.Empty;
                        customerWebServiceInfo.CustomerType = customer[Constants.WebServiceSetXmlCustomerTypeTagName].InnerText.Trim();


                        ArrayList documentList = new ArrayList();
                        foreach (XmlNode documentNode in customer.ChildNodes)
                        {
                            if (documentNode.Name.ToUpper().Equals(Constants.WebServiceSetXmlDocTagName))
                            {
                                DocWebServiceInfo docWebServiceInfo = new DocWebServiceInfo();

                                // Get the DocId and DocSubId
                                docWebServiceInfo.DocId = documentNode[Constants.WebServiceSetXmlDocIdTagName].InnerText.Trim();
                                docWebServiceInfo.DocSubId = documentNode[Constants.WebServiceSetXmlDocSubIdTagName].InnerText.Trim();

                                XmlNode xmlNodeDocDescription = documentNode[Constants.WebServiceSetXmlDocDescriptionTagName];
                                if (xmlNodeDocDescription != null)
                                    docWebServiceInfo.DocDescription = xmlNodeDocDescription.InnerText.Trim();
                                else
                                    docWebServiceInfo.DocDescription = string.Empty;

                                XmlNode xmlNodeIdentityNoSub = documentNode[Constants.WebServiceSetXmlIdentityNoSubTagName];
                                if (xmlNodeIdentityNoSub != null)
                                    docWebServiceInfo.IdentityNoSub = xmlNodeIdentityNoSub.InnerText.Trim();
                                else
                                    docWebServiceInfo.IdentityNoSub = string.Empty;

                                XmlNode xmlNodeCustomerIdSubFromSource = documentNode[Constants.WebServiceSetXmlCustomerIdSubFromSourceTagName];
                                if (xmlNodeCustomerIdSubFromSource != null)
                                    docWebServiceInfo.CustomerIdSubFromSource = xmlNodeCustomerIdSubFromSource.InnerText.Trim();
                                else
                                    docWebServiceInfo.CustomerIdSubFromSource = string.Empty;

                                XmlNode xmlNodeDocStartDate = documentNode[Constants.WebServiceSetXmlDocStartDateTagName];
                                if (xmlNodeDocStartDate != null)
                                    docWebServiceInfo.DocStartDate = xmlNodeDocStartDate.InnerText.Trim();
                                else
                                    docWebServiceInfo.CustomerIdSubFromSource = string.Empty;

                                XmlNode xmlNodeDocEndDate = documentNode[Constants.WebServiceSetXmlDocEndDateTagName];
                                if (xmlNodeDocEndDate != null)
                                    docWebServiceInfo.DocEndDate = xmlNodeDocEndDate.InnerText.Trim();
                                else
                                    docWebServiceInfo.DocEndDate = string.Empty;


                                ArrayList fileList = new ArrayList();
                                foreach (XmlNode fileNode in documentNode.ChildNodes)
                                {
                                    if (fileNode.Name.ToUpper().Equals(Constants.WebServiceSetXmlFileTagName))
                                    {
                                        FileWebServiceInfo fileWebServiceInfo = new FileWebServiceInfo();

                                        // Get the file name
                                        fileWebServiceInfo.Name = fileNode[Constants.WebServiceSetXmlNameTagName].InnerText.Trim();

                                        ArrayList metaDataList = new ArrayList();
                                        foreach (XmlNode metadataNode in fileNode.ChildNodes)
                                        {
                                            if (metadataNode.Name.ToUpper().Equals(Constants.WebServiceSetXmlMetaDataTagName))
                                            {
                                                MetadataWebServiceInfo metadaWebServiceInfo = new MetadataWebServiceInfo();

                                                // Get the metadata details
                                                metadaWebServiceInfo.CertNo = metadataNode[Constants.WebServiceSetXmlCertNoTagName].InnerText.Trim();
                                                metadaWebServiceInfo.CertDate = metadataNode[Constants.WebServiceSetXmlCertDateTagName].InnerText.Trim();



                                                XmlNode xmlNodeLocalForeign = metadataNode[Constants.WebServiceSetXmlLocalForeignTagName];
                                                if (xmlNodeLocalForeign != null)
                                                    metadaWebServiceInfo.LocalForeign = xmlNodeLocalForeign.InnerText.Trim();
                                                else
                                                    metadaWebServiceInfo.LocalForeign = string.Empty;

                                                XmlNode xmlNodeMarriageType = metadataNode[Constants.WebServiceSetXmlMarriageTypeTagName];
                                                if (xmlNodeMarriageType != null)
                                                    metadaWebServiceInfo.MarriageType = xmlNodeMarriageType.InnerText.Trim();
                                                else
                                                    metadaWebServiceInfo.MarriageType = string.Empty;

                                                metadaWebServiceInfo.IsVerified = (!String.IsNullOrEmpty(metadataNode[Constants.WebServiceSetXmlIsVerifiedTagName].InnerText.Trim()) ?
                                                   bool.Parse(metadataNode[Constants.WebServiceSetXmlIsVerifiedTagName].InnerText.Trim()) :
                                                   false);

                                                metadaWebServiceInfo.IsAccepted = (!String.IsNullOrEmpty(metadataNode[Constants.WebServiceSetXmlIsAcceptedTagName].InnerText.Trim()) ?
                                                   bool.Parse(metadataNode[Constants.WebServiceSetXmlIsAcceptedTagName].InnerText.Trim()) :
                                                   false);

                                                XmlNode xmlNodeDocChannel = metadataNode[Constants.WebServiceSetXmlDocChannelTagName];
                                                if (xmlNodeDocChannel != null)
                                                    metadaWebServiceInfo.DocChannel = xmlNodeDocChannel.InnerText.Trim();
                                                else
                                                    metadaWebServiceInfo.DocChannel = string.Empty;

                                                XmlNode xmlNodeCmDocumentID = metadataNode[Constants.WebServiceSetXmlCmDocumentIdTagName];
                                                if (xmlNodeCmDocumentID != null)
                                                    metadaWebServiceInfo.CmDocumentID = xmlNodeCmDocumentID.InnerText.Trim();
                                                else
                                                    metadaWebServiceInfo.CmDocumentID = string.Empty;


                                                metaDataList.Add(metadaWebServiceInfo);

                                                break; // Each document should only have one set of metadata
                                            }
                                        }

                                        MetadataWebServiceInfo[] metadaWebServiceInfoArray = new MetadataWebServiceInfo[metaDataList.Count];

                                        for (int cnt = 0; cnt < metaDataList.Count; cnt++)
                                        {
                                            metadaWebServiceInfoArray[cnt] = (MetadataWebServiceInfo)metaDataList[cnt];
                                        }

                                        fileWebServiceInfo.Metadata = metadaWebServiceInfoArray;

                                        fileList.Add(fileWebServiceInfo);
                                    }
                                }

                                FileWebServiceInfo[] fileWebServiceInfoArray = new FileWebServiceInfo[fileList.Count];

                                for (int cnt = 0; cnt < fileList.Count; cnt++)
                                {
                                    fileWebServiceInfoArray[cnt] = (FileWebServiceInfo)fileList[cnt];
                                }

                                docWebServiceInfo.Files = fileWebServiceInfoArray;

                                documentList.Add(docWebServiceInfo);
                            }
                        }

                        DocWebServiceInfo[] docWebServiceInfoArray = new DocWebServiceInfo[documentList.Count];

                        for (int cnt = 0; cnt < documentList.Count; cnt++)
                        {
                            docWebServiceInfoArray[cnt] = (DocWebServiceInfo)documentList[cnt];
                        }

                        customerWebServiceInfo.Documents = docWebServiceInfoArray;

                        customerList.Add(customerWebServiceInfo);
                    }

                    customers = new CustomerWebServiceInfo[customerList.Count];

                    for (int cnt = 0; cnt < customerList.Count; cnt++)
                    {
                        customers[cnt] = (CustomerWebServiceInfo)customerList[cnt];
                    }
                }
                catch (Exception e)
                {
                    Util.DWMSLog("", e.Message, EventLogEntryType.Error);
                    Util.DWMSLog("", e.StackTrace, EventLogEntryType.Error);
                }
            }

            return customers;
        }

        #endregion

        class CustomerWebServiceInfo
        {
            public string RefNo { get; set; }
            public string CustomerIdFromSource { get; set; }
            public string Name { get; set; }
            public string Nric { get; set; }
            public string IdentityType { get; set; }
            public string CustomerType { get; set; }
            public DocWebServiceInfo[] Documents { get; set; }
        }

        class DocWebServiceInfo
        {
            //represents the DocumentClass Class in wsdl CDB->DWMS
            public string DocId { get; set; }
            public string DocSubId { get; set; }
            public string DocDescription { get; set; }
            public string CustomerIdSubFromSource { get; set; }
            public string IdentityNoSub { get; set; }
            public string DocStartDate { get; set; }
            public string DocEndDate { get; set; }

            public FileWebServiceInfo[] Files { get; set; }

        }

        class FileWebServiceInfo
        {
            public string Name { get; set; }
            public MetadataWebServiceInfo[] Metadata { get; set; }
        }

        class MetadataWebServiceInfo
        {
            //represents the ImageInfoClass Class in wsdl CDB->DWMS
            //Image Name, ImageURL, Image Size, DateReceivedFromSource, IsMatchedWithExternalOrg, DateFiled, not included even though the wsdl is passing to us

            public string CertNo { get; set; }
            public string CertDate { get; set; }
            public string LocalForeign { get; set; }
            public bool IsAccepted { get; set; }
            public string MarriageType { get; set; }
            public bool IsVerified { get; set; }
            public string DocChannel { get; set; }
            public string CmDocumentID { get; set; }

        }

    }
}
