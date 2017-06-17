using System;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using DWMS_OCR.App_Code.Helper;
using System.IO;
using DWMS_OCR.App_Code.Bll;
using System.Collections;

namespace DWMS_OCR.MaintenanceService
{
    partial class DWMS_Maintenance : ServiceBase
    {
        byte notificationMailSent = 0;
        byte secondTimer;
        #region Members and Constructors
        public DWMS_Maintenance()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists(Constants.DWMSMaintenanceLogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(Constants.DWMSMaintenanceLogSource, Constants.DWMSMaintenanceLog);
            }

            eventLog.Source = Constants.DWMSMaintenanceLogSource;
            eventLog.Log = Constants.DWMSMaintenanceLog;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Windows Service Start
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            // Start the timer
            Util.MaintenanceLog(string.Empty, "DWMS_Maintenance_Service Started.", EventLogEntryType.Information);
            timer.Enabled = true;
            timer.Start();
        }

        /// <summary>
        /// Windows Service continue
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();

            Util.MaintenanceLog(string.Empty, "DWMS_Maintenance_Service Continued.", EventLogEntryType.Information);
            timer.Start();
        }

        /// <summary>
        /// Windows Service stop
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();

            Util.MaintenanceLog(string.Empty, "DWMS_Maintenance_Service Paused.", EventLogEntryType.Information);
            timer.Stop();
        }

        /// <summary>
        /// Windows Service Stop
        /// </summary>
        protected override void OnStop()
        {
            Util.MaintenanceLog(string.Empty, "DWMS_Maintenance_Service Stopped.", EventLogEntryType.Information);
            timer.Enabled = false;
            timer.Stop();
        }

        /// <summary>
        /// Windows Service shutdown
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();

            Util.MaintenanceLog(string.Empty, "DWMS_Maintenance_Service Shut down.", EventLogEntryType.Information);
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
                OCRChecker();
                // Check OcrLastWorking in Parameter Table. If more than certain time, send eMail notification
                //CheckOcrLastWorking();

                secondTimer++;
                //after 12x timer_elapsed, update the OcrLastWorking parameter value in Parameter DB
                if (secondTimer > 10)
                {
                    secondTimer = 0;
                // Delete from temporary folders
                DeleteFromTempFolder();

                // Delete the raw pages without sets
                //DeleteRawPagesWithoutSets(); too many folders to process

                // Clean up the external sources folder
                CleanUpExternalSourcesFolder();

                CleanUpDoc();

                NoDocset();

                // Delete the sample documents folders without sample document record in the DB
                CleanUpSampleDoc();
                }

            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Error (DWMS_Maintenance): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                Util.MaintenanceLog(string.Empty, errorMessage, EventLogEntryType.Error);
            }

            // Start the timer again
            timer.Enabled = true;
            timer.Start();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Delete from temporary folder
        /// </summary>
        private void DeleteFromTempFolder()
        {
            string tempDirPath = Retrieve.GetTempDirPath();
            DirectoryInfo tempDir = new DirectoryInfo(tempDirPath);

            #region Delete temporary folders
            DirectoryInfo[] dirInfos = tempDir.GetDirectories();

            foreach (DirectoryInfo dirInfo in dirInfos)
            {
                // Check the age of the directory
                // If the age is greater than or equal to 10 days, delete the directory
                ParameterDb parameterDb = new ParameterDb();
                TimeSpan difference = DateTime.Now.Subtract(dirInfo.CreationTime);

                if (difference.TotalDays >= parameterDb.GetMinimumAgeForTemporaryFiles())
                {
                    try
                    {
                        dirInfo.Delete(true);
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (DWMS_Maintenance.DeleteFromTempFolder): Directory={0}, Message={1}, StackTrace={2}", 
                            dirInfo.Name, ex.Message, ex.StackTrace);
                        Util.MaintenanceLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                }
            }
            #endregion

            #region Delete temporary files
            FileInfo[] fileInfos = tempDir.GetFiles();

            foreach (FileInfo fileInfo in fileInfos)
            {
                if (!fileInfo.Extension.ToUpper().Equals(".DB"))
                {
                    // Check the age of the directory
                    // If the age is greater than or equal to 10 days, delete the directory
                    ParameterDb parameterDb = new ParameterDb();
                    TimeSpan difference = DateTime.Now.Subtract(fileInfo.CreationTime);

                    if (difference.TotalDays >= parameterDb.GetMinimumAgeForTemporaryFiles())
                    {
                        try
                        {
                            fileInfo.Delete();
                        }
                        catch (Exception ex)
                        {
                            string warningMessage = String.Format("Warning (DWMS_Maintenance.DeleteFromTempFolder): File={0}, Message={1}, StackTrace={2}",
                                fileInfo.Name, ex.Message, ex.StackTrace);
                            Util.MaintenanceLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                        }
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Delete the raw pages without sets
        /// </summary>
        private void DeleteRawPagesWithoutSets()
        {
            string rawPageDirPath = Retrieve.GetRawPageOcrDirPath();
            DirectoryInfo rawPageDir = new DirectoryInfo(rawPageDirPath);

            #region Delete Raw Pages without sets
            DirectoryInfo[] rawPageDirInfos = rawPageDir.GetDirectories();

            foreach (DirectoryInfo dirInfo in rawPageDirInfos)
            {
                // Check the age of the directory
                // If the age is greater than or equal to 10 days, delete the directory
                ParameterDb parameterDb = new ParameterDb();
                TimeSpan difference = DateTime.Now.Subtract(dirInfo.CreationTime);

                if (difference.TotalDays >= parameterDb.GetMinimumAgeForTemporaryFiles())
                {
                    int rawPageId = 0;

                    int.TryParse(dirInfo.Name, out rawPageId);

                    if (rawPageId > 0)
                    {
                        RawFileDb rawFileDb = new RawFileDb();

                        if (rawFileDb.GetRawFilesByRawPageId(rawPageId).Rows.Count <= 0)
                        {
                            try
                            {
                                dirInfo.Delete(true);
                            }
                            catch (Exception ex)
                            {
                                string warningMessage = String.Format("Warning (DWMS_Maintenance.DeleteRawPagesWithoutSets): Dir={0}, Message={1}, StackTrace={2}",
                                    dirInfo.Name, ex.Message, ex.StackTrace);

                                Util.MaintenanceLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                            }
                        }
                    }
                }
            }
            #endregion
        }

        private void CleanUpExternalSourcesFolder()
        {
            CleanUpMyDoc();
            CleanUpScan();
            CleanUpEmail();
            CleanUpFax();
        }

        private void CleanUpMyDoc()
        {
            string myDocsDirPath = Retrieve.GetMyDocForOcrDirPath();
            DirectoryInfo myDocMainDirInfo = new DirectoryInfo(myDocsDirPath);

            #region Get the Imported Docs Directory
            string importedDocsDirPath = Retrieve.GetImportedMyDocsOcrDirPath();

            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);

            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);
            #endregion

            #region Get the Failed Docs Directory
            string failedDocsDirPath = Retrieve.GetFailedMyDocsOcrDirPath();

            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            DirectoryInfo failedDirInfo = new DirectoryInfo(failedDocsDirPath);
            #endregion

            // Get the subfolders of the main dir
            DirectoryInfo[] subDirInfos = myDocMainDirInfo.GetDirectories();
            
            foreach (DirectoryInfo subDirInfo in subDirInfos)
            {
                if (!subDirInfo.Name.ToLower().Equals("imported") && !subDirInfo.Name.ToLower().Equals("failed") &&
                    (subDirInfo.Name.ToLower().EndsWith("_imported") || subDirInfo.Name.ToLower().EndsWith("_failed")))
                {
                    try
                    {
                        string newDir = string.Empty;
                        if (subDirInfo.Name.ToLower().EndsWith("_imported"))
                            newDir = Path.Combine(importedDirInfo.FullName, subDirInfo.Name);
                        else if (subDirInfo.Name.ToLower().EndsWith("_failed"))
                            newDir = Path.Combine(failedDirInfo.FullName, subDirInfo.Name);

                        if (!String.IsNullOrEmpty(newDir))
                        {
                            if (Directory.Exists(newDir))
                                newDir = newDir + Guid.NewGuid().ToString().Substring(0, 8);

                            subDirInfo.MoveTo(newDir);
                        }
                    }
                    catch(Exception ex)
                    {
                        string warningMessage = String.Format("Warning (CleanUpMyDoc): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.MaintenanceLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                }
            }
        }

        private void CleanUpScan()
        {
            string scanDirPath = Retrieve.GetScanForOcrDirPath();
            DirectoryInfo scanMainDirInfo = new DirectoryInfo(scanDirPath);

            #region Get the Imported Docs Directory
            string importedDocsDirPath = Retrieve.GetImportedScanOcrDirPath();

            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);

            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);
            #endregion

            #region Get the Failed Docs Directory
            string failedDocsDirPath = Retrieve.GetFailedScanOcrDirPath();

            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            DirectoryInfo failedDirInfo = new DirectoryInfo(failedDocsDirPath);
            #endregion

            // Get the subfolders of the main dir
            DirectoryInfo[] subDirInfos = scanMainDirInfo.GetDirectories();

            foreach (DirectoryInfo subDirInfo in subDirInfos)
            {
                if (!subDirInfo.Name.ToLower().Equals("imported") && !subDirInfo.Name.ToLower().Equals("failed") &&
                    (subDirInfo.Name.ToLower().EndsWith("_imported") || subDirInfo.Name.ToLower().EndsWith("_failed")))
                {
                    try
                    {
                        string newDir = string.Empty;
                        if (subDirInfo.Name.ToLower().EndsWith("_imported"))
                            newDir = Path.Combine(importedDirInfo.FullName, subDirInfo.Name);
                        else if (subDirInfo.Name.ToLower().EndsWith("_failed"))
                            newDir = Path.Combine(failedDirInfo.FullName, subDirInfo.Name);

                        if (!String.IsNullOrEmpty(newDir))
                        {
                            if (Directory.Exists(newDir))
                                newDir = newDir + Guid.NewGuid().ToString().Substring(0, 8);

                            subDirInfo.MoveTo(newDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (CleanUpScan): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.MaintenanceLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                }
            }
        }

        private void CleanUpEmail()
        {
            string emailDirPath = Retrieve.GetEmailForOcrDirPath();
            DirectoryInfo emailMainDirInfo = new DirectoryInfo(emailDirPath);

            #region Get the Imported Docs Directory
            string importedDocsDirPath = Retrieve.GetImportedEmailOcrDirPath();

            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);

            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);
            #endregion

            #region Get the Failed Docs Directory
            string failedDocsDirPath = Retrieve.GetFailedEmailOcrDirPath();

            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            DirectoryInfo failedDirInfo = new DirectoryInfo(failedDocsDirPath);
            #endregion

            // Get the subfolders of the main dir
            DirectoryInfo[] subDirInfos = emailMainDirInfo.GetDirectories();

            foreach (DirectoryInfo subDirInfo in subDirInfos)
            {
                if (!subDirInfo.Name.ToLower().Equals("imported") && !subDirInfo.Name.ToLower().Equals("failed") &&
                    (subDirInfo.Name.ToLower().EndsWith("_imported") || subDirInfo.Name.ToLower().EndsWith("_failed")))
                {
                    try
                    {
                        string newDir = string.Empty;
                        if (subDirInfo.Name.ToLower().EndsWith("_imported"))
                            newDir = Path.Combine(importedDirInfo.FullName, subDirInfo.Name);
                        else if (subDirInfo.Name.ToLower().EndsWith("_failed"))
                            newDir = Path.Combine(failedDirInfo.FullName, subDirInfo.Name);

                        if (!String.IsNullOrEmpty(newDir))
                        {
                            if (Directory.Exists(newDir))
                                newDir = newDir + Guid.NewGuid().ToString().Substring(0, 8);

                            subDirInfo.MoveTo(newDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        string warningMessage = String.Format("Warning (CleanUpEmail): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                        Util.MaintenanceLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                    }
                }
            }
        }

        private void CleanUpFax()
        {
            string faxDirPath = Retrieve.GetFaxForOcrDirPath();
            DirectoryInfo faxMainDirInfo = new DirectoryInfo(faxDirPath);

            #region Get the Imported Docs Directory
            string importedDocsDirPath = Retrieve.GetImportedFaxOcrDirPath();

            if (!Directory.Exists(importedDocsDirPath))
                Directory.CreateDirectory(importedDocsDirPath);

            DirectoryInfo importedDirInfo = new DirectoryInfo(importedDocsDirPath);
            #endregion

            #region Get the Failed Docs Directory
            string failedDocsDirPath = Retrieve.GetFailedFaxOcrDirPath();

            if (!Directory.Exists(failedDocsDirPath))
                Directory.CreateDirectory(failedDocsDirPath);

            DirectoryInfo failedDirInfo = new DirectoryInfo(failedDocsDirPath);
            #endregion

            // Get the files of the main dir
            ArrayList files = new ArrayList();
            files.AddRange(faxMainDirInfo.GetFiles("*_IMPORTED"));
            files.AddRange(faxMainDirInfo.GetFiles("*_FAILED"));

            FileInfo[] fileInfos = (FileInfo[])files.ToArray(typeof(FileInfo));

            foreach(FileInfo fileInfo in fileInfos)
            {
                try
                {
                    string newFilePath = string.Empty;
                    if (fileInfo.Name.ToLower().EndsWith("_imported"))
                        newFilePath = Path.Combine(importedDirInfo.FullName, fileInfo.Name);
                    else if (fileInfo.Name.ToLower().EndsWith("_failed"))
                        newFilePath = Path.Combine(failedDirInfo.FullName, fileInfo.Name);

                    if (!String.IsNullOrEmpty(newFilePath))
                    {
                        if (File.Exists(newFilePath))
                        {
                            string fileNameWoExtension = newFilePath.Substring(0, newFilePath.LastIndexOf("."));
                            string extension = newFilePath.Substring(newFilePath.LastIndexOf("."));
                            newFilePath = fileNameWoExtension + Guid.NewGuid().ToString().Substring(0, 8) + extension;
                        }

                        fileInfo.MoveTo(newFilePath);
                    }
                }
                catch (Exception ex)
                {
                    string warningMessage = String.Format("Warning (CleanUpFax): Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                    Util.MaintenanceLog(string.Empty, warningMessage, EventLogEntryType.Warning);
                }
            }
        }

        private void CleanUpDoc()
        {
            DocDb docDb = new DocDb();
            DocSetDb docSetDb = new DocSetDb();

            DataTable dt = docDb.GetDistinctOrigSetIdForNullSetId();

            foreach (DataRow dr in dt.Rows)
            {
                int originalSetId = int.Parse(dr["OriginalSetId"].ToString());

                if (docSetDb.GetDocSetById(originalSetId).Rows.Count <= 0)
                {
                    docDb.DeleteByOriginalSetIdSetIdNull(originalSetId);
                }
            }
        }

        private void CleanUpSampleDoc()
        {
            SampleDocDb sampleDocDb = new SampleDocDb();

            string sampleDocDirPath = Retrieve.GetSampleDocsForOcrDirPath();

            DirectoryInfo sampleDocDirInfo = new DirectoryInfo(sampleDocDirPath);

            DirectoryInfo[] docTypeDirs = sampleDocDirInfo.GetDirectories();

            foreach(DirectoryInfo docTypeDir in docTypeDirs)
            {
                DirectoryInfo[] sampleDocs = docTypeDir.GetDirectories();

                foreach(DirectoryInfo sampleDoc in sampleDocs)
                {
                    int sampleDocId = -1;

                    if (int.TryParse(sampleDoc.Name, out sampleDocId))
                    {
                        if (sampleDocDb.GetSampleDocById(sampleDocId).Rows.Count <= 0)
                        {
                            try
                            {
                                sampleDoc.Delete(true);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
        }

        private void NoDocset()
        {
            #region Files without sets
            DocSetDb docSetDb = new DocSetDb();
            RawFileDb rawFileDb = new RawFileDb();
            string warningString;
            string forOcrDocDirPath = Retrieve.GetDocsForOcrDirPath();
            DirectoryInfo mainDirInfo = new DirectoryInfo(forOcrDocDirPath);
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
                            if (setDir.Exists && setDir.GetDirectories().Length <= 0)
                            {
                                //if (!(rawFileDb.GetRawFilesByDocSetId(setId).Count > 0))
                                //    docSetDb.UpdateSetStatus(setId, SetStatusEnum.Categorization_Failed);//temp for checking
                                setDir.Delete(true);
                                warningString = String.Format("Delete: Directory " + setId.ToString() + " have no Docset: File={0}", setDir.Name);
                                Util.MaintenanceLog(string.Empty, warningString, EventLogEntryType.Error);
                            }
                            else
                            {
                                warningString = String.Format("Not Deleted: Directory " + setId.ToString() + " have no Docset: File={0}", setDir.Name);
                                Util.MaintenanceLog(string.Empty, warningString, EventLogEntryType.Warning);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            #endregion
        }

        private void OCRChecker()
        {
            try
            {
                ParameterDb parameterDb = new ParameterDb();
                DateTime ocrLastWorking = DateTime.Parse(parameterDb.GetParameterValue(ParameterNameEnum.OcrLastWorking));
                DateTime currentTime = DateTime.Now;

                ServiceController serviceController = new ServiceController("DWMS_OCR_Checker");
                if (serviceController.Status.ToString() != "Running")
                {
                    //TimeSpan timeout = TimeSpan.FromMinutes(15);
                    //serviceController.Start();
                    //serviceController.WaitForStatus(ServiceControllerStatus.Running, timeout);
                    Util.MaintenanceLog(string.Empty, "OCR Checker" + serviceController.Status.ToString(), EventLogEntryType.Information);
                    try
                    {
                        System.Diagnostics.Process.Start(ConfigurationManager.AppSettings["SerivceRestart"].Trim());
                        serviceController.Close();
                    }
                    catch { Util.MaintenanceLog(string.Empty, "Failed to restart OCR Checker" + serviceController.Status.ToString(), EventLogEntryType.Error); }
                }
            }
            catch (Exception ex)
            {
                Util.MaintenanceLog("DWMS_Maintenance.CheckOcrLastWorking", "OCR Checker Is Error. " + ex, EventLogEntryType.Error);
            }
        }

        private void CheckOcrLastWorking()
        {
            try
            {
                ParameterDb parameterDb = new ParameterDb();
                DateTime ocrLastWorking = DateTime.Parse(parameterDb.GetParameterValue(ParameterNameEnum.OcrLastWorking));
                DateTime currentTime = DateTime.Now;

                // to compare between current time and OcrLastWorking value
                TimeSpan diff = currentTime.Subtract(ocrLastWorking);

                if (diff.TotalMinutes >= int.Parse(parameterDb.GetParameterValue(ParameterNameEnum.MaxTimeOcrNotWorkingTrigger).Trim()))
                {   // if the difference is more than time allowed and email hasn't been sent more than maximum times, send notification email and put some warning in the log 
                    ServiceController serviceController = new ServiceController("DWMS_OCR_Service_New");
                    if (notificationMailSent < int.Parse(parameterDb.GetParameterValue(ParameterNameEnum.MaxNotifSent).Trim()) && serviceController.Status.ToString() != "StartPending" && Convert.ToInt16(diff.TotalMinutes) % int.Parse(parameterDb.GetParameterValue(ParameterNameEnum.MaxTimeOcrNotWorkingTrigger).Trim()) == 0)
                    {
                        string subject = "OCR Error Notification";
                        string mailMessage = "According to record, OCR Status is " + serviceController.Status.ToString() + "\nOCR Last Working was updated " + Convert.ToInt16(diff.TotalMinutes) + " minutes ago.\nPlease contact the admin to check the DWMS OCR Service if it's still working. Thanks";
                        string recipientEmail = parameterDb.GetParameterValue(ParameterNameEnum.ErrorNotificationMailingList).Trim();
                        try
                        {
                            bool sent = Util.SendMail(parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), recipientEmail, string.Empty, string.Empty, subject, mailMessage);
                            //if (sent)
                            //{
                            notificationMailSent++;
                            string logMessage = "OcrLastWorking was updated " + Convert.ToInt16(diff.TotalMinutes) + " minutes ago. DWMS OCR Service is " +  serviceController.Status.ToString()+ ". Notification mail is being sent to " + recipientEmail;
                            Util.MaintenanceLog(string.Empty, logMessage, EventLogEntryType.Error);
                            if (serviceController.Status.ToString() == "Running")
                            {
                                //serviceController.Dispose();
                                Util.MaintenanceLog(string.Empty, "Try to restart OCR engine :" + serviceController.Status.ToString(), EventLogEntryType.Error);
                                //mailMessage = "Trying to restart OCR engine.\nPlease contact the admin to check the DWMS OCR Service if it's still working. Thanks";
                                //Util.SendMail(parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), recipientEmail, string.Empty, string.Empty, subject, mailMessage);
                                try
                                {
                                    System.Diagnostics.Process.Start(ConfigurationManager.AppSettings["SerivceRestart"].Trim());
                                    serviceController.Close();
                                }
                                catch { Util.MaintenanceLog(string.Empty, "Failed to stop OCR engine" + serviceController.Status.ToString(), EventLogEntryType.Error); }
                            }
                            else if (serviceController.Status.ToString() == "Stopped")
                            {
                                try
                                {
                                    Util.MaintenanceLog(string.Empty, "Try to start OCR engine :" + serviceController.Status.ToString(), EventLogEntryType.Error);
                                    //mailMessage = "Trying to restart OCR engine.\nPlease contact the admin to check the DWMS OCR Service if it's still working. Thanks";
                                    //Util.SendMail(parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), recipientEmail, string.Empty, string.Empty, subject, mailMessage);
                                    TimeSpan timeout = TimeSpan.FromMinutes(15);
                                    serviceController.Start();
                                    serviceController.WaitForStatus(ServiceControllerStatus.Running, timeout);
                                    Util.MaintenanceLog(string.Empty, "OCR engine" + serviceController.Status.ToString(), EventLogEntryType.Information);
                                }
                                catch { Util.MaintenanceLog(string.Empty, "Failed to start OCR engine" + serviceController.Status.ToString(), EventLogEntryType.Error); }
                            }
                            //}
                        }
                        catch
                        {
                            Util.MaintenanceLog("DWMS_Maintenance.CheckOcrLastWorking.SendMail", "Catch: OCR Last Working Notification Mail Sending Is Failed", EventLogEntryType.Error);
                            //ServiceController serviceController = new ServiceController("DWMS_OCR_Service_New");
                            if (serviceController.Status.ToString() == "Running")
                            {
                                //serviceController.Dispose();
                                Util.MaintenanceLog(string.Empty, "Catch: Try to stop OCR engine" + serviceController.Status.ToString(), EventLogEntryType.Error);
                                //mailMessage = "Trying to stop OCR engine.\nPlease contact the admin to check the DWMS OCR Service if it's still working. Thanks";
                                //Util.SendMail(parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), recipientEmail, string.Empty, string.Empty, subject, mailMessage);
                                try
                                {
                                    System.Diagnostics.Process.Start(ConfigurationManager.AppSettings["SerivceRestart"].Trim());
                                    //TimeSpan timeout = TimeSpan.FromMinutes(5);
                                    //serviceController.Stop();
                                    //serviceController.WaitForStatus(ServiceControllerStatus.Running, timeout);
                                    //Util.MaintenanceLog(string.Empty, "OCR engine" + serviceController.Status.ToString(), EventLogEntryType.Information);
                                    //serviceController.Close();
                                }
                                catch { Util.MaintenanceLog(string.Empty, "Catch: Failed to stop OCR engine" + serviceController.Status.ToString(), EventLogEntryType.Error); }
                            }
                            else if (serviceController.Status.ToString() == "Stopped")
                            {
                                //ServiceController serviceController = new ServiceController("DWMS_OCR_Service_New");
                                Util.MaintenanceLog(string.Empty, serviceController.Status.ToString(), EventLogEntryType.Error);
                                //mailMessage = "Trying to restart OCR engine.\nPlease contact the admin to check the DWMS OCR Service if it's still working. Thanks";
                                //Util.SendMail(parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(), recipientEmail, string.Empty, string.Empty, subject, mailMessage);
                                TimeSpan timeout = TimeSpan.FromMinutes(15);
                                serviceController.Start();
                                serviceController.WaitForStatus(ServiceControllerStatus.Running, timeout);
                                Util.MaintenanceLog(string.Empty, "Catch: OCR engine" + serviceController.Status.ToString(), EventLogEntryType.Information);
                            }
                        }
                    }
                    // if the difference is more than time allowed but email has been sent more than maximum times, just put some warning in the log
                    else
                    {
                        string logMessage = "OcrLastWorking was updated " + Convert.ToInt16(diff.TotalMinutes) + " minutes ago. DWMS OCR Service is " + serviceController.Status.ToString();
                        Util.MaintenanceLog(string.Empty, logMessage, EventLogEntryType.Warning);
                    }
                }
                // if the difference is less than or equal to time allowed, reset the notification email count to 0 
                else
                    notificationMailSent = 0;
                //Util.MaintenanceLog(string.Empty, ocrLastWorking.ToString() + " is compared with " + currentTime.ToString() + ", so diff in minutes is " + Convert.ToInt16(diff.TotalMinutes), EventLogEntryType.Warning);
            }
            catch (Exception ex)
            {
                ServiceController serviceController = new ServiceController("DWMS_OCR_Service_New");
                Util.MaintenanceLog("DWMS_Maintenance.CheckOcrLastWorking", "OCR Last Working Check Is Error. " + ex, EventLogEntryType.Error);
            }
        }
        #endregion
    }
}
