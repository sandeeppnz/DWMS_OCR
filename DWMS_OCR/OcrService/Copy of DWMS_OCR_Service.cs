using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using DWMS_OCR.App_Code.Bll;
using System.IO;
using System.Collections;
using DWMS_OCR.App_Code.Helper;
using System.Threading;
using DWMS_OCR.App_Code.Dal;
using NHunspell;
using System.Xml;

namespace DWMS_OCR.OcrService
{
    partial class DWMS_OCR_Service : ServiceBase
    {
        #region Members and Constructor
        /// <summary>
        /// Members
        /// </summary>
        private Semaphore semaphore; // Semaphore for regulating the number of threads to run at the same time
        private ArrayList categorizationSamplePages; // Container of all the sample documents
        private Guid? importedBy; // SYSTEM Guid

        /// <summary>
        /// Constructor
        /// </summary>
        public DWMS_OCR_Service()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists(Constants.DWMSLogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(Constants.DWMSLogSource, Constants.DWMSLog);
            }

            eventLog.Source = Constants.DWMSLogSource;
            eventLog.Log = Constants.DWMSLog;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Windows Service Start event.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            // Disable the timer and stop it
            timer.Stop();
            timer.Enabled = false;

            Util.DWMSLog(string.Empty, "DWMS_OCR_Service Started.", EventLogEntryType.Information);

            // Get the SYSTEM GUID.  This will be used for all the imported sets.
            importedBy = Retrieve.GetSystemGuid();

            // ##### Do the clean up of the sample pages and retrieval of sample pages here #####
            // ##### so that this action will be done only once through the life of the Windows Service. #####

            // This will remove the lowest ranked sample pages
            Util.DWMSLog(string.Empty, "Removing Sample Documents.", EventLogEntryType.Information);
            RemoveLeastMatchedSampleDocuments();

            // Load the sample docs/pages
            Util.DWMSLog(string.Empty, "Loading Sample Pages.", EventLogEntryType.Information);
            categorizationSamplePages = LoadSamplePagesReturnDictionary();

            Util.DWMSLog(string.Empty, "Sample Pages Loaded.", EventLogEntryType.Information);

            // Enable the timer and start it
            timer.Enabled = true;
            timer.Start();
        }

        /// <summary>
        /// Windows Service continue event.
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();

