using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Collections;
using DWMS_OCR.App_Code.Bll;
using System.IO;
using DWMS_OCR.App_Code.Helper;
using System.Threading;
using DWMS_OCR.App_Code.Dal;
using NHunspell;

namespace DWMS_OCR.OcrService
{
    partial class DWMS_SampleDocOCR_Service : ServiceBase
    {
        #region Members and Constructor
        /// <summary>
        /// Members
        /// </summary>

        private Semaphore semaphore;

        /// <summary>
        /// Constructor
        /// </summary>
        public DWMS_SampleDocOCR_Service()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists(Constants.DWMSSampleLogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(Constants.DWMSSampleLogSource, Constants.DWMSSampleLog);
            }

            eventLog.Source = Constants.DWMSSampleLogSource;
            eventLog.Log = Constants.DWMSSampleLog;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Windows service start
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            Util.SampleLog(string.Empty, "DWMS_SampleDocOCR_Service Started.", EventLogEntryType.Information);

            // Start the timer
            timer.Enabled = true;
            timer.Start();
        }

        /// <summary>
        /// Windows Service continue
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();

            Util.SampleLog(string.Empty, "DWMS_SampleDocOCR_Service Continued.", EventLogEntryType.Information);
            timer.Start();
        }

        /// <summary>
        /// Windows Service stop
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();

            Util.SampleLog(string.Empty, "DWMS_SampleDocOCR_Service Paused.", EventLogEntryType.Information);
            timer.Stop();
        }

        /// <summary>
        /// Windows Service Stop
        /// </summary>
        protected override void OnStop()
        {
            Util.SampleLog(string.Empty, "DWMS_SampleDocOCR_Service Stopped.", EventLogEntryType.Information);
            timer.Stop();
        }

        /// <summary>
        /// Windows Service shutdown
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();

            Util.SampleLog(string.Empty, "DWMS_SampleDocOCR_Service Shut down.", EventLogEntryType.Information);
            timer.Stop();
        }

        /// <summary>
        /// Timer elapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Stop the timer. Start only after the process has been completed
            timer.Stop();
            timer.Enabled = false;

            try
            {
                // Get the maximum thread
                ParameterDb parameterDb = new ParameterDb();
                int maxThread = parameterDb.GetMaximumThreadsForOcr();
                semaphore = new Semaphore(maxThread, maxThread);

                StartOcr();

                semaphore.Close();
                semaphore = null;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Error (DWMS_SampleDocOCR_Service): Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);

                Util.SampleLog("DWMS_SampleDocOCR_Service.timer_Elapsed", errorMessage, EventLogEntryType.Error);
            }
            finally
            {
                if (semaphore != null)
                {
                    semaphore.Close();
                    semaphore = null;
                }
            }

            // Start the timer again
            timer.Enabled = true;
            timer.Start();
        }
        #endregion

        #region Private Helpers
        /// <summary>
        /// Start the OCR
        /// </summary>
        private void StartOcr()
        {
            ArrayList pages = new ArrayList();
            ArrayList sampleDocIds = new ArrayList();
            ArrayList sampleDocDirs = new ArrayList();
            
            // Copy the confirmed documents as a sample document
            CopyConfirmedDocuments();

            // Save the sample pages of each document
            ProcessDocs(ref pages, ref sampleDocIds, ref sampleDocDirs);

            // Do the OCR for each page
            OcrSamplePages(pages, sampleDocIds);

            // Do clean-up of the sample docs that was processed
            CleanUp(sampleDocDirs);
        }

        /// <summary>
        /// Copy all the confirmed set documents as sample documents
        /// </summary>
        private void CopyConfirmedDocuments()
        {
            SampleDocDb sampleDocDb = new SampleDocDb();
            DocSetDb docSetDb = new DocSetDb();
            DocDb docDb = new DocDb();
            RawPageDb rawPageDb = new RawPageDb();
            RelevanceRankingDb relevanceRankingDb = new RelevanceRankingDb();

            // Get the verified sets
            DocSet.DocSetDataTable verifiedSetsDt = docSetDb.GetDocSetByStatusConvertedToSampleDoc(SetStatusEnum.Verified.ToString(), false);

            ArrayList setIds = new ArrayList();

            if (verifiedSetsDt.Rows.Count > 0)
            //foreach (DocSet.DocSetRow set in verifiedSetsDt.Rows)
            {
                DocSet.DocSetRow set = verifiedSetsDt[0];

                // Get the document types for each set
                Doc.DocDataTable docDt = docDb.GetDocBySetId(set.Id);

                foreach (Doc.DocRow doc in docDt.Rows)
                {
                    string docFolder = docDb.GetDocFolder(doc.Id);
                    string docContent = rawPageDb.GetDocContents(doc.Id);
                    int contentLen = docContent.Trim().Replace(Environment.NewLine, "").Replace(" ", "").Length;

                    if (doc.Status.Equals(DocStatusEnum.Verified.ToString()) &&
                        !doc.DocTypeCode.Equals(DocTypeEnum.Unidentified.ToString()) &&
                        !doc.ImageCondition.Trim().Equals(ImageConditionEnum.BlurSLASHIncomplete.ToString().Replace("SLASH", "/")) &&
                        !docFolder.Equals("BL") &&
                        !doc.ConvertedToSampleDoc &&
                        ((contentLen > Constants.MIN_STR_LENGTH) || (IsValidTextForRelevanceRanking(docContent.Trim()) && contentLen < Constants.MIN_STR_LENGTH)))
                    {

                        #region Added BY Edward 26/3/2014 Freeze Sample Documents
                        DocTypeDb docTypeDb = new DocTypeDb();
                        DocType.DocTypeDataTable docTypeDt = docTypeDb.GetDocTypeByCode(doc.DocTypeCode);

                        if (docTypeDt.Rows.Count > 0)
                        {
                            DocType.DocTypeRow docTypeRow = docTypeDt[0];
                            if (!docTypeRow.IsAcquireNewSamplesNull())
                            {
                                if (!docTypeRow.AcquireNewSamples)
                                    continue;
                            }
                        }
                        #endregion


                        Util.SampleLog(string.Empty, String.Format("SetId: {0}, DocId: {1}, DocType: {2}", set.Id.ToString(), doc.Id.ToString(), doc.DocTypeCode), EventLogEntryType.Information);

                        // Get the raw page for each document
                        RawPage.RawPageDataTable rawPageDt = rawPageDb.GetRawPageByDocId(doc.Id);

                        //ArrayList rawPages = new ArrayList();
                        //ArrayList rawPageIds = new ArrayList();
                        Dictionary<int, string> rawPageData = new Dictionary<int, string>();

                        foreach (RawPage.RawPageRow rawPage in rawPageDt.Rows)
                        {
                            // Check if match
                            RelevanceRanking.RelevanceRankingDataTable relRankDt = relevanceRankingDb.GetRelevanceRankingByRawPageId(rawPage.Id);

                            if (relRankDt.Rows.Count > 0)
                            {
                                RelevanceRanking.RelevanceRankingRow relRank = relRankDt[0];

                                if (sampleDocDb.GetSampleDocCode(relRank.SampleDocId).Equals(doc.DocTypeCode))
                                {
                                    // Update the relRank row
                                    relevanceRankingDb.Update(relRank.Id, true);
                                }

                                //// Add the path of the raw page for creation of new document
                                //rawPages.Add(Util.GetRawPageFilePath(rawPage.Id));
                                //rawPageIds.Add(rawPage.Id);
                            }

                            // Add the path of the raw page for creation of new document
                            //rawPages.Add(Util.GetRawPageFilePath(rawPage.Id));
                            //rawPageIds.Add(rawPage.Id);
                            rawPageData.Add(rawPage.Id, Util.GetRawPageFilePath(rawPage.Id));
                        }

                        //if (rawPages.Count > 0)
                        if (rawPageData.Count > 0)
                        {
                            //bool add = false;

                            //// Check if the number of sample documents for this document type
                            //// has reached the maximum allowed
                            //if (sampleDocDb.HasMaximumSampleDocument(doc.DocTypeCode))
                            //{
                            //    // Delete one sample document
                            //    add = relevanceRankingDb.DeleteLeastSampleDocument(doc.DocTypeCode);
                            //}
                            //else
                            //{
                            //    add = true;
                            //}

                            //if (add)
                            //{
                                // Create new sample documents for the raw pages
                                //CreateNewSampleDocument(doc, rawPages, rawPageIds, set.Id);
                                CreateNewSampleDocument(doc, rawPageData, set.Id);

                                // Update the flag to indicate that the document has been copied
                                // as a sample document
                                docDb.UpdateConvertedToSampleDocFlag(doc.Id, true);
                            //}
                        }
                    }                    
                }

                // Add the set id
                if (!setIds.Contains(set.Id))
                    setIds.Add(set.Id);
            }

            if (setIds.Count > 0)
            {
                foreach (int setId in setIds)
                {
                    // Update set id to prevent from copying the documents again
                    docSetDb.UpdateCopyToSampleDocFlag(setId, true);
                }
            }
        }

        /// <summary>
        /// Create the new sample document
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="rawPages"></param>
        //private void CreateNewSampleDocument(Doc.DocRow doc, ArrayList rawPages, ArrayList rawPageIds, int setId)
        private void CreateNewSampleDocument(Doc.DocRow doc, Dictionary<int, string> rawPageData, int setId)
        {
            #region NEW IMPLEMENTATION (1 SampleDoc to 1 SamplePage)
            // Create a document for each Raw Page
            SampleDocDb sampleDocDb = new SampleDocDb();
            SamplePageDb samplePageDb = new SamplePageDb();
            RawPageDb rawPageDb = new RawPageDb();

            // Create the folder for the sample document
            string sampleMainDir = Retrieve.GetSampleDocsForOcrDirPath();
            string sampleDocDir = Path.Combine(sampleMainDir, doc.DocTypeCode);

            //foreach (int rawPageId in rawPageIds)
            foreach (KeyValuePair<int, string> rawPageKeyValue in rawPageData)
            {
                int rawPageId = rawPageKeyValue.Key;

                RawPage.RawPageDataTable rawPageDt = rawPageDb.GetRawPageById(rawPageId);

                if (rawPageDt.Rows.Count > 0)
                {
                    RawPage.RawPageRow rawPage = rawPageDt[0];

                    // Create the document of the Raw Page
                    int sampleDocId = sampleDocDb.Insert(doc.DocTypeCode, "SampleDoc.pdf", new byte[0], true);

                    if (sampleDocId > 0)
                    {
                        // Create the filename of the new sample document
                        string tempFolder = Util.CreateTempFolder();
                        string fileName = Path.Combine(tempFolder, String.Format("{0}_{1}_{2}.pdf", doc.DocTypeCode, sampleDocId, setId));

                        // Create the PDF file of the document
                        //Util.MergePdfFiles(rawPages, fileName);
                        ArrayList temp = new ArrayList();
                        temp.Add(rawPageKeyValue.Value);
                        Util.MergePdfFiles(temp, fileName);

                        // Add the file as a new sample document
                        if (File.Exists(fileName))
                        {
                            FileInfo file = new FileInfo(fileName);

                            // Update the filename of the sample doc record
                            sampleDocDb.UpdateFileName(sampleDocId, file.Name);

                            // Create the sample doc folder of the document
                            string currentSampleDocDir = Path.Combine(sampleDocDir, sampleDocId.ToString());

                            if (!Directory.Exists(currentSampleDocDir))
                                Directory.CreateDirectory(currentSampleDocDir);

                            // Copy the merged file to the folder
                            string sampleDocFileName = Path.Combine(currentSampleDocDir, file.Name);
                            file.CopyTo(sampleDocFileName, true);

                            samplePageDb.Insert(sampleDocId, rawPage.OcrText, true);
                        }
                        else
                        {
                            // Delete the sample doc record
                            sampleDocDb.Delete(sampleDocId);
                        }
                    }
                }
            }
            #endregion

            #region OLD IMPLEMENTATION (1 SampleDoc to many SamplePages)
            //// Add the sample doc record
            //SampleDocDb sampleDocDb = new SampleDocDb();
            //int sampleDocId = sampleDocDb.Insert(doc.DocTypeCode, "SampleDoc.pdf", new byte[0], true);

            //// Create the filename of the new sample document
            //string tempFolder = Util.CreateTempFolder();
            //string fileName = Path.Combine(tempFolder, String.Format("{0}_{1}_{2}.pdf", doc.DocTypeCode, sampleDocId, setId));

            //// Merge the pages
            //Util.MergePdfFiles(rawPages, fileName);

            //// Add the file as a new sample document
            //if (File.Exists(fileName))
            //{
            //    FileInfo file = new FileInfo(fileName);

            //    // Update the filename of the sample doc record
            //    sampleDocDb.UpdateFileName(sampleDocId, file.Name);

            //    // Create the folder for the sample document
            //    string sampleMainDir = Retrieve.GetSampleDocsForOcrDirPath();
            //    string sampleDocDir = Path.Combine(sampleMainDir, doc.DocTypeCode);
            //    sampleDocDir = Path.Combine(sampleDocDir, sampleDocId.ToString());

            //    if (!Directory.Exists(sampleDocDir))
            //        Directory.CreateDirectory(sampleDocDir);

            //    // Copy the merged file to the folder
            //    string sampleDocFileName = Path.Combine(sampleDocDir, file.Name);
            //    file.CopyTo(sampleDocFileName, true);

            //    // Copy the raw page OCR text as a new sample page
            //    SamplePageDb samplePageDb = new SamplePageDb();
            //    RawPageDb rawPageDb = new RawPageDb();
            //    foreach (int rawPageId in rawPageIds)
            //    {
            //        RawPage.RawPageDataTable rawPageDt = rawPageDb.GetRawPageById(rawPageId);

            //        if (rawPageDt.Rows.Count > 0)
            //        {
            //            RawPage.RawPageRow rawPage = rawPageDt[0];

            //            samplePageDb.Insert(sampleDocId, rawPage.OcrText, true);
            //        }
            //    }
            //}
            //else
            //{
            //    // Delete the sample doc record
            //    sampleDocDb.Delete(sampleDocId);
            //}
            #endregion
        }

        /// <summary>
        /// Process the documents
        /// </summary>
        /// <param name="pages"></param>
        private void ProcessDocs(ref ArrayList pages, ref ArrayList sampleDocIds, ref ArrayList sampleDocDirs)
        {            
            // Get the storage of the documents to be OCR'ed
            string forOcrDocDirPath = Retrieve.GetSampleDocsForOcrDirPath();
            DirectoryInfo mainDirInfo = new DirectoryInfo(forOcrDocDirPath);

            // Get the doc type directories
            DirectoryInfo[] docTypeDirInfo = mainDirInfo.GetDirectories();

            // Loop through each doc type dir (e.g., HLE, CPF and many more)
            foreach (DirectoryInfo docTypeDir in docTypeDirInfo)
            {
                string docTypeCode = docTypeDir.Name;

                if (!docTypeCode.Equals(DocTypeEnum.Unidentified.ToString()))
                {
                    // Get the sample doc directories
                    DirectoryInfo[] docTypeSampleDocDirInfo = docTypeDir.GetDirectories();

                    // Loop through each sample doc (e.g, 1, 2, 3, ... , n)
                    foreach (DirectoryInfo individualSampleDocDir in docTypeSampleDocDirInfo)
                    {
                        int sampleDocId = -1;

                        if (int.TryParse(individualSampleDocDir.Name, out sampleDocId))
                        {
                            // OCR the sample documents
                            SampleDocDb sampleDocDb = new SampleDocDb();
                            if (!sampleDocDb.IsSampleDocOcr(sampleDocId) && sampleDocDb.GetSampleDocById(sampleDocId).Rows.Count > 0)
                            {
                                //// Add the dir to the list
                                //sampleDocDirs.Add(sampleDocDir.FullName);

                                // Save the pages of the sample document
                                SaveSamplePages(individualSampleDocDir, sampleDocId, docTypeCode, docTypeDir,
                                    ref pages, ref sampleDocIds, ref sampleDocDirs);

                                //sampleDocIds.Add(sampleDocId);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save the raw pages
        /// </summary>
        /// <param name="individualSampleDocDir"></param>
        /// <param name="pagesForOcr"></param>
        private void SaveSamplePages(DirectoryInfo individualSampleDocDir, int sampleDocId, string docTypeCode, DirectoryInfo docTypeSampleDocDirInfo,
            ref ArrayList pagesForOcr, ref ArrayList processedSampleDocIds, ref ArrayList sampleDocDirsForCleanUp)
        {
            #region NEW IMPLEMENTATION (1 SampleDoc to 1 SamplePage)
            SampleDocDb sampleDocDb = new SampleDocDb();
            SamplePageDb samplePageDb = new SamplePageDb();

            try
            {
                // Get the raw file
                FileInfo[] Rawfiles = individualSampleDocDir.GetFiles();

                if (Rawfiles.Count() > 0)
                {
                    FileInfo rawFile = Rawfiles[0];

                    // Split the PDF files into pages
                    if (rawFile.Extension.ToUpper().Equals(".PDF"))
                    {
                        #region PDF File
                        // Create a temporary folder of the split pages
                        string splitPageTempFolder = Util.CreateTempFolder();

                        //ArrayList pdfArrayList = Util.PdfSplit(file.FullName, sampleDocDir.FullName, 1);
                        ArrayList splitPdfArrayList = Util.PdfSplit(rawFile.FullName, splitPageTempFolder, 1);
                        //ArrayList splitPdfArrayList = Util.SaveAsImageUsingAcdPdf(rawFile.FullName);

                        // Add the PDF file paths to the array
                        bool isFirstPage = true;
                        foreach (string indSplitPdfPage in splitPdfArrayList)
                        {
                            FileInfo indSplitPdfFile = new FileInfo(indSplitPdfPage);

                            #region If the PDF is the first page, use the sample doc record already created. Else, create a sample doc record for the page.
                            int currentSampleDocId = -1;

                            if (isFirstPage)
                            {
                                isFirstPage = false;
                                currentSampleDocId = sampleDocId;
                            }
                            else
                            {
                                // Create the document of the Raw Page
                                currentSampleDocId = sampleDocDb.Insert(docTypeCode, indSplitPdfFile.Name, new byte[0], false);
                            }
                            #endregion

                            if (currentSampleDocId > 0)
                            {
                                #region Create the document for the sample doc record
                                // Create the filename of the new sample document
                                string tempFolder = Util.CreateTempFolder();
                                string currentSampleDocFilePath = Path.Combine(tempFolder, String.Format("{0}_{1}.pdf", indSplitPdfFile.Name, currentSampleDocId));

                                // Create the PDF file of the document
                                ArrayList pdfArrayListTemp = new ArrayList();
                                pdfArrayListTemp.Add(indSplitPdfFile.FullName);
                                Util.MergePdfFiles(pdfArrayListTemp, currentSampleDocFilePath);
                                #endregion

                                // Add the file as a new sample document
                                if (File.Exists(currentSampleDocFilePath))
                                {
                                    #region Save the Sample Doc
                                    FileInfo currentSampleDocFile = new FileInfo(currentSampleDocFilePath);

                                    // Update the filename of the sample doc record
                                    sampleDocDb.UpdateFileName(currentSampleDocId, currentSampleDocFile.Name);

                                    // Create the sample doc folder of the document
                                    string currentSampleDocDirTemp = Path.Combine(docTypeSampleDocDirInfo.FullName, currentSampleDocId.ToString());

                                    if (!Directory.Exists(currentSampleDocDirTemp))
                                        Directory.CreateDirectory(currentSampleDocDirTemp);

                                    // Copy the merged file to the folder
                                    string sampleDocFileName = Path.Combine(currentSampleDocDirTemp, currentSampleDocFile.Name);
                                    currentSampleDocFile.CopyTo(sampleDocFileName, true);

                                    // Save the sample page                                
                                    int samplePageId = samplePageDb.Insert(currentSampleDocId, string.Empty, false);

                                    if (samplePageId > 0)
                                    {
                                        #region Save the Sample page
                                        //string imagePath = Util.SaveAsTiffImage(indSplitPdfPage); -- Converting to image
                                        //FileInfo imageFile = new FileInfo(imagePath); -- Converting to image

                                        // Copy the file into a folder that has a name equal to the SamplePage Id
                                        string samplePageTempPath = Path.Combine(currentSampleDocDirTemp, samplePageId.ToString());
                                        string newSamplePageTempPath = Path.Combine(samplePageTempPath, indSplitPdfFile.Name);
                                        //string newSamplePageTempPath = Path.Combine(samplePageTempPath, imageFile.Name); -- Converting to image

                                        // If the folder does not exists, create one
                                        if (!Directory.Exists(samplePageTempPath))
                                            Directory.CreateDirectory(samplePageTempPath);

                                        // Move the file
                                        indSplitPdfFile.CopyTo(newSamplePageTempPath);
                                        //imageFile.MoveTo(newSamplePageTempPath); -- Converting to image

                                        // Add the page to the page list
                                        string[] pageData = new string[2];
                                        pageData[0] = samplePageId.ToString();
                                        pageData[1] = newSamplePageTempPath;
                                        pagesForOcr.Add(pageData);
                                        #endregion

                                        // Add the dir to the list of directories to be cleaned-up
                                        sampleDocDirsForCleanUp.Add(currentSampleDocDirTemp);

                                        // Add the sample doc id to the list of samples to be updated after processing
                                        processedSampleDocIds.Add(currentSampleDocId);
                                    }
                                    else
                                    {
                                        // Delete the sample doc record
                                        sampleDocDb.Delete(currentSampleDocId);
                                    }
                                    #endregion
                                }
                                else
                                {
                                    // Delete the sample doc record
                                    sampleDocDb.Delete(currentSampleDocId);
                                }
                            }
                        }

                        try
                        {
                            rawFile.Delete();
                        }
                        catch(Exception ex)
                        {
                            string warningMessage = string.Format("Error (DWMS_SampleDocOCR.Service.SaveSamplePages): Message={0}", ex.Message);

                            Util.SampleLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                        }
                        #endregion
                    }
                    else
                    {
                        #region Image File
                        // Save the sample page                            
                        int samplePageId = samplePageDb.Insert(sampleDocId, string.Empty, false);

                        if (samplePageId > 0)
                        {
                            //string imagePath = Util.SaveAsTiffImage(rawFile.FullName); -- Convert to image
                            //FileInfo imageFile = new FileInfo(imagePath); -- Convert to image

                            // Copy the file into a folder that has a name equal to the SamplePage Id
                            string samplePageTempPath = Path.Combine(individualSampleDocDir.FullName, samplePageId.ToString());
                            string newSamplePageTempPath = Path.Combine(samplePageTempPath, rawFile.Name);
                            //string newSamplePageTempPath = Path.Combine(samplePageTempPath, imageFile.Name); -- Convert to image

                            // If the folder does not exists, create one
                            if (!Directory.Exists(samplePageTempPath))
                                Directory.CreateDirectory(samplePageTempPath);

                            // Move the file
                            rawFile.CopyTo(newSamplePageTempPath);
                            //imageFile.MoveTo(newSamplePageTempPath); -- Convert to image

                            // Add the page to the page list
                            string[] pageData = new string[2];
                            pageData[0] = samplePageId.ToString();
                            pageData[1] = newSamplePageTempPath;
                            pagesForOcr.Add(pageData);

                            // Add the dir to the list of directories to be cleaned-up
                            sampleDocDirsForCleanUp.Add(individualSampleDocDir.FullName);

                            // Add the sample doc id to the list of samples to be updated after processing
                            processedSampleDocIds.Add(sampleDocId);
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format("Error (DWMS_SampleDocOCR.Service.SaveSamplePages): Message={0}"
                    , ex.Message);

                Util.SampleLog("DWMS_SampleDocOCR.Service.SaveSamplePages", errorMessage, EventLogEntryType.Error);
            }
            #endregion

            #region OLD IMPLEMENTATION (1 SampleDoc to many SamplePages)
            //SamplePageDb samplePageDb = new SamplePageDb();

            //try
            //{
            //    // Get the raw file
            //    FileInfo[] Rawfiles = sampleDocDir.GetFiles();

            //    if (Rawfiles.Count() > 0)
            //    {
            //        FileInfo file = Rawfiles[0];

            //        // Split the PDF files into pages
            //        if (file.Extension.ToUpper().Equals(".PDF"))
            //        {
            //            #region PDF File
            //            ArrayList pdfArrayList = Util.PdfSplit(file.FullName, sampleDocDir.FullName, 1);

            //            // Add the PDF file paths to the array
            //            foreach (string pdfPagePath in pdfArrayList)
            //            {
            //                FileInfo pdfFile = new FileInfo(pdfPagePath);

            //                // Save the sample page                                
            //                int samplePageId = samplePageDb.Insert(sampleDocId, string.Empty, false);

            //                if (samplePageId > 0)
            //                {
            //                    string imagePath = Util.SaveAsImage(pdfPagePath);

            //                    FileInfo imageFile = new FileInfo(imagePath);

            //                    // Copy the file into a folder that has a name equal to the SamplePage Id
            //                    string samplePageTempPath = Path.Combine(sampleDocDir.FullName, samplePageId.ToString());
            //                    string newSamplePageTempPath = Path.Combine(samplePageTempPath, imageFile.Name);

            //                    // If the folder does not exists, create one
            //                    if (!Directory.Exists(samplePageTempPath))
            //                        Directory.CreateDirectory(samplePageTempPath);

            //                    // Move the file
            //                    imageFile.MoveTo(newSamplePageTempPath);

            //                    // Add the page to the page list
            //                    string[] pageData = new string[2];
            //                    pageData[0] = samplePageId.ToString();
            //                    pageData[1] = newSamplePageTempPath;
            //                    pages.Add(pageData);
            //                }

            //                // Delete the individual page
            //                try
            //                {
            //                    pdfFile.Delete();
            //                }
            //                catch (Exception)
            //                {
            //                }
            //            }
            //            #endregion
            //        }
            //        else
            //        {
            //            #region Image File
            //            // Save the sample page                            
            //            int samplePageId = samplePageDb.Insert(sampleDocId, string.Empty, false);

            //            if (samplePageId > 0)
            //            {                            
            //                string imagePath = Util.SaveAsImage(file.FullName);

            //                FileInfo imageFile = new FileInfo(imagePath);

            //                // Copy the file into a folder that has a name equal to the SamplePage Id
            //                string samplePageTempPath = Path.Combine(sampleDocDir.FullName, samplePageId.ToString());
            //                string newSamplePageTempPath = Path.Combine(samplePageTempPath, imageFile.Name);

            //                // If the folder does not exists, create one
            //                if (!Directory.Exists(samplePageTempPath))
            //                    Directory.CreateDirectory(samplePageTempPath);

            //                // Move the file
            //                imageFile.MoveTo(newSamplePageTempPath);

            //                // Add the page to the page list
            //                string[] pageData = new string[2];
            //                pageData[0] = samplePageId.ToString();
            //                pageData[1] = newSamplePageTempPath;
            //                pages.Add(pageData);
            //            }
            //            #endregion
            //        }                        
            //    }
            //}
            //catch (Exception ex)
            //{
            //    string errorMessage = string.Format("Error (DWMS_SampleDocOCR.Service.SaveSamplePages): Message={0}"
            //        , ex.Message);

            //    Util.SampleLogError("DWMS_SampleDocOCR.Service.SaveSamplePages", errorMessage, DateTime.Now);
            //}
            #endregion
        }

        /// <summary>
        /// Ocr the sample pages
        /// </summary>
        /// <param name="pages"></param>
        private void OcrSamplePages(ArrayList pages, ArrayList sampleDocIds)
        {
            int threadCount = pages.Count;

            if (threadCount > 0)
            {
                ParameterDb parameterDb = new ParameterDb();
                int binarize = parameterDb.GetOcrBinarize();
                int bgFactor = parameterDb.GetOcrBackgroundFactor();
                int fgFactor = parameterDb.GetOcrForegroundFactor();
                int quality = parameterDb.GetOcrQuality();
                string morph = parameterDb.GetOcrMorph();
                bool dotMatrix = parameterDb.GetOcrDotMatrix();
                int despeckle = parameterDb.GetOcrDespeckle();

                Thread[] threads = new Thread[threadCount];

                // Assign threads for each page
                for (int index = 0; index < threadCount; index++)
                {
                    string[] pageData = (string[])pages[index];
                    int samplePageId = int.Parse(pageData[0]);
                    string filePath = pageData[1].ToString();

                    SampleDocThreadInfo sampleDocThreadInfo = new SampleDocThreadInfo();
                    sampleDocThreadInfo.SamplePageId = samplePageId;
                    sampleDocThreadInfo.FilePath = filePath;
                    sampleDocThreadInfo.Binarize = binarize;
                    sampleDocThreadInfo.BackgroundFactor = bgFactor;
                    sampleDocThreadInfo.ForegroundFactor = fgFactor;
                    sampleDocThreadInfo.Quality = quality;
                    sampleDocThreadInfo.Morph = morph;
                    sampleDocThreadInfo.DotMatrix = dotMatrix;
                    sampleDocThreadInfo.Despeckle = despeckle;

                    Thread thread = new Thread(new ParameterizedThreadStart(SampleDocOcrThreadCallback));
                    threads[index] = thread;
                    thread.Start(sampleDocThreadInfo);  
                }

                // Wait for all threads to finish
                foreach (Thread thread in threads)
                {
                    thread.Join();
                }

                // Update the sample doc
                SampleDocDb sampleDocDb = new SampleDocDb();                
                foreach (int sampleDocId in sampleDocIds)
                {
                    sampleDocDb.Update(sampleDocId, true);
                }
            }
        }

        /// <summary>
        /// Sample Doc OCR Thread callback
        /// </summary>
        /// <param name="parameter"></param>
        private void SampleDocOcrThreadCallback(object parameter)
        {
            SampleDocThreadInfo sampleDocThreadInfo = parameter as SampleDocThreadInfo;
            int samplePageId = sampleDocThreadInfo.SamplePageId;
            string filePath = sampleDocThreadInfo.FilePath;

            int binarize = sampleDocThreadInfo.Binarize;
            int bgFactor = sampleDocThreadInfo.BackgroundFactor;
            int fgFactor = sampleDocThreadInfo.ForegroundFactor;
            int quality = sampleDocThreadInfo.Quality;
            string morph = sampleDocThreadInfo.Morph;
            bool dotMatrix = sampleDocThreadInfo.DotMatrix;
            int despeckle = sampleDocThreadInfo.Despeckle;

            try
            {
                // Wait until the thread is assigned a resource
                semaphore.WaitOne();

                string ocrText = string.Empty;
                bool result = false;

                string tempContents = string.Empty;

                if (new FileInfo(filePath).Extension.ToUpper().Equals(".PDF"))
                {
                    int errorCode = Util.ExtractTextFromSearcheablePdf(filePath, null, true, out tempContents);

                    if (errorCode < 0)
                    {
                        //ArrayList imageArrayList = Util.SavePdfToIndividualImage(filePath);

                        //if (imageArrayList.Count > 0)
                        //    filePath = (string)imageArrayList[0];

                        tempContents = string.Empty;
                    }
                }

                // Assign 3 chances for the OCR to complete the process
                for (int cnt = 0; cnt < 3; cnt++)
                {
                    if (String.IsNullOrEmpty(tempContents))
                    {
                        OcrManager ocrManager = new OcrManager(filePath, -1, binarize, bgFactor, fgFactor, quality, morph, dotMatrix, despeckle);
                        result = ocrManager.GetOcrTextWithoutPdf(filePath, out ocrText);
                        ocrManager.Dispose();
                    }
                    else
                    {
                        ocrText = tempContents;
                        result = true;
                    }

                    if (result)
                    {
                        SamplePageDb samplePageDb = new SamplePageDb();
                        samplePageDb.Update(samplePageId, ocrText, true);

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                string errorSummary = string.Format("Error (DWMS_SampleDocOCR_Service.ThreadPoolCallback): File={0}, Message={1}, StackTrace={2}"
                    , filePath, e.Message, e.StackTrace);

                Util.SampleLog("DWMS_SampleDocOCR_Service.ThreadPoolCallback", errorSummary, EventLogEntryType.Error);

                try
                {
                    RawPageDb rawPageDb3 = new RawPageDb();
                    rawPageDb3.Update(samplePageId, true);
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                try
                {
                    // Delete the image (TIFF) file
                    FileInfo imageFile = new FileInfo(filePath);
                    imageFile.Delete();
                }
                catch (Exception)
                {
                }

                // Release the resource for this thread
                semaphore.Release();
            }
        }

        /// <summary>
        /// Remove all other files/sub-directories created for this sample document
        /// </summary>
        /// <param name="sampleDocDirs"></param>
        private void CleanUp(ArrayList sampleDocDirs)
        {
            foreach (string sampleDocDirStr in sampleDocDirs)
            {
                DirectoryInfo sampleDocDir = new DirectoryInfo(sampleDocDirStr);

                DirectoryInfo[] subDirs = sampleDocDir.GetDirectories();

                foreach (DirectoryInfo subDir in subDirs)
                {
                    try
                    {
                        subDir.Delete(true);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private bool IsValidTextForRelevanceRanking(string ocrText)
        {
            ParameterDb parameterDb = new ParameterDb();
            int MINIMUM_ENGLISH_WORD_COUNT = parameterDb.GetMinimumEnglishWordCount();
            decimal MINIMUM_ENGLISH_WORD_PERCENTAGE = parameterDb.GetMinimumEnglishWordPercentage();
            int MINIMUM_WORD_LENGTH = parameterDb.GetMinimumWordLength();

            string libAffPath = string.Empty;
            string libDicPath = string.Empty;

            Retrieve.GetHunspellResourcesPath(out libAffPath, out libDicPath);

            Hunspell spellChecker = new Hunspell(libAffPath, libDicPath);

            string[] arr = Util.SplitString(ocrText, true, true);
            //decimal wordCount = (decimal)arr.Length;

            decimal wordCount = 0;

            foreach (string word in arr)
            {
                if (word.Length >= MINIMUM_WORD_LENGTH)
                    wordCount++;
            }

            if (wordCount == 0)
                return false;

            decimal englishWordCount = 0;

            foreach (string word in arr)
            {
                try
                {
                    if (word.Length >= MINIMUM_WORD_LENGTH && spellChecker.Spell(word))
                        englishWordCount++;
                }
                catch (Exception ex)
                {
                    string errorString = String.Format("Error (DWMS_SampleDocOCR_Service.IsValidTextForRelevanceRanking): Message={0}, StackTrace={1}",
                        ex.Message, ex.StackTrace);

                    Util.SampleLog("DWMS_SampleDocOCR_Service.IsValidTextForRelevanceRanking", errorString, EventLogEntryType.Error);
                }
            }

            if (englishWordCount < MINIMUM_ENGLISH_WORD_COUNT)
                return false;

            if (englishWordCount / wordCount < MINIMUM_ENGLISH_WORD_PERCENTAGE)
                return false;

            return true;
        }
        #endregion
    }

    class SampleDocThreadInfo
    {
        public int SamplePageId { get; set; }
        public string FilePath { get; set; }

        // OCR Parameters
        public int Binarize { get; set; }
        public string Morph { get; set; }
        public int BackgroundFactor { get; set; }
        public int ForegroundFactor { get; set; }
        public int Quality { get; set; }
        public bool DotMatrix { get; set; }
        public int Despeckle { get; set; }
    }
}