            Util.DWMSLog(string.Empty, "DWMS_OCR_Service Continued.", EventLogEntryType.Information);
            timer.Start();
        }

        /// <summary>
        /// Windows Service stop event.
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();

            Util.DWMSLog(string.Empty, "DWMS_OCR_Service Paused.", EventLogEntryType.Information);
            timer.Stop();
        }

        /// <summary>
        /// Windows Service Stop event.
        /// </summary>
        protected override void OnStop()
        {
            Util.DWMSLog(string.Empty, "DWMS_OCR_Service Stopped.", EventLogEntryType.Information);

            timer.Stop();
        }

        /// <summary>
        /// Windows Service shutdown event.
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();

            Util.DWMSLog(string.Empty, "DWMS_OCR_Service Shut down.", EventLogEntryType.Information);
            timer.Stop();
        }

        /// <summary>
        /// Timer elapsed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Stop the timer. Start only after all the processes have been completed.
            timer.Stop();
            timer.Enabled = false;

            try
            {
                // Get the ForOcr directory
                string forOcrDocDirPath = Retrieve.GetDocsForOcrDirPath();
                DirectoryInfo mainDirInfo = new DirectoryInfo(forOcrDocDirPath);

                // Get the maximum thread count
                ParameterDb parameterDb = new ParameterDb();
                int maxThread = parameterDb.GetMaximumThreadsForOcr();
                semaphore = new Semaphore(maxThread, maxThread);

                // Delete the failed sets. Failed set fall into two categories:
                // 1. With physical files but with no set information in the database;
                // 2. With set information but with no physical files.
                DeleteFailedSets(mainDirInfo);

                // Process the sets uploaded from sources other than the IMPORTING function
                ProcessMyDoc(mainDirInfo); // Process the sets uploaded through MyDoc
                ProcessFax(mainDirInfo); // Process the sets uploaded through Fax
                ProcessScan(mainDirInfo); // Process the sets uploaded through Scan
                ProcessEmail(mainDirInfo); // Process the sets uploaded through Email
                ProcessWebService(mainDirInfo); // Process the sets uploaded through Web Service

                // Do the OCR on the selected set
                List<int> setIds = StartOcr(mainDirInfo);

                // Categorize the set
                StartCategorization(setIds);

                SetIsBeingProcessed(setIds);

                semaphore.Close();
                semaphore = null;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Error (DWMS_OCR_Service.timer_Elapsed): Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);

                Util.DWMSLog("DWMS_OCR_Service.timer_Elapsed", errorMessage, EventLogEntryType.Error);
            }
            finally
            {
                if (semaphore != null)
                {
                    semaphore.Close();
                    semaphore = null;
                }
            }

            // Start the timer again.
            timer.Enabled = true;
            timer.Start();
        }
        #endregion

        #region Private Methods
        #region MyDoc Processes
        /// <summary>
        /// Process the MyDoc directory.
        /// </summary>
        /// <param name="mainDirInfo">MyDoc directory</param>
        private void ProcessMyDoc(DirectoryInfo mainDirInfo)
        {
            // Get the MyDoc directory
            string myDocsDirPath = Retrieve.GetMyDocForOcrDirPath();
            DirectoryInfo myDocMainDirInfo = new DirectoryInfo(myDocsDirPath);

            // Get the MyDoc/Imported directory
            string importedDocsDirPath = Retrieve.GetImportedMyDocsOcrDirPath();
            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);
            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);

            // Get the MyDoc/Failed directory
            string failedDocsDirPath = Retrieve.GetFailedMyDocsOcrDirPath();
            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            // Get the subfolders of the ForOcr directory
            DirectoryInfo[] subDirInfos = myDocMainDirInfo.GetDirectories();

            ArrayList subDirToMove = new ArrayList();

            #region Process the Sub-Directories
            // Loop through each subdirectory
            foreach (DirectoryInfo subDirInfo in subDirInfos)
            {
                if (!subDirInfo.Name.ToLower().Equals("imported") &&
                    !subDirInfo.Name.ToLower().Equals("failed") &&
                    !subDirInfo.Name.ToLower().EndsWith("_imported") &&
                    !subDirInfo.Name.ToLower().EndsWith("_failed"))
                {
                    // For each dir, get the XML file to get info about the set
                    FileInfo[] xmlFiles = subDirInfo.GetFiles(Constants.MyDocSummaryXmlFileName);

                    // Get the file
                    if (xmlFiles.Length > 0)
                    {
                        FileInfo xmlFile = xmlFiles[0];

                        // Check the age of the directory
                        // If the age is greater than or equal to age being set in the parameters page, process the directory
                        ParameterDb parameterDb = new ParameterDb();
                        TimeSpan difference = DateTime.Now.Subtract(subDirInfo.CreationTime);

                        if (difference.TotalMinutes >= parameterDb.GetMinimumAgeForExternalFiles())
                        {
                            // Parse the XML file to retrieve the data
                            MyDocSummaryXml summaryXml = new MyDocSummaryXml(xmlFile.FullName, subDirInfo.FullName);

                            string newDir = string.Empty;
                            string failReason = string.Empty;
                            string exceptionMessage = string.Empty;

                            if (summaryXml.IsValid)
                            {
                                // Create the set record of the directory
                                int setId = -1;
                                bool success = CreateSet(SourceFileEnum.MyDoc, mainDirInfo, summaryXml, null, new ArrayList(), string.Empty,
                                    string.Empty, string.Empty, string.Empty, false, out setId);

                                if (success)
                                {
                                    // Log the information that the set has been created for the directory
                                    Util.DWMSLog(string.Empty, String.Format("Set Created for {0}(MyDoc): Id={1}", subDirInfo.Name, setId), EventLogEntryType.Information);

                                    newDir = Path.Combine(importedDirInfo.FullName, subDirInfo.Name);
                                }
                                else
                                {
                                    // If the creation of the set was unsuccessful, move the directory to the 'Failed' folder
                                    newDir = Path.Combine(failedDocsDirPath, subDirInfo.Name);

                                    failReason = "Failed to create set.";
                                    exceptionMessage = "Failed to create set.";
                                }
                            }
                            else
                            {
                                // If there are errors when parsing the XML file, move the directory to the 'Failed' folder
                                newDir = Path.Combine(failedDocsDirPath, subDirInfo.Name);

                                failReason = summaryXml.GenericError;
                                exceptionMessage = summaryXml.ExceptionMessage;
                            }

                            if (!String.IsNullOrEmpty(newDir))
                            {
                                if (Directory.Exists(newDir))
                                {
                                    newDir = newDir + Guid.NewGuid().ToString().Substring(0, 8);
                                }

                                //string[] data = new string[5];
                                //data[0] = subDirInfo.FullName;
                                //data[1] = newDir;
                                //data[2] = failReason;
                                //data[3] = exceptionMessage;
                                //data[4] = summaryXml.RefNo;

                                //subDirToMove.Add(data);

                                // Info of where the directory will be moved to
                                SetInfo setInfo = new SetInfo();
                                setInfo.DirectoryPath = subDirInfo.FullName;
                                setInfo.NewDirectoryPath = newDir;
                                setInfo.ErrorReason = failReason;
                                setInfo.ErrorException = exceptionMessage;
                                setInfo.ReferenceNumber = summaryXml.RefNo;

                                subDirToMove.Add(setInfo);
                            }
                        }
                    }
                    else
                    {
                        // Log the error in the Log file
                        string errorMessage = String.Format("Error (MyDocSummaryXml.ProcessMyDoc): DirPath={0}, Message={1}",
                            subDirInfo.FullName, string.Format("Importing of {0} failed. No XML file was found.", subDirInfo.Name.ToUpper()));

                        Util.DWMSLog("MyDocSummaryXml.ProcessMyDoc", errorMessage, EventLogEntryType.Error);

                        //// Move the folder to the failed folder when there is no XML file
                        //string[] data = new string[5];
                        //data[0] = subDirInfo.FullName;
                        //data[1] = Path.Combine(failedDocsDirPath, subDirInfo.Name);
                        //data[2] = "No XML file found.";
                        //data[3] = string.Format("Importing of {0} failed. No XML file was found.", subDirInfo.Name.ToUpper());
                        //data[4] = string.Empty;

                        //subDirToMove.Add(data);

                        // Info of where the directory will be moved to as well as the error information if any
                        SetInfo setInfo = new SetInfo();
                        setInfo.DirectoryPath = subDirInfo.FullName;
                        setInfo.NewDirectoryPath = Path.Combine(failedDocsDirPath, subDirInfo.Name);
                        setInfo.ErrorReason = "No XML file found.";
                        setInfo.ErrorException = string.Format("Importing of {0} failed. No XML file was found.", subDirInfo.Name.ToUpper());

                        subDirToMove.Add(setInfo);
                    }
                }
            }
            #endregion

            #region Move the Sub-Directories to Imported/Failed folder
            // Move the folders that are imported
            if (subDirToMove.Count > 0)
            {
                foreach (SetInfo data in subDirToMove)
                //foreach (string[] data in subDirToMove)
                {
                    //string subDir = data[0];
                    //string newDir = data[1];
                    //string error = data[2];
                    //string exception = data[3];
                    //string refNo = data[4];

                    string subDir = data.DirectoryPath;
                    string newDir = data.NewDirectoryPath;
                    string error = data.ErrorReason;
                    string exception = data.ErrorException;
                    string refNo = data.ReferenceNumber;

                    DirectoryInfo subDirInfo = new DirectoryInfo(subDir);

                    try
                    {
                        // Move the sub-directory
                        subDirInfo.MoveTo(newDir);
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (DWMS_OCR_Service.ProcessMyDocs): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.DWMSLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                    finally
                    {
                        if (Directory.Exists(subDir))
                        {
                            string textToAppend = (newDir.ToLower().Contains("imported") ? "_IMPORTED" : "_FAILED");

                            string renamedDir = subDir + textToAppend;
                            try
                            {
                                //DirectoryInfo subDirInfo = new DirectoryInfo(subDir);
                                subDirInfo.MoveTo(renamedDir);
                            }
                            catch (Exception ex)
                            {
                                string warningMessage = String.Format("Warning (DWMS_OCR_Service.ProcessMyDocs;finally): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                                Util.DWMSLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                            }
                        }

                        if (!String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(exception))
                        {
                            // Log the exception for the directory
                            ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                            exceptionLogDb.LogException(null, "MyDoc", refNo, subDirInfo.Name, error, exception, true);

                        }
                    }
                }
            }
            #endregion
        }
        #endregion

        #region Fax Processes
        /// <summary>
        /// Process the Fax directory.
        /// </summary>
        /// <param name="mainDirInfo">Fax directory</param>
        private void ProcessFax(DirectoryInfo mainDirInfo)
        {
            // Get the Fax directory
            string faxDirPath = Retrieve.GetFaxForOcrDirPath();
            DirectoryInfo faxMainDirInfo = new DirectoryInfo(faxDirPath);

            // Get the Fax/Imported directory
            string importedDocsDirPath = Retrieve.GetImportedFaxOcrDirPath();
            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);
            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);

            // Get the Fax/Failed directory
            string failedDocsDirPath = Retrieve.GetFailedFaxOcrDirPath();
            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            // Get the XML files from the Fax directory
            FileInfo[] fileInfos = faxMainDirInfo.GetFiles("*.xml");

            ArrayList fileToMove = new ArrayList();

            #region Process the XML files
            // Loop through each xml file
            foreach (FileInfo xmlFile in fileInfos)
            {
                if (xmlFile.Name.ToLower().EndsWith("_imported") && xmlFile.Name.ToLower().EndsWith("_failed"))
                {
                    // Check the age of the file.  If the age is greater than or equal to parameter value (in minutes), process the directory.
                    ParameterDb parameterDb = new ParameterDb();
                    TimeSpan difference = DateTime.Now.Subtract(xmlFile.CreationTime);

                    if (difference.TotalMinutes >= parameterDb.GetMinimumAgeForExternalFiles())
                    {
                        // Parse the XML file to retrieve the data
                        FaxSummaryXml summaryXml = new FaxSummaryXml(xmlFile.FullName, faxMainDirInfo.FullName);
                        string newDir = string.Empty;
                        string failReason = string.Empty;
                        string exceptionMessage = string.Empty;

                        if (summaryXml.IsValid)
                        {
                            // Create the set record
                            int setId = -1;

                            bool success = CreateSet(SourceFileEnum.Fax, mainDirInfo, null, summaryXml, new ArrayList(), string.Empty,
                                string.Empty, string.Empty, string.Empty, false, out setId);

                            if (success)
                            {
                                Util.DWMSLog(string.Empty, String.Format("Set Created for {0}(Fax): Id={1}", xmlFile.Name, setId), EventLogEntryType.Information);

                                newDir = importedDirInfo.FullName;
                            }
                            else
                            {
                                newDir = failedDocsDirPath;

                                failReason = "Failed to create set.";
                                exceptionMessage = "Failed to create set.";
                            }
                        }
                        else
                        {
                            newDir = failedDocsDirPath;

                            failReason = summaryXml.GenericError;
                            exceptionMessage = summaryXml.ExceptionMessage;
                        }

                        if (!String.IsNullOrEmpty(newDir))
                        {
                            // Add the xml and pdf file to the list of files to be moved
                            //string[] data = new string[5];

                            //data[0] = xmlFile.FullName;
                            //data[1] = (summaryXml.Documents.Count > 0 ? (string)summaryXml.Documents[0] : string.Empty);
                            //data[2] = newDir;
                            //data[3] = failReason;
                            //data[4] = exceptionMessage;

                            //fileToMove.Add(data);

                            SetInfo setInfo = new SetInfo();
                            setInfo.DirectoryPath = xmlFile.FullName;
                            setInfo.NewDirectoryPath = newDir;
                            setInfo.ErrorReason = failReason;
                            setInfo.ErrorException = exceptionMessage;
                            setInfo.Documents = (summaryXml.Documents.Count > 0 ? (string)summaryXml.Documents[0] : string.Empty);

                            fileToMove.Add(setInfo);
                        }
                    }
                }
            }
            #endregion

            #region Move the files to Imported/Failed folder
            if (fileToMove.Count > 0)
            {
                foreach (SetInfo setInfo in fileToMove)
                //foreach (string[] data in fileToMove)
                {
                    //string xmlPath = data[0];
                    //string filePath = data[1];
                    //string destPath = data[2];
                    //string error = data[3];
                    //string exception = data[4];
                    string xmlPath = setInfo.DirectoryPath;
                    string filePath = setInfo.Documents;
                    string destPath = setInfo.NewDirectoryPath;
                    string error = setInfo.ErrorReason;
                    string exception = setInfo.ErrorException;

                    #region Move XML File to indicate that it has been processed
                    try
                    {
                        FileInfo file = new FileInfo(xmlPath);

                        string newFilePath = Path.Combine(destPath, file.Name);

                        if (File.Exists(newFilePath))
                        {
                            string fileNameWoExtension = newFilePath.Substring(0, newFilePath.LastIndexOf("."));
                            string extension = newFilePath.Substring(newFilePath.LastIndexOf("."));
                            newFilePath = fileNameWoExtension + Guid.NewGuid().ToString().Substring(0, 8) + extension;
                        }

                        file.MoveTo(newFilePath);
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (DWMS_OCR_Service.ProcessFax): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.DWMSLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                    finally
                    {
                        // If moving the file failed, rename the file instead
                        if (File.Exists(xmlPath))
                        {
                            FileInfo file = new FileInfo(xmlPath);

                            string textToAppend = (destPath.ToLower().Contains("imported") ? "_IMPORTED" : "_FAILED");

                            string renamedFile = file.FullName + textToAppend;
                            try
                            {
                                file.MoveTo(renamedFile);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    #endregion

                    #region Move PDF File
                    try
                    {
                        if (!String.IsNullOrEmpty(filePath))
                        {
                            FileInfo file = new FileInfo(filePath);

                            string newFilePath = Path.Combine(destPath, file.Name);

                            if (File.Exists(newFilePath))
                            {
                                string fileNameWoExtension = newFilePath.Substring(0, newFilePath.LastIndexOf("."));
                                string extension = newFilePath.Substring(newFilePath.LastIndexOf("."));
                                newFilePath = fileNameWoExtension + Guid.NewGuid().ToString().Substring(0, 8) + extension;
                            }

                            file.MoveTo(newFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (DWMS_OCR_Service.ProcessFax): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.DWMSLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                    finally
                    {
                        // If moving the file failed, rename the file instead
                        if (File.Exists(filePath))
                        {
                            FileInfo file = new FileInfo(filePath);

                            string textToAppend = (destPath.ToLower().Contains("imported") ? "_IMPORTED" : "_FAILED");

                            string renamedFile = file.FullName + textToAppend;
                            try
                            {
                                file.MoveTo(renamedFile);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    #endregion

                    if (!String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(exception))
                    {
                        FileInfo xmlFile = new FileInfo(xmlPath);

                        // Log the exception for the directory
                        ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                        exceptionLogDb.LogException(null, "Fax", string.Empty, xmlFile.Name, error, exception, true);
                    }
                }
            }
            #endregion
        }
        #endregion

        #region Scan Processes
        /// <summary>
        /// Process the Scan directory.
        /// </summary>
        /// <param name="mainDirInfo">Scan directory</param>
        private void ProcessScan(DirectoryInfo mainDirInfo)
        {
            // Get the Scan directory
            string scanDirPath = Retrieve.GetScanForOcrDirPath();
            DirectoryInfo scanMainDirInfo = new DirectoryInfo(scanDirPath);

            // Get the Scan/Imported directory
            string importedDocsDirPath = Retrieve.GetImportedScanOcrDirPath();
            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);
            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);

            // Get the Scan/Failed directory
            string failedDocsDirPath = Retrieve.GetFailedScanOcrDirPath();
            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            // Get the subfolders of the Scan directory
            DirectoryInfo[] subDirInfos = scanMainDirInfo.GetDirectories();

            ArrayList subDirToMove = new ArrayList();

            #region Process the Sub-Directories
            // Loop through each subdirectory
            foreach (DirectoryInfo subDirInfo in subDirInfos)
            {
                if (!subDirInfo.Name.ToLower().Equals("imported") && !subDirInfo.Name.ToLower().Equals("failed") &&
                    !subDirInfo.Name.ToLower().EndsWith("_imported") && !subDirInfo.Name.ToLower().EndsWith("_failed"))
                {
                    // Check the age of the file. If the age is greater than or equal to the parameter value (int minutes), process the directory.
                    ParameterDb parameterDb = new ParameterDb();
                    TimeSpan difference = DateTime.Now.Subtract(subDirInfo.CreationTime);

                    if (difference.TotalMinutes >= parameterDb.GetMinimumAgeForExternalFiles())
                    {
                        // Get the reference number
                        string referenceNumber = string.Empty;
                        string[] dirNameSplit = subDirInfo.Name.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                        if (dirNameSplit.Length == 5)
                        {
                            // Ex: LOANS-120723-0082-N12N26671-S1234567A
                            referenceNumber = dirNameSplit[3];
                        }

                        // Get the files of the main dir
                        FileInfo[] fileInfos = subDirInfo.GetFiles();

                        ArrayList fileList = new ArrayList();

                        // Loop through each xml file
                        foreach (FileInfo file in fileInfos)
                        {
                            if (file.Extension.ToUpper().Equals(".PDF") ||
                                file.Extension.ToUpper().Equals(".TIF") ||
                                file.Extension.ToUpper().Equals(".TIFF") ||
                                file.Extension.ToUpper().Equals(".BMP") ||
                                file.Extension.ToUpper().Equals(".PNG") ||
                                file.Extension.ToUpper().Equals(".GIF") ||
                                file.Extension.ToUpper().Equals(".JPG") ||
                                file.Extension.ToUpper().Equals(".JPEG"))
                                fileList.Add(file.FullName);
                        }

                        string newDir = string.Empty;
                        string failReason = string.Empty;
                        string exceptionMessage = string.Empty;

                        if (fileList.Count > 0)
                        {
                            // Create the set record
                            int setId = -1;

                            bool success = CreateSet(SourceFileEnum.Scan, mainDirInfo, null, null, fileList, referenceNumber,
                                subDirInfo.Name, string.Empty, string.Empty, false, out setId);

                            if (success)
                            {
                                Util.DWMSLog(string.Empty, String.Format("Set Created for {0}(Scan): Id={1}", subDirInfo.Name, setId), EventLogEntryType.Information);

                                newDir = Path.Combine(importedDirInfo.FullName, subDirInfo.Name);
                            }
                            else
                            {
                                // Move the directory to the 'Failed' folder
                                newDir = Path.Combine(failedDocsDirPath, subDirInfo.Name);
                                failReason = "Failed to create set.";
                                exceptionMessage = "Failed to create set.";
                            }
                        }
                        else
                        {
                            // Move the directory to the 'Failed' folder
                            newDir = Path.Combine(failedDocsDirPath, subDirInfo.Name);
                            failReason = "No valid files found.";
                            exceptionMessage = "No valid files found.";
                        }

                        if (!String.IsNullOrEmpty(newDir))
                        {
                            if (Directory.Exists(newDir))
                            {
                                newDir = newDir + Guid.NewGuid().ToString().Substring(0, 8);
                            }

                            // Get the set info
                            //string[] data = new string[5];
                            //data[0] = subDirInfo.FullName;
                            //data[1] = newDir;
                            //data[2] = failReason;
                            //data[3] = exceptionMessage;
                            //data[4] = referenceNumber;

                            //subDirToMove.Add(data);

                            SetInfo setInfo = new SetInfo();
                            setInfo.DirectoryPath = subDirInfo.FullName;
                            setInfo.NewDirectoryPath = newDir;
                            setInfo.ErrorReason = failReason;
                            setInfo.ErrorException = exceptionMessage;
                            setInfo.ReferenceNumber = referenceNumber;

                            subDirToMove.Add(setInfo);
                        }
                    }
                }
            }
            #endregion

            #region Move the Sub-Directories to Imported/Failed folder
            // Move the folders that are imported
            if (subDirToMove.Count > 0)
            {
                foreach (SetInfo data in subDirToMove)
                //foreach (string[] data in subDirToMove)
                {
                    //string subDir = data[0];
                    //string newDir = data[1];
                    //string error = data[2];
                    //string exception = data[3];
                    //string refNo = data[4];
                    string subDir = data.DirectoryPath;
                    string newDir = data.NewDirectoryPath;
                    string error = data.ErrorReason;
                    string exception = data.ErrorException;
                    string refNo = data.ReferenceNumber;

                    DirectoryInfo subDirInfo = new DirectoryInfo(subDir);

                    try
                    {
                        // Move the sub-directory
                        subDirInfo.MoveTo(newDir);
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (DWMS_OCR_Service.ProcessScan): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.DWMSLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                    finally
                    {
                        if (Directory.Exists(subDir))
                        {
                            string textToAppend = (newDir.ToLower().Contains("imported") ? "_IMPORTED" : "_FAILED");

                            string renamedDir = subDir + textToAppend;
                            try
                            {
                                //DirectoryInfo subDirInfo = new DirectoryInfo(subDir);
                                subDirInfo.MoveTo(renamedDir);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (!String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(exception))
                        {
                            // Log the exception for the directory
                            ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                            exceptionLogDb.LogException(null, "Scan", refNo, refNo, error, exception, true);
                        }
                    }
                }
            }
            #endregion
        }
        #endregion

        #region Email Processes
        /// <summary>
        /// Process the files from the Email directory
        /// </summary>
        /// <param name="mainDirInfo">Email directory</param>
        private void ProcessEmail(DirectoryInfo mainDirInfo)
        {
            // Get the Email directory
            string emailDirPath = Retrieve.GetEmailForOcrDirPath();
            DirectoryInfo emailMainDirInfo = new DirectoryInfo(emailDirPath);

            // Get the Email/Imported directory
            string importedDocsDirPath = Retrieve.GetImportedEmailOcrDirPath();
            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);
            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);

            // Get the Email/Failed directory
            string failedDocsDirPath = Retrieve.GetFailedEmailOcrDirPath();
            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            // Get the subfolders of the main dir
            DirectoryInfo[] subDirInfos = emailMainDirInfo.GetDirectories();

            ArrayList subDirToMove = new ArrayList();

            #region Process the Sub-Directories
            foreach (DirectoryInfo subDirInfo in subDirInfos)
            {
                if (!subDirInfo.Name.ToLower().Equals("imported") && !subDirInfo.Name.ToLower().Equals("failed") &&
                    !subDirInfo.Name.ToLower().EndsWith("_imported") && !subDirInfo.Name.ToLower().EndsWith("_failed"))
                {
                    // Check the age of the file.  If the age is greater than or equal to the parameter value (in minutes), process the directory.
                    ParameterDb parameterDb = new ParameterDb();
                    TimeSpan difference = DateTime.Now.Subtract(subDirInfo.CreationTime);

                    if (difference.TotalMinutes >= parameterDb.GetMinimumAgeForExternalFiles())
                    {
                        // Get the reference number
                        string referenceNumber = string.Empty;
                        string[] dirNameSplit = subDirInfo.Name.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                        if (dirNameSplit.Length > 2)
                        {
                            // Ex: 120723-0082-N12N26671-S1234567A
                            referenceNumber = dirNameSplit[2];
                        }

                        // Get the files of the main dir
                        FileInfo[] fileInfos = subDirInfo.GetFiles();

                        ArrayList fileList = new ArrayList();

                        // Loop through each xml file
                        foreach (FileInfo file in fileInfos)
                        {
                            if (file.Extension.ToUpper().Equals(".PDF") ||
                                file.Extension.ToUpper().Equals(".TIF") ||
                                file.Extension.ToUpper().Equals(".TIFF") ||
                                file.Extension.ToUpper().Equals(".BMP") ||
                                file.Extension.ToUpper().Equals(".PNG") ||
                                file.Extension.ToUpper().Equals(".GIF") ||
                                file.Extension.ToUpper().Equals(".JPG") ||
                                file.Extension.ToUpper().Equals(".JPEG"))
                                fileList.Add(file.FullName);
                        }

                        string newDir = string.Empty;
                        string failReason = string.Empty;
                        string exceptionMessage = string.Empty;

                        if (fileList.Count > 0)
                        {
                            // Create the set record
                            int setId = -1;

                            bool success = CreateSet(SourceFileEnum.Email, mainDirInfo, null, null, fileList, referenceNumber,
                                subDirInfo.Name, string.Empty, string.Empty, false, out setId);

                            if (success)
                            {
                                Util.DWMSLog(string.Empty, String.Format("Set Created for {0}(Email): Id={1}", subDirInfo.Name, setId), EventLogEntryType.Information);

                                newDir = Path.Combine(importedDirInfo.FullName, subDirInfo.Name);
                            }
                            else
                            {
                                // Move the directory to the 'Failed' folder
                                newDir = Path.Combine(failedDocsDirPath, subDirInfo.Name);
                                failReason = "Failed to create set.";
                                exceptionMessage = "Failed to create set.";
                            }
                        }
                        else
                        {
                            // Move the directory to the 'Failed' folder
                            newDir = Path.Combine(failedDocsDirPath, subDirInfo.Name);
                            failReason = "No valid files found.";
                            exceptionMessage = "No valid files found.";
                        }

                        if (!String.IsNullOrEmpty(newDir))
                        {
                            if (Directory.Exists(newDir))
                            {
                                newDir = newDir + Guid.NewGuid().ToString().Substring(0, 8);
                            }

                            // Get the set info
                            //string[] data = new string[5];
                            //data[0] = subDirInfo.FullName;
                            //data[1] = newDir;
                            //data[2] = failReason;
                            //data[3] = exceptionMessage;
                            //data[4] = referenceNumber;

                            //subDirToMove.Add(data);

                            SetInfo setInfo = new SetInfo();
                            setInfo.DirectoryPath = subDirInfo.FullName;
                            setInfo.NewDirectoryPath = newDir;
                            setInfo.ErrorReason = failReason;
                            setInfo.ErrorException = exceptionMessage;
                            setInfo.ReferenceNumber = referenceNumber;

                            subDirToMove.Add(setInfo);
                        }
                    }
                }
            }
            #endregion

            #region Move the Sub-Directories to Imported/Failed folder
            if (subDirToMove.Count > 0)
            {
                foreach (SetInfo data in subDirToMove)
                //foreach (string[] data in subDirToMove)
                {
                    //string subDir = data[0];
                    //string newDir = data[1];
                    //string error = data[2];
                    //string exception = data[3];
                    //string refNo = data[4];
                    string subDir = data.DirectoryPath;
                    string newDir = data.NewDirectoryPath;
                    string error = data.ErrorReason;
                    string exception = data.ErrorException;
                    string refNo = data.ReferenceNumber;

                    DirectoryInfo subDirInfo = new DirectoryInfo(subDir);

                    try
                    {
                        // Move the sub-directory
                        subDirInfo.MoveTo(newDir);
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (DWMS_OCR_Service.ProcessScan): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.DWMSLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                    finally
                    {
                        if (Directory.Exists(subDir))
                        {
                            string textToAppend = (newDir.ToLower().Contains("imported") ? "_IMPORTED" : "_FAILED");

                            string renamedDir = subDir + textToAppend;
                            try
                            {
                                //DirectoryInfo subDirInfo = new DirectoryInfo(subDir);
                                subDirInfo.MoveTo(renamedDir);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (!String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(exception))
                        {
                            // Log the exception for the directory
                            ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                            exceptionLogDb.LogException(null, "Email", refNo, subDirInfo.Name, error, exception, true);
                        }
                    }
                }
            }
            #endregion
        }
        #endregion

        #region Web Service Processes
        /// <summary>
        /// Process the sub-directories in the WebService directory
        /// </summary>
        /// <param name="mainDirInfo">WebService sub-directory</param>
        private void ProcessWebService(DirectoryInfo mainDirInfo)
        {
            // Get the WebService directory
            string webServiceDirPath = Retrieve.GetWebServiceForOcrDirPath();
            DirectoryInfo webServiceMainDirInfo = new DirectoryInfo(webServiceDirPath);

            // Get the WebService/Imported directory
            string importedDocsDirPath = Retrieve.GetImportedWebServiceOcrDirPath();
            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);
            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);

            // Get the WebService/Failed directory
            string failedDocsDirPath = Retrieve.GetFailedWebServiceOcrDirPath();
            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            // Get the subfolders of the main dir
            DirectoryInfo[] subDirInfos = webServiceMainDirInfo.GetDirectories();

            ArrayList subDirToMove = new ArrayList();

            #region Process the Sub-Directories
            foreach (DirectoryInfo subDirInfo in subDirInfos)
            {
                if (!subDirInfo.Name.ToLower().Equals("imported") && !subDirInfo.Name.ToLower().Equals("failed") &&
                    !subDirInfo.Name.ToLower().EndsWith("_imported") && !subDirInfo.Name.ToLower().EndsWith("_failed"))
                {
                    // Check the age of the file.  If the age is greater than or equal to the parameter value (in minutes), process the directory.
                    ParameterDb parameterDb = new ParameterDb();
                    TimeSpan difference = DateTime.Now.Subtract(subDirInfo.CreationTime);

                    if (difference.TotalMinutes >= parameterDb.GetMinimumAgeForExternalFiles())
                    {
                        // Get the reference number
                        string referenceNumber = string.Empty;
                        string acknowledgementNo = subDirInfo.Name;
                        string channel = string.Empty;
                        string xmlContents = string.Empty;
                        bool hasDocId = false;

                        // Get the reference number and channel
                        RetrieveRefNoAndChannelForWebService(subDirInfo, out referenceNumber, out channel, out xmlContents, out hasDocId);

                        // Get the files of the main dir
                        FileInfo[] fileInfos = subDirInfo.GetFiles();

                        ArrayList fileList = new ArrayList();

                        // Loop through each file
                        foreach (FileInfo file in fileInfos)
                        {
                            if (file.Extension.ToUpper().Equals(".PDF") ||
                                file.Extension.ToUpper().Equals(".TIF") ||
                                file.Extension.ToUpper().Equals(".TIFF") ||
                                file.Extension.ToUpper().Equals(".BMP") ||
                                file.Extension.ToUpper().Equals(".PNG") ||
                                file.Extension.ToUpper().Equals(".GIF") ||
                                file.Extension.ToUpper().Equals(".JPG") ||
                                file.Extension.ToUpper().Equals(".JPEG"))
                                fileList.Add(file.FullName);
                        }

                        string newDir = string.Empty;
                        string failReason = string.Empty;
                        string exceptionMessage = string.Empty;

                        if (fileList.Count > 0)
                        {
                            // Create the set record
                            int setId = -1;

                            bool success = CreateSet(SourceFileEnum.WebService, mainDirInfo, null, null, fileList, referenceNumber,
                                acknowledgementNo, channel, xmlContents, hasDocId, out setId);

                            if (success)
                            {
                                Util.DWMSLog(string.Empty, String.Format("Set Created for {0}(WebService): Id={1}", subDirInfo.Name, setId), EventLogEntryType.Information);

                                newDir = Path.Combine(importedDirInfo.FullName, subDirInfo.Name);
                            }
                            else
                            {
                                // Move the directory to the 'Failed' folder
                                newDir = Path.Combine(failedDocsDirPath, subDirInfo.Name);
                                failReason = "Failed to create set.";
                                exceptionMessage = "Failed to create set.";
                            }
                        }
                        else
                        {
                            // Move the directory to the 'Failed' folder
                            newDir = Path.Combine(failedDocsDirPath, subDirInfo.Name);
                            failReason = "No valid files found.";
                            exceptionMessage = "No valid files found.";
                        }

                        if (!String.IsNullOrEmpty(newDir))
                        {
                            if (Directory.Exists(newDir))
                            {
                                newDir = newDir + Guid.NewGuid().ToString().Substring(0, 8);
                            }

                            // Get the set info
                            //string[] data = new string[5];
                            //data[0] = subDirInfo.FullName;
                            //data[1] = newDir;
                            //data[2] = failReason;
                            //data[3] = exceptionMessage;
                            //data[4] = referenceNumber;

                            //subDirToMove.Add(data);

                            SetInfo setInfo = new SetInfo();
                            setInfo.DirectoryPath = subDirInfo.FullName;
                            setInfo.NewDirectoryPath = newDir;
                            setInfo.ErrorReason = failReason;
                            setInfo.ErrorException = exceptionMessage;
                            setInfo.ReferenceNumber = referenceNumber;

                            subDirToMove.Add(setInfo);
                        }
                    }
                }
            }
            #endregion

            #region Move the Sub-Directories to Imported/Failed folder
            if (subDirToMove.Count > 0)
            {
                foreach (SetInfo data in subDirToMove)
                //foreach (string[] data in subDirToMove)
                {
                    //string subDir = data[0];
                    //string newDir = data[1];
                    //string error = data[2];
                    //string exception = data[3];
                    //string refNo = data[4];

                    string subDir = data.DirectoryPath;
                    string newDir = data.NewDirectoryPath;
                    string error = data.ErrorReason;
                    string exception = data.ErrorException;
                    string refNo = data.ReferenceNumber;

                    DirectoryInfo subDirInfo = new DirectoryInfo(subDir);

                    try
                    {
                        // Move the sub-directory
                        subDirInfo.MoveTo(newDir);
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (DWMS_OCR_Service.ProcessWebService): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.DWMSLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                    finally
                    {
                        if (Directory.Exists(subDir))
                        {
                            string textToAppend = (newDir.ToLower().Contains("imported") ? "_IMPORTED" : "_FAILED");

                            string renamedDir = subDir + textToAppend;
                            try
                            {
                                subDirInfo.MoveTo(renamedDir);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (!String.IsNullOrEmpty(error) && !String.IsNullOrEmpty(exception))
                        {
                            // Log the exception for the directory
                            ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                            exceptionLogDb.LogException(null, "WebService", refNo, subDirInfo.Name, error, exception, true);
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Get the reference number and channel from the WebService directory
        /// </summary>
        /// <param name="subDirInfo">Sub-directory</param>
        /// <param name="refNo">Reference number</param>
        /// <param name="channel">Channel of the set</param>
        private void RetrieveRefNoAndChannelForWebService(DirectoryInfo subDirInfo, out string refNo, out string channel, out string xmlContents, out bool hasDocId)
        {
            refNo = string.Empty;
            channel = string.Empty;
            xmlContents = string.Empty;
            hasDocId = false;

            // Get the XML file of the set
            FileInfo[] xmlFiles = subDirInfo.GetFiles("set.xml");

            if (xmlFiles.Length > 0)
            {
                FileInfo xmlFile = xmlFiles[0];

                try
                {
                    // Load the XML file
                    XmlDocument summaryXmlDoc = new XmlDocument();
                    summaryXmlDoc.Load(xmlFile.FullName);

                    xmlContents = summaryXmlDoc.OuterXml;

                    // Get the reference number
                    XmlNodeList headerNode = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.WebServiceSetXmlRefNoTagName);

                    if (headerNode.Count > 0)
                    {
                        XmlNode header = headerNode[0];
                        refNo = header.InnerText.Trim();
                    }

                    // Get the channel
                    headerNode = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.WebServiceSetXmlChannelTagName);

                    if (headerNode.Count > 0)
                    {
                        XmlNode header = headerNode[0];
                        channel = header.InnerText.Trim();
                    }

                    // Get the channel
                    headerNode = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.WebServiceSetXmlHasDocIdTagName);

                    if (headerNode.Count > 0)
                    {
                        XmlNode header = headerNode[0];
                        hasDocId = (!String.IsNullOrEmpty(header.InnerText.Trim()) ? bool.Parse(header.InnerText.Trim()) : false);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        #endregion

        #region OCR Processes
        /// <summary>
        /// Start the OCR process. 
        /// </summary>
        /// <param name="mainDirInfo">Set directory for process</param>
        /// <returns></returns>
        private List<int> StartOcr(DirectoryInfo mainDirInfo)
        {
            ArrayList pages = new ArrayList();
            List<int> setIds = new List<int>();

            // Save the raw pages of each document
            ProcessDocs(mainDirInfo, ref pages, ref setIds);

            // Do the OCR for each page
            OcrRawPages(pages, ref setIds);

            return setIds;
        }

        /// <summary>
        /// Process the set.
        /// </summary>
        /// <param name="mainDirInfo">ForOcr directory</param>
        /// <param name="pages">Page list</param>
        /// <param name="setIds">Set id list</param>
        private void ProcessDocs(DirectoryInfo mainDirInfo, ref ArrayList pages, ref List<int> setIds)
        {
            DocSetDb docSetDb = new DocSetDb();

            // Get the id of the first set to be processed
            int setId = docSetDb.GetTopOneDocSetIdForOcrProcess();

            if (setId > 0)
            {
                Util.DWMSLog(string.Empty, "Set for OCR Process: " + setId.ToString(), EventLogEntryType.Information);
                string setDirPath = Path.Combine(mainDirInfo.FullName, setId.ToString());
                DirectoryInfo setDir = new DirectoryInfo(setDirPath);

                // Set the 'IsBeingProcessed' flag to true
                docSetDb.SetIsBeingProcessed(int.Parse(setDir.Name), true);

                // Split and save the pages of the documents
                SaveRawPagesForSet(setDir, ref pages);

                // Add the set id to the list
                if (!setIds.Contains(int.Parse(setDir.Name)))
                    setIds.Add(int.Parse(setDir.Name));
            }
        }

        /// <summary>
        /// Split the documents into individual pages and save into the database.
        /// </summary>
        /// <param name="setDir">Set directory</param>
        /// <param name="pages">Page list</param>
        private void SaveRawPagesForSet(DirectoryInfo setDir, ref ArrayList pages)
        {
            RawPageDb rawPageDb = new RawPageDb();
            RawFileDb rawFileDb = new RawFileDb();
            LogActionDb logActionDb = new LogActionDb();

            // Get the RawPage directory
            string rawPagesMainDirPath = Retrieve.GetRawPageOcrDirPath();
            DirectoryInfo rawPageMainDirInfo = new DirectoryInfo(rawPagesMainDirPath);

            // If there are documents currently for the set, clear them for re-categorization
            #region Delete current RawPages of the set
            int setId = int.Parse(setDir.Name);

            using (RawFile.RawFileDataTable rawFilesDt = rawFileDb.GetRawFilesByDocSetId(setId))
            {
                if (rawFilesDt.Rows.Count > 0)
                {
                    foreach (RawFile.RawFileRow rawFile in rawFilesDt.Rows)
                    {
                        try
                        {
                            using (RawPage.RawPageDataTable rawPagesDt = rawPageDb.GetRawPageByRawFileId(rawFile.Id))
                            {
                                foreach (RawPage.RawPageRow rawPage in rawPagesDt)
                                {
                                    // Update the rawpage and set the document id to null
                                    rawPageDb.UpdateSetDocIdNull(rawPage.Id);

                                    string rawPagePath = Path.Combine(rawPageMainDirInfo.FullName, rawPage.Id.ToString());

                                    // Delete the raw page main folder dir
                                    if (Directory.Exists(rawPagePath))
                                    {
                                        DirectoryInfo rawPageDirInfo = new DirectoryInfo(rawPagePath);
                                        rawPageDirInfo.Delete(true);
                                    }
                                }
                            }

                            string rawFileDir = Path.Combine(setDir.FullName, rawFile.Id.ToString());

                            // Delete the sub-directories (raw page dirs) of the raw file dir
                            if (Directory.Exists(rawFileDir))
                            {
                                DirectoryInfo rawFileDirInfo = new DirectoryInfo(rawFileDir);

                                DirectoryInfo[] rawFileSubDirs = rawFileDirInfo.GetDirectories();

                                foreach (DirectoryInfo rawFileSubDir in rawFileSubDirs)
                                {
                                    rawFileSubDir.Delete(true);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            try
                            {
                                // Delete the raw pages for the raw file
                                rawPageDb.DeleteRawPagesByRawFileId(rawFile.Id);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            try
            {
                // Delete the personal records by set
                DocPersonalDb docPersonalDb = new DocPersonalDb();
                docPersonalDb.DeleteByDocSetId(setId);
            }
            catch (Exception)
            {
            }

            try
            {
                // Delete the documents of the set (if any)
                DocDb docDb = new DocDb();
                docDb.DeleteByDocSetId(setId);
            }
            catch (Exception)
            {
            }
            #endregion

            // Create the RawPages and images for OCR
            #region Create RawPages
            // Get the raw file directories for the set
            DirectoryInfo[] rawFilesDirInfo = setDir.GetDirectories();

            // Loop through each raw file dir
            foreach (DirectoryInfo rawFilesDir in rawFilesDirInfo)
            {
                try
                {
                    // Get the raw file
                    FileInfo[] rawfiles = rawFilesDir.GetFiles();

                    if (rawfiles.Count() > 0)
                    {
                        FileInfo file = rawfiles[0];

                        try
                        {
                            // Split the PDF files into pages
                            if (file.Extension.ToUpper().Equals(".PDF"))
                            {
                                // PDF File process
                                PreparePdfForProcessing(setId, file, rawPageMainDirInfo, rawFilesDir, ref pages);
                            }
                            else if ((file.Extension.ToUpper().Equals(".TIF") || file.Extension.ToUpper().Equals(".TIFF")) &&
                                Util.CountTiffPages(file.FullName) > 1)
                            {
                                // Multi-page TIFF File process
                                PrepareMultiPageTiffForProcessing(setId, file, rawPageMainDirInfo, rawFilesDir, ref pages);
                            }
                            else
                            {
                                // Image File process
                                PrepareImagesForProcessing(setId, file, rawPageMainDirInfo, rawFilesDir, ref pages);
                            }
                        }
                        catch (Exception ex)
                        {
                            string errorSummary = string.Format("File={0}, Message={1}", file.Name,
                                ex.Message + (ex.Message.ToLower().Contains("the document has no pages") ? " File could be a secured document." : string.Empty));

                            throw new Exception(errorSummary, ex.InnerException);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log in the windows service log
                    string errorSummary = string.Format("Error (DWMS_OCR_Service.SaveRawPagesForSet): {0}, StackTrace={1}", ex.Message, ex.StackTrace);
                    Util.DWMSLog("DWMS_OCR_Service.SaveRawPagesForSet", errorSummary, EventLogEntryType.Error);

                    // Log the error to show in the set action log
                    logActionDb.Insert(importedBy.Value,
                        LogActionEnum.REPLACE1_COLON_REPLACE2.ToString(),
                        LogActionEnum.File_Error.ToString(),
                        ex.Message,
                        string.Empty, string.Empty, LogTypeEnum.S, setId);

                    // Log the exception for the directory
                    ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                    exceptionLogDb.LogException(setId, string.Empty, string.Empty, string.Empty, "There is an error when processing the file.", ex.Message, true);
                }
            }
            #endregion
        }

        /// <summary>
        /// Assign threads for each page
        /// </summary>
        /// <param name="pages"></param>
        private void OcrRawPages(ArrayList pages, ref List<int> setIds)
        {
            int threadCount = pages.Count;

            if (threadCount > 0)
            {
                DocSetDb docSetDb = new DocSetDb();
                ParameterDb parameterDb = new ParameterDb();
                int maxOcrPages = parameterDb.GetMaximumPagesForOcr();

                if (threadCount >= maxOcrPages)
                {
                    // If the number of pages is greater than the maximum, no OCR will be done.  Instead, the document will be classified as unidentified
                    #region Page Exceed Maximum Allowable
                    if (setIds.Count > 0)
                    {
                        Util.DWMSLog(string.Empty, String.Format("Page count greater than the maximum allowable ({0}) for OCR.", maxOcrPages), EventLogEntryType.Information);

                        // Save the document into the Doc table
                        DocDb docDb = new DocDb();
                        int docId = docDb.Insert(setIds[0], DocTypeEnum.Unidentified.ToString(), setIds[0], DocStatusEnum.New.ToString());

                        if (docId > 0)
                        {
                            RawPageDb rawPageDb = new RawPageDb();
                            for (int index = 0; index < threadCount; index++)
                            {
                                string[] pageData = (string[])pages[index];
                                int rawPageId = int.Parse(pageData[0]);
                                string filePath = pageData[1].ToString();

                                Util.CreateSearcheablePdfFile(filePath);

                                rawPageDb.Update(rawPageId, docId, index + 1);

                                //// Delete the image (TIFF) file
                                //try
                                //{
                                //    FileInfo imageFile = new FileInfo(filePath);
                                //    imageFile.Delete();
                                //}
                                //catch (Exception)
                                //{
                                //}
                            }

                            // Update the status of the set
                            // If there is a verification officer assigned to the set, the status will be Pending Verification.
                            // Else, it will New
                            docSetDb.UpdateSetStatus(setIds[0], (docSetDb.HasVerificationOfficerAssigned(setIds[0]) ? SetStatusEnum.Pending_Verification : SetStatusEnum.New));

                            // Update the isProcessed flag
                            //docSetDb.SetIsBeingProcessed(setIds[0], false);

                            // Insert the doc personal record for the doc
                            DocPersonalDb docPersonalDb = new DocPersonalDb();
                            int docPersonalId = docPersonalDb.Insert(setIds[0], string.Empty, string.Empty,
                                DocFolderEnum.Unidentified.ToString(), string.Empty);

                            // Insert the association of the doc and doc personal
                            SetDocRefDb setDocRefDb = new SetDocRefDb();
                            setDocRefDb.Insert(docId, docPersonalId);
                        }

                        // Remove the setId from the ArrayList to prevent it from being categorized
                        setIds = new List<int>();
                    }
                    #endregion
                }
                else if (docSetDb.ToSkipCategorization(setIds[0]) && !docSetDb.ToSkipCategorizationFromWebService(setIds[0]))
                {
                    // Create the pdf documents for the individual pages
                    #region Skip Categorization
                    for (int index = 0; index < threadCount; index++)
                    {
                        string[] pageData = (string[])pages[index];
                        string filePath = pageData[1].ToString();

                        Util.CreateSearcheablePdfFile(filePath);
                    }
                    #endregion
                }
                else
                {
                    #region OCR Thread
                    int binarize = parameterDb.GetOcrBinarize();
                    int bgFactor = parameterDb.GetOcrBackgroundFactor();
                    int fgFactor = parameterDb.GetOcrForegroundFactor();
                    int quality = parameterDb.GetOcrQuality();
                    string morph = parameterDb.GetOcrMorph();
                    bool dotMatrix = parameterDb.GetOcrDotMatrix();
                    int despeckle = parameterDb.GetOcrDespeckle();

                    Util.DWMSLog(string.Empty, "OCR of pages started.", EventLogEntryType.Information);

                    Thread[] threads = new Thread[threadCount];

                    // Assign threads for each page
                    for (int index = 0; index < threadCount; index++)
                    {
                        string[] pageData = (string[])pages[index];
                        int rawPageId = int.Parse(pageData[0]);
                        string filePath = pageData[1].ToString();

                        ThreadInfo threadInfo = new ThreadInfo();
                        threadInfo.RawPageId = rawPageId;
                        threadInfo.FilePath = filePath;
                        threadInfo.Binarize = binarize;
                        threadInfo.BackgroundFactor = bgFactor;
                        threadInfo.ForegroundFactor = fgFactor;
                        threadInfo.Quality = quality;
                        threadInfo.Morph = morph;
                        threadInfo.DotMatrix = dotMatrix;
                        threadInfo.Despeckle = despeckle;
                        threadInfo.SetId = setIds[0];

                        Thread thread = new Thread(new ParameterizedThreadStart(OcrThreadCallback));
                        threads[index] = thread;

                        thread.Start(threadInfo);
                    }

                    // Wait for all threads to finish
                    foreach (Thread thread in threads)
                    {
                        thread.Join();
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// OCR Thread callback
        /// </summary>
        /// <param name="parameter"></param>
        private void OcrThreadCallback(object parameter)
        {
            ThreadInfo threadInfo = parameter as ThreadInfo;
            int rawPageId = threadInfo.RawPageId;
            string filePath = threadInfo.FilePath;
            string originalFilePath = filePath;
            int setId = threadInfo.SetId;

            int binarize = threadInfo.Binarize;
            int bgFactor = threadInfo.BackgroundFactor;
            int fgFactor = threadInfo.ForegroundFactor;
            int quality = threadInfo.Quality;
            string morph = threadInfo.Morph;
            bool dotMatrix = threadInfo.DotMatrix;
            int despeckle = threadInfo.Despeckle;

            bool deleteTempFile = false;

            try
            {
                #region OCR Process
                // Wait until the thread is assigned a resource
                semaphore.WaitOne();

                string ocrText = string.Empty;
                bool ocrSuccess = false;
                string errorReason = string.Empty;
                string errorException = string.Empty;

                // If the PDF file is searcheable, retrieve the text of the PDF.  If the result is an empty string, do an
                // OCR of the page to retrieve the text.
                string tempContents = string.Empty;

                if (new FileInfo(filePath).Extension.ToUpper().Equals(".PDF"))
                {
                    int errorCode = Util.ExtractTextFromSearcheablePdf(filePath, setId, false, out tempContents);

                    if (errorCode < 0)
                    {
                        // Feed the PDF file directly into the OCR engine
                        tempContents = string.Empty;
                    }
                }

                // Assign 3 chances for the OCR to complete the process
                for (int cnt = 0; cnt < 3; cnt++)
                {
                    if (String.IsNullOrEmpty(tempContents))
                    {
                        OcrManager ocrManager = new OcrManager(filePath, setId, binarize, bgFactor, fgFactor, quality, morph, dotMatrix, despeckle);
                        ocrSuccess = ocrManager.GetOcrText(out ocrText, out errorReason, out errorException);
                        ocrManager.Dispose();
                    }
                    else
                    {
                        // Create the searcheable PDF copy
                        FileInfo file = new FileInfo(filePath);
                        string searcheablePdf = Path.Combine(file.DirectoryName, file.Name + "_s.pdf");
                        try
                        {
                            file.CopyTo(searcheablePdf);
                        }
                        catch (Exception)
                        {
                        }

                        ocrText = tempContents;
                        ocrSuccess = true;
                    }

                    if (ocrSuccess)
                    {
                        RawPageDb rawPageDb2 = new RawPageDb();
                        rawPageDb2.Update(rawPageId, ocrText, true);

                        break;
                    }
                }

                // Check if the OCR was successful
                // If not successful, create a PDF from the image
                if (!ocrSuccess && String.IsNullOrEmpty(ocrText))
                {
                    Util.CreateSearcheablePdfFile(originalFilePath);

                    // Set the 'OcrFailed' flag to true
                    RawPageDb rawPageDb2 = new RawPageDb();
                    rawPageDb2.UpdateOcrFailed(rawPageId, true);

                    if (!String.IsNullOrEmpty(errorReason) && !String.IsNullOrEmpty(errorException))
                    {
                        // Log the exception
                        ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                        exceptionLogDb.LogException(setId, string.Empty, string.Empty, string.Empty, errorReason, errorException, false);
                    }
                }
                #endregion
            }
            catch (Exception e)
            {
                // Create a searchable PDF
                Util.CreateSearcheablePdfFile(originalFilePath);

                RawPageDb rawPageDb3 = new RawPageDb();
                rawPageDb3.Update(rawPageId, true);

                string errorSummary = string.Format("Error (DWMS_OCR_Services.OcrThreadCallback): File={0}, Message={1}, StackTrace={2}"
                    , filePath, e.Message, e.StackTrace);

                Util.DWMSLog("DWMS_OCR_Services.OcrThreadCallback", errorSummary, EventLogEntryType.Error);

                // Log the exception for the directory
                ExceptionLogDb exceptionLogDb = new ExceptionLogDb();

                string reason = "OCR of file failed.";
                string errorMessage = String.Format("File={0}, Message={1}",
                    (originalFilePath.Contains("\\") ? originalFilePath.Substring(originalFilePath.LastIndexOf("\\") + 1) : originalFilePath),
                    e.Message);

                exceptionLogDb.LogException(setId, string.Empty, string.Empty, string.Empty, reason, errorMessage, false);
            }
            finally
            {
                // Delete the temporary image (PNG) file
                try
                {
                    if (deleteTempFile)
                    {
                        FileInfo imageFile = new FileInfo(filePath);
                        imageFile.Delete();
                    }
                }
                catch (Exception)
                {
                }

                // Release the resource for this thread
                semaphore.Release();
            }
        }
        #endregion

        #region Categorization Processes
        /// <summary>
        /// Start the categorization
        /// </summary>
        private void StartCategorization(List<int> setIds)
        {
            ArrayList setIdList = new ArrayList();

            // Sort the sets by raw page count
            //SortSetIdsByRawPageCount(ref setIds);

            // Categorize the sets
            CategorizeSet(setIds);
        }

        /// <summary>
        /// Get the set ids for categorization
        /// </summary>
        /// <param name="setIdList"></param>
        private void GetSetIds(DirectoryInfo mainDirInfo, ref ArrayList setIdList)
        {
            // Get subfolders of the main dir
            DirectoryInfo[] usersDirInfo = mainDirInfo.GetDirectories();

            // Loop through each sub directory to process files
            foreach (DirectoryInfo usersDir in usersDirInfo)
            {
                // Get the set directories for each user
                DirectoryInfo[] setDirInfo = usersDir.GetDirectories();

                // Loop through each set dir
                foreach (DirectoryInfo setDir in setDirInfo)
                {
                    DocSetDb docSetDb = new DocSetDb();
                    int setId = int.Parse(setDir.Name);
                    string status = docSetDb.GetDocSetStatus(setId);
                    bool readyForOcr = docSetDb.GetReadyForOcrFlag(setId);

                    // Process only sets that are not yet categorized
                    if (!String.IsNullOrEmpty(status) &&
                        status.Equals(SetStatusEnum.Pending_Categorization.ToString()) &&
                        readyForOcr)
                    {
                        // Add the set id to the list
                        setIdList.Add(setDir.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Sort the Set Ids by the number of raw pages
        /// </summary>
        /// <param name="setIds"></param>
        private void SortSetIdsByRawPageCount(ref List<int> setIds)
        {
            RawPageDb rawPageDb = new RawPageDb();

            // Get the pagecount of each set
            List<int[]> setIdsWithPageCount = new List<int[]>();
            foreach (int setId in setIds)
            {
                int pageCount = rawPageDb.CountPagesByDocSetId(setId);

                int[] setData = new int[] { setId, pageCount };

                setIdsWithPageCount.Add(setData);
            }

            // Sort the set according to the pagecount
            List<int[]> sortedSetIds = setIdsWithPageCount.OrderBy(item => item[1]).ToList();

            // Return the new sorted set ids
            List<int> newSetIds = new List<int>();
            foreach (int[] setData in sortedSetIds)
            {
                newSetIds.Add(setData[0]);
            }

            setIds.Clear();
            setIds.AddRange(newSetIds);
        }

        /// <summary>
        /// Categorize the set
        /// </summary>
        /// <param name="setIdList"></param>
        private void CategorizeSet(List<int> setIdList)
        {
            int threadCount = setIdList.Count;

            if (threadCount > 0)
            {
                Util.DWMSLog(string.Empty, "Categorization for set started.", EventLogEntryType.Information);

                // Check if the set is to be skipped
                DocSetDb docSetDb = new DocSetDb();
                if (docSetDb.ToSkipCategorization(setIdList[0]) && !docSetDb.ToSkipCategorizationFromWebService(setIdList[0]))
                {
                    // Categorize the skipped set
                    CategorizeSkippedSet(setIdList[0]);
                }
                else if (docSetDb.ToSkipCategorizationFromWebService(setIdList[0]))
                {
                    // Categorize the set from Web Service
                    CategorizeSetFromWebService(setIdList[0]);
                }
                else
                {
                    // Categorize the set
                    int setId = setIdList[0];
                    CategorizationThread categorizationThread = new CategorizationThread(setId, categorizationSamplePages);
                    categorizationThread.Categorize();
                }

                Util.DWMSLog(string.Empty, "Categorization for set ended.", EventLogEntryType.Information);
            }
        }

        /// <summary>
        /// Categorize a skipped set
        /// </summary>
        /// <param name="setId"></param>
        private void CategorizeSkippedSet(int setId)
        {
            // Get the storage of the Raw Pages
            string rawPagesMainDirPath = Retrieve.GetRawPageOcrDirPath();
            DirectoryInfo rawPageMainDirInfo = new DirectoryInfo(rawPagesMainDirPath);

            RawFileDb rawFileDb = new RawFileDb();
            RawPageDb rawPageDb = new RawPageDb();
            DocDb docDb = new DocDb();
            DocSetDb docSetDb = new DocSetDb();
            DocPersonalDb docPersonalDb = new DocPersonalDb();
            SetDocRefDb setDocRefDb = new SetDocRefDb();

            // Get the raw files of the set
            RawFile.RawFileDataTable rawFileTable = rawFileDb.GetRawFilesByDocSetId(setId);

            foreach (RawFile.RawFileRow rawFileRow in rawFileTable)
            {
                // Create a document record of the raw file
                // Save the document into the Doc table                
                int docId = docDb.Insert(setId, DocTypeEnum.Unidentified.ToString(), setId, DocStatusEnum.New.ToString());

                if (docId > 0)
                {
                    // Update the raw pages of the raw file
                    RawPage.RawPageDataTable rawPageTable = rawPageDb.GetRawPageByRawFileId(rawFileRow.Id);

                    foreach (RawPage.RawPageRow rawPageRow in rawPageTable)
                    {
                        rawPageDb.Update(rawPageRow.Id, docId, rawPageRow.RawPageNo);
                    }

                    // Update the status of the set
                    // If there is a verification officer assigned to the set, the status will be Pending Verification.
                    // Else, it will be New                    
                    docSetDb.UpdateSetStatus(setId, (docSetDb.HasVerificationOfficerAssigned(setId) ? SetStatusEnum.Pending_Verification : SetStatusEnum.New));

                    // Insert the doc personal record for the doc                    
                    int docPersonalId = docPersonalDb.Insert(setId, string.Empty, string.Empty,
                        DocFolderEnum.Unidentified.ToString(), string.Empty);

                    // Insert the association of the doc and doc personal                    
                    setDocRefDb.Insert(docId, docPersonalId);
                }
            }
        }

        /// <summary>
        /// Categorize the set from Web Service
        /// </summary>
        /// <param name="setId"></param>
        private void CategorizeSetFromWebService(int setId)
        {
            RawFileDb rawFileDb = new RawFileDb();
            RawPageDb rawPageDb = new RawPageDb();
            DocDb docDb = new DocDb();
            DocSetDb docSetDb = new DocSetDb();
            DocPersonalDb docPersonalDb = new DocPersonalDb();
            AppPersonalDb appPersonalDb = new AppPersonalDb();
            SetDocRefDb setDocRefDb = new SetDocRefDb();
            AppDocRefDb appDocRefDb = new AppDocRefDb();
            DocTypeDb docTypeDb = new DocTypeDb();
            MetaDataDb metadataDb = new MetaDataDb();

            CustomerWebServiceInfo[] customers = ParseWebServiceXml(setId);

            if (customers.Length > 0)
            {

                bool isAllVerified = true;

                foreach (CustomerWebServiceInfo customer in customers)
                {
                    string nric = customer.Nric;
                    string refNo = customer.RefNo;

                    foreach (DocWebServiceInfo doc in customer.Documents)
                    {
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

                        //2012-12-12
                        // Create the document record
                        // Check if the Document is IsVerified, set status to Verified
                        // begin
                        DocStatusEnum docStatus = DocStatusEnum.New;
                        //MetadataWebServiceInfo tempMetadata = null;
                        //tempMetadata = doc.Files[0].Metadata[0];

                        //2012-12-17, if any customer's documents are not verified, the status of the docset is not verified
                        foreach (FileWebServicInfo file in doc.Files)
                        {

                            foreach (MetadataWebServiceInfo metadata in file.Metadata)
                            {
                                if (!metadata.IsVerified)
                                {
                                    isAllVerified = false;
                                }
                            }
                        }




                        //if (tempMetadata != null)
                        //{
                        if (isAllVerified)
                        {
                            docStatus = DocStatusEnum.Verified;


                            //Update DocSet Verification OIC to as System
                            // Push the verified set to Completeness module
                            ProfileDb profileDb = new ProfileDb();
                            Guid? verificationOIC = profileDb.GetSystemGuid();
                            if (verificationOIC.HasValue)
                            {

                                //update set status, verification, and log in the AuditTrail
                                // Update VerificationOIC as System
                                // Update the status of DocSet as Verified
                                bool success = docSetDb.UpdateSetStatus(setId, verificationOIC.Value, SetStatusEnum.Verified, true, false, LogActionEnum.Confirmed_set);


                                //update reference no status
                                DocAppDb docAppDb = new DocAppDb();
                                DocApp.DocAppDataTable docApps = docAppDb.GetDocAppByDocSetId(setId);

                                foreach (DocApp.DocAppRow docAppRow in docApps.Rows)
                                {
                                    // Update the status to "Verified" if the status if "Pending_Documents"
                                    if (docAppRow.Status.Trim().Equals(AppStatusEnum.Pending_Documents.ToString()))
                                        docAppDb.UpdateRefStatus(docAppRow.Id, verificationOIC.Value, AppStatusEnum.Verified, false, false, null);

                                    // Update the Date In if it is null
                                    if (docAppRow.IsDateInNull())
                                        docAppDb.UpdateDateIn(docAppRow.Id, docSetDb.GetEarliestVerificationDateInByDocAppId(docAppRow.Id));
                                }
                            }
                        }
                        //}





                        int createdDocId = docDb.Insert(setId, docType, setId, docStatus.ToString());


                        // Insert the AppDocRef or SetDocRer record
                        AppPersonal.AppPersonalDataTable appPersonalDt = appPersonalDb.GetAppPersonalByNricAndRefNo(nric, refNo);

                        if (appPersonalDt.Rows.Count > 0)
                        {
                            // Insert the association of the doc and doc personal
                            AppPersonal.AppPersonalRow appPersonalDr = appPersonalDt[0];

                            appDocRefDb.Insert(createdDocId, appPersonalDr.Id);
                        }
                        else
                        {
                            // Insert the doc personal record for the doc
                            int docPersonalId = docPersonalDb.Insert(setId, nric, string.Empty,
                                DocFolderEnum.Unidentified.ToString(), string.Empty);

                            // Insert the association of the doc and doc personal
                            setDocRefDb.Insert(createdDocId, docPersonalId);
                        }

                        string certNo = string.Empty;
                        string certDate = string.Empty;
                        bool isForeign = false;
                        bool isMuslim = false;


                        int docPageNo = 1;
                        foreach (FileWebServicInfo file in doc.Files)
                        {
                            string fileName = file.Name;

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

                            foreach (MetadataWebServiceInfo metadata in file.Metadata)
                            {
                                certNo = metadata.CertNo;
                                //2012-12-12
                                //certDate = string.IsNullOrEmpty(metadata.CertDate) ? string.Empty : metadata.CertDate;
                                certDate = metadata.CertDate;

                                isForeign = metadata.IsForeign;
                                isMuslim = metadata.IsMuslim;

                            }
                        }

                        // Get the metadata for the document
                        MetaDataHardCoded metaDataHardCoded = new MetaDataHardCoded(createdDocId, docType);
                        metaDataHardCoded.CreateMetaDataForWebService(certNo, certDate, isForeign, isMuslim);

                        // Insert hard code meta data
                        foreach (MetaDataOcr metaData in metaDataHardCoded.MetaData)
                        {
                            metadataDb.Insert(createdDocId, metaData.FieldName, metaData.FieldValue, metaData.VerificationMandatory, metaData.CompletenessMandatory,
                                metaData.VerificationVisible, metaData.CompletenessVisible, metaData.IsFixed, metaData.MaximumLength, false);
                        }
                    }
                }
            }

            // Update the status of the set
            // If there is a verification officer assigned to the set, the status will be Pending Verification.
            // Else, it will be New
            //2012-12-12 (Suppose the document set is set as verified, due to the webservice import, 
            //
            //
            if (!docSetDb.IsSetVerified(setId))
                docSetDb.UpdateSetStatus(setId, (docSetDb.HasVerificationOfficerAssigned(setId) ? SetStatusEnum.Pending_Verification : SetStatusEnum.New));
        }

        /// <summary>
        /// Parse the Web Service XML file
        /// </summary>
        /// <param name="setId">Set id</param>
        /// <returns>CustomerWebServiceInfo array</returns>
        private CustomerWebServiceInfo[] ParseWebServiceXml(int setId)
        {
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

                        customerWebServiceInfo.RefNo = refNo;

                        // Get the NRIC
                        customerWebServiceInfo.Nric = customer[Constants.WebServiceSetXmlNricTagName].InnerText.Trim();

                        ArrayList documentList = new ArrayList();
                        foreach (XmlNode documentNode in customer.ChildNodes)
                        {
                            if (documentNode.Name.ToUpper().Equals(Constants.WebServiceSetXmlDocTagName))
                            {
                                DocWebServiceInfo docWebServiceInfo = new DocWebServiceInfo();

                                // Get the DocId and DocSubId
                                docWebServiceInfo.DocId = documentNode[Constants.WebServiceSetXmlDocIdTagName].InnerText.Trim();
                                docWebServiceInfo.DocSubId = documentNode[Constants.WebServiceSetXmlDocSubIdTagName].InnerText.Trim();

                                ArrayList fileList = new ArrayList();
                                foreach (XmlNode fileNode in documentNode.ChildNodes)
                                {
                                    if (fileNode.Name.ToUpper().Equals(Constants.WebServiceSetXmlFileTagName))
                                    {
                                        FileWebServicInfo fileWebServiceInfo = new FileWebServicInfo();

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
                                                metadaWebServiceInfo.IsForeign = (!String.IsNullOrEmpty(metadataNode[Constants.WebServiceSetXmlIsForeignTagName].InnerText.Trim()) ?
                                                    bool.Parse(metadataNode[Constants.WebServiceSetXmlIsForeignTagName].InnerText.Trim()) :
                                                    false);
                                                metadaWebServiceInfo.IsMuslim = (!String.IsNullOrEmpty(metadataNode[Constants.WebServiceSetXmlIsMuslimTagName].InnerText.Trim()) ?
                                                    bool.Parse(metadataNode[Constants.WebServiceSetXmlIsMuslimTagName].InnerText.Trim()) :
                                                    false);

                                                //2012-12-12
                                                metadaWebServiceInfo.IsVerified = (!String.IsNullOrEmpty(metadataNode[Constants.WebServiceSetXmlIsVerifiedTagName].InnerText.Trim()) ?
                                                   bool.Parse(metadataNode[Constants.WebServiceSetXmlIsVerifiedTagName].InnerText.Trim()) :
                                                   false);


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

                                FileWebServicInfo[] fileWebServiceInfoArray = new FileWebServicInfo[fileList.Count];

                                for (int cnt = 0; cnt < fileList.Count; cnt++)
                                {
                                    fileWebServiceInfoArray[cnt] = (FileWebServicInfo)fileList[cnt];
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

        #region MISC Processes
        /// <summary>
        /// Delete those sets without files and those files without set information
        /// </summary>
        private void DeleteFailedSets(DirectoryInfo mainDirInfo)
        {
            DocSetDb docSetDb = new DocSetDb();

            #region Sets without files
            // Get those sets with 'Pending Categorization' status
            using (DocSet.DocSetDataTable docSetDt = docSetDb.GetDocSetByStatus(SetStatusEnum.Pending_Categorization.ToString()))
            {
                foreach (DocSet.DocSetRow docSet in docSetDt)
                {
                    // Get the directory of the set
                    string setDirPath = Path.Combine(mainDirInfo.FullName, docSet.Id.ToString());
                    DirectoryInfo dirInfo = new DirectoryInfo(setDirPath);

                    // If the directory does not exists or the directory does not have any subdirectories, delete the set
                    bool dirExists = Directory.Exists(setDirPath);
                    if (!dirExists || (dirExists && dirInfo.GetDirectories().Length <= 0))
                    {
                        if (dirExists)
                        {
                            try
                            {
                                dirInfo.Delete(true);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        docSetDb.Delete(docSet.Id);
                    }
                }
            }
            #endregion

            #region Files without sets
            // Get the all set directories
            DirectoryInfo[] setDirs = mainDirInfo.GetDirectories();

            foreach (DirectoryInfo setDir in setDirs)
            {
                int setId = -1;

                if (int.TryParse(setDir.Name, out setId))
                {
                    // Check if the set record exists for the directory
                    if (docSetDb.GetDocSetById(setId).Rows.Count <= 0)
                    {
                        // Delete the directory
                        try
                        {
                            setDir.Delete(true);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Create set for Other sources documents
        /// </summary>
        /// <param name="source"></param>
        /// <param name="mainDirInfo"></param>
        /// <param name="myDocSummaryXml"></param>
        /// <param name="faxXml"></param>
        /// <param name="pdfFilePath"></param>
        /// <param name="referenceNumberParameter"></param>
        /// <param name="setId"></param>
        /// <returns></returns>
        private bool CreateSet(SourceFileEnum source, DirectoryInfo mainDirInfo, MyDocSummaryXml myDocSummaryXml, FaxSummaryXml faxXml,
            ArrayList filePath, string referenceNumberParameter, string acknowledgementNoParameter, string webServiceChannel, string webServiceXmlContent,
            bool webServiceHasDocId, out int setId)
        {
            setId = -1;

            bool success = false;

            DocSetDb docSetDb = new DocSetDb();
            DocAppDb docAppDb = new DocAppDb();
            MasterListDb masterListDb = new MasterListDb();
            MasterListItemDb masterListItemDb = new MasterListItemDb();
            ProfileDb profileDb = new ProfileDb();
            setId = -1;

            string acknowledgeNo = string.Empty;
            int departmentId = -1;
            int sectionid = -1;
            string referenceType = string.Empty;
            string referenceNo = string.Empty;
            int docAppId = 0;
            string channel = string.Empty;

            // Retrieve the channel and docAppId
            if (source == SourceFileEnum.MyDoc)
            {
                channel = myDocSummaryXml.Source;
                docAppId = myDocSummaryXml.DocAppId;
                referenceNo = myDocSummaryXml.RefNo;
                referenceType = myDocSummaryXml.RefType;
                acknowledgeNo = myDocSummaryXml.AcknowledgementNo;
            }
            else if (source == SourceFileEnum.Fax)
            {
                channel = faxXml.Source;
                docAppId = -1;
                acknowledgeNo = faxXml.AcknowledgementNo;
            }
            else if (source == SourceFileEnum.Scan)
            {
                channel = "Scan";
                referenceNo = referenceNumberParameter;
                referenceType = (String.IsNullOrEmpty(referenceNo) ? string.Empty : Util.GetReferenceType(referenceNo));
                acknowledgeNo = (acknowledgementNoParameter.ToUpper().Contains("LOAN") ? acknowledgementNoParameter.Substring(acknowledgementNoParameter.IndexOf("-") + 1) : acknowledgementNoParameter);
            }
            else if (source == SourceFileEnum.Email)
            {
                channel = "Email";
                referenceNo = referenceNumberParameter;
                referenceType = (String.IsNullOrEmpty(referenceNo) ? string.Empty : Util.GetReferenceType(referenceNo));
                acknowledgeNo = acknowledgementNoParameter;
            }
            else if (source == SourceFileEnum.WebService)
            {
                channel = webServiceChannel;
                referenceNo = referenceNumberParameter;
                referenceType = (String.IsNullOrEmpty(referenceNo) ? string.Empty : Util.GetReferenceType(referenceNo));
                acknowledgeNo = acknowledgementNoParameter;
            }

            string setNo = Util.FormulateSetNumber(docSetDb.GetNextIdNo(), referenceNo, referenceType, out departmentId, out sectionid);

            // Get the channel
            int masterListId = masterListDb.GetMasterListIdByName(MasterListEnum.Uploading_Channels.ToString().Replace("_", " "));

            if (masterListId > 0)
            {
                channel = masterListItemDb.GetMasterListItemName(masterListId, channel);
            }

            // Get the docAppId
            docAppDb.GetAppDetails(docAppId, referenceNo, referenceType, out docAppId);

            // Save the set information
            setId = docSetDb.Insert(setNo, DateTime.Now, SetStatusEnum.Pending_Categorization, string.Empty, 1, string.Empty, string.Empty, channel, importedBy.Value,
                null, departmentId, sectionid, docAppId, acknowledgeNo, webServiceXmlContent, webServiceHasDocId);

            if (setId > 0)
            {
                string setDirPath = Path.Combine(mainDirInfo.FullName, setId.ToString());

                ArrayList filePathList = new ArrayList();

                if (source == SourceFileEnum.MyDoc)
                {
                    filePathList = myDocSummaryXml.Documents;
                    LogActionDb logActionDb = new LogActionDb();

                    if (!String.IsNullOrEmpty(myDocSummaryXml.AcknowledgementNoError))
                    {
                        logActionDb.Insert(importedBy.Value, myDocSummaryXml.AcknowledgementNoError, string.Empty, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, setId);
                    }
                }
                else if (source == SourceFileEnum.Fax)
                {
                    filePathList = faxXml.Documents;
                }
                else if (source == SourceFileEnum.Scan ||
                    source == SourceFileEnum.Email ||
                    source == SourceFileEnum.WebService)
                {
                    if (filePath.Count > 0)
                        filePathList.AddRange(filePath);
                }

                // Save the uploaded documents
                success = SaveUploadedDocument(setId, filePathList, setDirPath, source);

                if (success)
                    // Save the raw file
                    success = SaveRawFile(setId, setDirPath);

                if (success)
                    // Update the 'ReadyForOcr' flag
                    docSetDb.SetReadyForOcr(setId, true);
            }

            return success;
        }

        /// <summary>
        /// Save the uploaded documents
        /// </summary>
        private bool SaveUploadedDocument(int setId, ArrayList documents, string uploadedDocsDir, SourceFileEnum source)
        {
            // If the folder does not exists, create one
            if (!Directory.Exists(uploadedDocsDir))
                Directory.CreateDirectory(uploadedDocsDir);

            ArrayList filePathList = new ArrayList();

            // Get the document file paths
            if (source == SourceFileEnum.MyDoc)
            {
                foreach (MyDocDocumentXml document in documents)
                {
                    filePathList.Add(document.FilePath);
                }
            }
            else if (source == SourceFileEnum.Fax ||
                source == SourceFileEnum.Scan ||
                source == SourceFileEnum.Email ||
                source == SourceFileEnum.WebService)
            {
                foreach (string document in documents)
                {
                    filePathList.Add(document);
                }
            }

            foreach (string filePath in filePathList)
            {
                try
                {
                    FileInfo attachmentFile = new FileInfo(filePath);
                    string newFileName = Path.Combine(uploadedDocsDir, attachmentFile.Name);

                    attachmentFile.CopyTo(newFileName);
                }
                catch (Exception ex)
                {
                    try
                    {
                        // Log the error to show in the set action log
                        string error = String.Format("{0}: Message={1};File={2}", LogActionEnum.File_Error, ex.Message,
                            (filePath.Contains("\\") ? filePath.Substring(filePath.LastIndexOf("\\") + 1) : filePath));

                        LogActionDb logActionDb = new LogActionDb();
                        logActionDb.Insert(importedBy.Value, error, string.Empty, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, setId);
                    }
                    catch (Exception)
                    {
                    }

                    string warningString = String.Format("Warning (DWMS_OCR_Service.SaveUploadedDocument): File={0}, Message={1}, StackTrace={2}",
                        filePath, ex.Message, ex.StackTrace);

                    Util.DWMSLog(string.Empty, warningString, EventLogEntryType.Warning);
                }
            }

            return true;
        }

        /// <summary>
        /// Save the raw file(s)
        /// </summary>
        private bool SaveRawFile(int setId, string uploadedDocsDir)
        {
            RawFileDb rawFileDb = new RawFileDb();

            DirectoryInfo dirInfo = new DirectoryInfo(uploadedDocsDir);
            FileInfo[] files = dirInfo.GetFiles("*.*");

            // Save the uploaded files
            foreach (FileInfo file in files)
            {
                try
                {
                    if (!file.Extension.ToUpper().EndsWith(".DB"))
                    {
                        // Insert the raw file
                        //int temp = rawFileDb.Insert(setId, file.Name, Util.FileToBytes(file.FullName));
                        int temp = rawFileDb.Insert(setId, file.Name, new byte[0]);

                        // Create the folder for each file uploaded
                        string rawFileDir = Path.Combine(uploadedDocsDir, temp.ToString());

                        try
                        {
                            Directory.Delete(rawFileDir);
                        }
                        catch (Exception)
                        {
                        }

                        // Create the directory
                        if (!Directory.Exists(rawFileDir))
                            Directory.CreateDirectory(rawFileDir);

                        string newFileName = Path.Combine(rawFileDir, file.Name);

                        // Copy the file to its respective dir
                        file.MoveTo(newFileName);
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        // Log the error to show in the set action log
                        string error = String.Format("{0}: Message={1};File={2}", LogActionEnum.File_Error, ex.Message, file.Name);

                        LogActionDb logActionDb = new LogActionDb();
                        logActionDb.Insert(importedBy.Value, error, string.Empty, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, setId);
                    }
                    catch (Exception)
                    {
                    }

                    string errorString = String.Format("Error (DWMS_OCR_Service.SaveRawFile): File={0}, Message={1}, StackTract={1}",
                        file.FullName, ex.Message, ex.StackTrace);

                    Util.DWMSLog("DWMS_OCR_Service.SaveRawFile", errorString, EventLogEntryType.Error);
                }
            }

            return true;
        }

        /// <summary>
        /// Update the 'IsBeingProcessed' flag after the OCR and categorization has been done for the set
        /// </summary>
        /// <param name="setIds"></param>
        private void SetIsBeingProcessed(List<int> setIds)
        {
            DocSetDb docSetDb = new DocSetDb();

            foreach (int setId in setIds)
            {
                docSetDb.SetIsBeingProcessed(setId, false);
            }
        }

        /// <summary>
        /// Load sample pages while retrieving the english words.
        /// </summary>
        /// <returns>Sample pages arraylist</returns>
        private ArrayList LoadSamplePagesReturnDictionary()
        {
            // Get some parameter values
            ParameterDb parameterDb = new ParameterDb();
            int MINIMUM_WORD_LENGTH = parameterDb.GetMinimumWordLength();
            int MINIMUM_ENGLISH_WORD_COUNT = parameterDb.GetMinimumEnglishWordCount();
            decimal MINIMUM_ENGLISH_WORD_PERCENTAGE = parameterDb.GetMinimumEnglishWordPercentage();
            double SAMPLE_PAGE_PERCENTAGE = parameterDb.GetTopSamplePagesPercentage();

            // Get the sample documents for categorization
            DocTypeDb docTypeDb = new DocTypeDb();
            DocType.DocTypeDataTable docTypeDt = docTypeDb.GetDocTypes();

            ArrayList documentList = new ArrayList();

            foreach (DocType.DocTypeRow docType in docTypeDt)
            {
                CategorizationSampleDoc categorizationSampleDoc = new CategorizationSampleDoc(docType.Code);

                // Get all the sample pages for the document type
                categorizationSampleDoc.GetSamplePages(MINIMUM_WORD_LENGTH, MINIMUM_ENGLISH_WORD_COUNT, MINIMUM_ENGLISH_WORD_PERCENTAGE);

                // Get the top sample documents
                categorizationSampleDoc.GetTopSamplePages();

                // Add the sample pages of the document type to the list
                documentList.Add(categorizationSampleDoc);
            }

            return documentList;
        }

        /// <summary>
        /// Remove the least matched sample documents that exceed the maximum allowed.
        /// </summary>
        private void RemoveLeastMatchedSampleDocuments()
        {
            RelevanceRankingDb relevanceRankingDb = new RelevanceRankingDb();
            DocTypeDb docTypeDb = new DocTypeDb();

            // Get all the document types
            DocType.DocTypeDataTable docTypeDt = docTypeDb.GetDocTypes();

            // For each document type, remove the least matched sample documents
            foreach (DocType.DocTypeRow docTypeDr in docTypeDt)
            {
                relevanceRankingDb.DeleteLeastSampleDocument(docTypeDr.Code);
            }
        }

        private void PreparePdfForProcessing(int setId, FileInfo file, DirectoryInfo rawPageMainDirInfo, DirectoryInfo rawFilesDir, ref ArrayList pages)
        {
            if (!Util.IsSecuredPdf(file.FullName))
            {
                #region Non-secured PDF processing
                RawPageDb rawPageDb = new RawPageDb();
                LogActionDb logActionDb = new LogActionDb();

                ArrayList pdfArrayList = Util.PdfSplit(file.FullName, rawFilesDir.FullName, 1);
                //ArrayList pdfArrayList = Util.SaveAsImageUsingAcdPdf(file.FullName); -- Splitting using ABCpdf

                int pageNo = 1;

                // Add the PDF file paths to the array
                foreach (string pdfPagePath in pdfArrayList)
                {
                    FileInfo pdfFile = new FileInfo(pdfPagePath);

                    // Save the raw page                                
                    int rawPageId = rawPageDb.Insert(int.Parse(rawFilesDir.Name), pageNo, new byte[0],
                        string.Empty, new byte[0], new byte[0], false);

                    if (rawPageId > 0)
                    {
                        string rawPageTempPath = Path.Combine(rawPageMainDirInfo.FullName, rawPageId.ToString());

                        // If the folder does not exists, create one
                        if (!Directory.Exists(rawPageTempPath))
                            Directory.CreateDirectory(rawPageTempPath);

                        string newRawPageTempPath = Path.Combine(rawPageTempPath, pdfFile.Name);

                        try
                        {
                            #region Old Implementation - Convert the PDF to TIFF
                            //string imagePath = Util.SaveAsImage(pdfPagePath); -- Converting to image
                            //FileInfo imageFile = new FileInfo(imagePath); -- Converting to image

                            // Copy the file into a folder that has a name equal to the RawPage Id  
                            //string newRawPageTempPath = Path.Combine(rawPageTempPath, pdfFile.Name);
                            //string newRawPageTempPath = Path.Combine(rawPageTempPath, imageFile.Name); -- Converting to image
                            //string newRawPageTempPath = Path.Combine(rawPageTempPath, pdfFile.Name); -- Splitting using ABCpdf

                            // Move the file
                            //imageFile.MoveTo(newRawPageTempPath); -- Converting to image
                            //pdfFile.MoveTo(newRawPageTempPath); -- Splitting using ABCpdf

                            // Save as JPEG image
                            //string imagePath = Util.SaveAsJpegImage(newRawPageTempPath);
                            //FileInfo imageFile = new FileInfo(imagePath);

                            // Create the thumbnail file
                            //string thumbnail = ImageManager.Resize(imageFile.FullName);
                            //string thumbnail = ImageManager.Resize(newRawPageTempPath); -- Converting to image

                            //try
                            //{
                            //    // Delete the JPEG file
                            //    imageFile.Delete();
                            //}
                            //catch (Exception)
                            //{
                            //}
                            #endregion

                            // Move the file
                            pdfFile.CopyTo(newRawPageTempPath, true);

                            try
                            {
                                // Create the thumbnail file
                                //ImageManager.Resize(newRawPageTempPath, 113, 160);
                                string tempImagePath = Util.SaveAsJpegThumbnailImage(newRawPageTempPath);
                                string thumbNailPath = ImageManager.Resize(tempImagePath);

                                try
                                {
                                    File.Delete(tempImagePath);
                                }
                                catch
                                {
                                }
                            }
                            catch (Exception)
                            {
                                // Log the error to show in the set action log
                                logActionDb.Insert(importedBy.Value,
                                    LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
                                    LogActionEnum.Thumbnail_Creation_Error.ToString(),
                                    pdfFile.FullName.Contains("\\") ? pdfFile.FullName.Substring(pdfFile.FullName.LastIndexOf("\\") + 1) : pdfFile.FullName,
                                    string.Empty, string.Empty, LogTypeEnum.S, setId);
                            }

                            // Add the page to the page list
                            string[] pageData = new string[2];
                            pageData[0] = rawPageId.ToString();
                            pageData[1] = newRawPageTempPath;
                            pages.Add(pageData);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                // Log in the windows service log
                                string errorSummary = string.Format("Error (DWMS_OCR_Service.PreparePdfForProcessing): File={0}, Message={1}, StackTrace={2}",
                                    pdfFile.FullName, ex.Message, ex.StackTrace);
                                Util.DWMSLog("DWMS_OCR_Service.PreparePdfForProcessing", errorSummary, EventLogEntryType.Error);

                                // Log the error to show in the set action log
                                logActionDb.Insert(importedBy.Value,
                                    LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_REPLACE2_SEMICOLON_File_EQUALSSIGN_REPLACE3.ToString(),
                                    LogActionEnum.File_Error.ToString(),
                                    ex.Message,
                                    pdfFile.FullName.Contains("\\") ? pdfFile.FullName.Substring(pdfFile.FullName.LastIndexOf("\\") + 1) : pdfFile.FullName,
                                    string.Empty, LogTypeEnum.S, setId);

                                // Use the original PDF file for the View page
                                // Copy the file into a folder that has a name equal to the RawPage Id                                        
                                newRawPageTempPath = Path.Combine(rawPageTempPath, pdfFile.Name + "_s.pdf");

                                // Move the file
                                pdfFile.CopyTo(newRawPageTempPath);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    // Delete the original
                    try
                    {
                        pdfFile.Delete();
                    }
                    catch (Exception)
                    {
                    }

                    pageNo++;
                }
                #endregion
            }
            else
            {
                #region Secured PDF processing
                ArrayList imageArrayList = Util.SavePdfToIndividualImage(file.FullName);

                // Add the PDF file paths to the array
                foreach (string imagePagePath in imageArrayList)
                {
                    PrepareImagesForProcessing(setId, new FileInfo(imagePagePath), rawPageMainDirInfo, rawFilesDir, ref pages);
                }
                #endregion
            }
        }

        private void PrepareMultiPageTiffForProcessing(int setId, FileInfo file, DirectoryInfo rawPageMainDirInfo, DirectoryInfo rawFilesDir, ref ArrayList pages)
        {
            RawPageDb rawPageDb = new RawPageDb();
            LogActionDb logActionDb = new LogActionDb();

            ArrayList tiffArrayList = Util.TiffSplit(file.FullName, rawFilesDir.FullName);

            int pageNo = 1;

            // Add the TIFF file paths to the array
            foreach (string tiffPagePath in tiffArrayList)
            {
                FileInfo tiffFile = new FileInfo(tiffPagePath);

                // Save the raw page                                
                int rawPageId = rawPageDb.Insert(int.Parse(rawFilesDir.Name), pageNo, new byte[0],
                    string.Empty, new byte[0], new byte[0], false);

                if (rawPageId > 0)
                {
                    string rawPageTempPath = Path.Combine(rawPageMainDirInfo.FullName, rawPageId.ToString());

                    // If the folder does not exists, create one
                    if (!Directory.Exists(rawPageTempPath))
                        Directory.CreateDirectory(rawPageTempPath);

                    // Copy the file into a folder that has a name equal to the RawPage Id                                        
                    string newRawPageTempPath = Path.Combine(rawPageTempPath, tiffFile.Name);

                    // Move the file
                    try
                    {
                        tiffFile.CopyTo(newRawPageTempPath);

                        try
                        {
                            // Create the thumbnail file
                            ImageManager.Resize(newRawPageTempPath);
                        }
                        catch (Exception)
                        {
                            // Log the error to show in the set action log
                            logActionDb.Insert(importedBy.Value,
                                LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
                                LogActionEnum.Thumbnail_Creation_Error.ToString(),
                                tiffFile.FullName.Contains("\\") ? tiffFile.FullName.Substring(tiffFile.FullName.LastIndexOf("\\") + 1) : tiffFile.FullName,
                                string.Empty, string.Empty, LogTypeEnum.S, setId);
                        }

                        // Add the page to the page list
                        string[] pageData = new string[2];
                        pageData[0] = rawPageId.ToString();
                        pageData[1] = newRawPageTempPath;
                        pages.Add(pageData);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            // Log in the windows service log
                            string errorSummary = string.Format("Error (DWMS_OCR_Service.PrepareMultiPageTiffForProcessing): File={0}, Message={1}, StackTrace={2}",
                                tiffFile.FullName, ex.Message, ex.StackTrace);
                            Util.DWMSLog("DWMS_OCR_Service.PrepareMultiPageTiffForProcessing", errorSummary, EventLogEntryType.Error);

                            // Log the error to show in the set action log
                            logActionDb.Insert(importedBy.Value,
                                LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_REPLACE2_SEMICOLON_File_EQUALSSIGN_REPLACE3.ToString(),
                                LogActionEnum.File_Error.ToString(),
                                ex.Message,
                                tiffFile.FullName.Contains("\\") ? tiffFile.FullName.Substring(tiffFile.FullName.LastIndexOf("\\") + 1) : tiffFile.FullName,
                                string.Empty, LogTypeEnum.S, setId);

                            // Use the original TIFF file for the View page
                            Util.CreateSearcheablePdfFile(tiffFile.FullName);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    // Copy the original
                    try
                    {
                        // Copy the original file to the individual folders
                        //string origRawPageTempPath = Path.Combine(rawPageTempPath, tiffFile.Name);
                        //tiffFile.CopyTo(origRawPageTempPath);
                    }
                    catch (Exception)
                    {
                    }
                }

                // Delete the original
                try
                {
                    tiffFile.Delete();
                }
                catch (Exception)
                {
                }

                pageNo++;
            }
        }

        private void PrepareImagesForProcessing(int setId, FileInfo file, DirectoryInfo rawPageMainDirInfo, DirectoryInfo rawFilesDir, ref ArrayList pages)
        {
            RawPageDb rawPageDb = new RawPageDb();
            LogActionDb logActionDb = new LogActionDb();

            // Save the raw page
            int rawPageId = rawPageDb.Insert(int.Parse(rawFilesDir.Name), 1, new byte[0],
                string.Empty, new byte[0], new byte[0], false);

            if (rawPageId > 0)
            {
                //string imagePath = Util.SaveAsImage(file.FullName); -- Converting to image

                //FileInfo imageFile = new FileInfo(imagePath); -- Converting to image

                // Copy the file into a folder that has a name equal to the RawPage Id
                string rawPageTempPath = Path.Combine(rawPageMainDirInfo.FullName, rawPageId.ToString());
                string newRawPageTempPath = Path.Combine(rawPageTempPath, file.Name);
                //string newRawPageTempPath = Path.Combine(rawPageTempPath, imageFile.Name); -- Converting to image

                // If the folder does not exists, create one
                if (!Directory.Exists(rawPageTempPath))
                    Directory.CreateDirectory(rawPageTempPath);

                try
                {
                    // Copy the file
                    file.CopyTo(newRawPageTempPath);
                    //imageFile.MoveTo(newRawPageTempPath); -- Converting to image

                    // Create the thumbnail file
                    //string thumbnail = ImageManager.Resize(newRawPageTempPath);

                    try
                    {
                        // Create the thumbnail file
                        ImageManager.Resize(newRawPageTempPath);
                    }
                    catch (Exception)
                    {
                        // Log the error to show in the set action log
                        logActionDb.Insert(importedBy.Value,
                            LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
                            LogActionEnum.Thumbnail_Creation_Error.ToString(),
                            file.FullName.Contains("\\") ? file.FullName.Substring(file.FullName.LastIndexOf("\\") + 1) : file.FullName,
                            string.Empty, string.Empty, LogTypeEnum.S, setId);
                    }

                    // Add the page to the page list
                    string[] pageData = new string[2];
                    pageData[0] = rawPageId.ToString();
                    pageData[1] = newRawPageTempPath;
                    pages.Add(pageData);
                }
                catch (Exception ex)
                {
                    try
                    {
                        // Log in the windows service log
                        string errorSummary = string.Format("Error (DWMS_OCR_Service.PrepareImagesForProcessing): File={0}, Message={1}, StackTrace={2}",
                                file.FullName, ex.Message, ex.StackTrace);
                        Util.DWMSLog("DWMS_OCR_Service.PrepareImagesForProcessing", errorSummary, EventLogEntryType.Error);

                        // Log the error to show in the set action log
                        logActionDb.Insert(importedBy.Value,
                            LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_REPLACE2_SEMICOLON_File_EQUALSSIGN_REPLACE3.ToString(),
                            LogActionEnum.File_Error.ToString(),
                            ex.Message,
                            file.FullName.Contains("\\") ? file.FullName.Substring(file.FullName.LastIndexOf("\\") + 1) : file.FullName,
                            string.Empty, LogTypeEnum.S, setId);

                        // Use the original image file for the View page
                        Util.CreateSearcheablePdfFile(file.FullName);
                    }
                    catch (Exception)
                    {
                    }
                }

                // Copy the original
                try
                {
                    // Copy the original file to the individual folders
                    //string origRawPageTempPath = Path.Combine(rawPageTempPath, file.Name);
                    //file.CopyTo(origRawPageTempPath);
                }
                catch (Exception)
                {
                }
            }
        }
        #endregion
        #endregion
    }

    class ThreadInfo
    {
        // OCR Parameters
        public int RawPageId { get; set; }
        public string FilePath { get; set; }

        // Categorization Parameters
        public int SetId { get; set; }

        // OCR Parameters
        public int Binarize { get; set; }
        public string Morph { get; set; }
        public int BackgroundFactor { get; set; }
        public int ForegroundFactor { get; set; }
        public int Quality { get; set; }
        public bool DotMatrix { get; set; }
        public int Despeckle { get; set; }
    }

    class SetInfo
    {
        public string DirectoryPath { get; set; }
        public string NewDirectoryPath { get; set; }
        public string ErrorReason { get; set; }
        public string ErrorException { get; set; }
        public string ReferenceNumber { get; set; }
        public string Documents { get; set; }
    }

    class CustomerWebServiceInfo
    {
        public string RefNo { get; set; }
        public string Nric { get; set; }
        public DocWebServiceInfo[] Documents { get; set; }
    }

    class DocWebServiceInfo
    {
        public string DocId { get; set; }
        public string DocSubId { get; set; }
        public FileWebServicInfo[] Files { get; set; }
    }

    class FileWebServicInfo
    {
        public string Name { get; set; }
        public MetadataWebServiceInfo[] Metadata { get; set; }
    }

    class MetadataWebServiceInfo
    {
        public string CertNo { get; set; }
        public string CertDate { get; set; }
        public bool IsForeign { get; set; }
        public bool IsMuslim { get; set; }

        //2012-12-12
        public bool IsVerified { get; set; }


    }
}
