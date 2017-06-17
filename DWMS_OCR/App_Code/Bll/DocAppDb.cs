using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.DocAppTableAdapters;
using DWMS_OCR.App_Code.Dal;
using System.Data;

namespace DWMS_OCR.App_Code.Bll
{
    class DocAppDb
    {
        private DocAppTableAdapter _DocAppTableAdapter = null;

        protected DocAppTableAdapter Adapter
        {
            get
            {
                if (_DocAppTableAdapter == null)
                    _DocAppTableAdapter = new DocAppTableAdapter();

                return _DocAppTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public DocApp.DocAppDataTable GetDocApps()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document set by id
        /// </summary>
        /// <returns></returns>
        public DocApp.DocAppDataTable GetDocAppById(int id)
        {
            return Adapter.GetDataById(id);
        }


        public int GetAttemptCountSentToCDB(int docAppId)
        {
            int? count = (int?) Adapter.GetSentToCDBAttemptCount(docAppId);
            return count.HasValue ? count.Value : 0;
        }

        public DataTable GetDocAppsReadyToSendToCDB(SendToCDBStatusEnum status)//, string exclusion)
        {
            return DocAppDs.GetDocAppsReadyToSendToCDB(status.ToString());//, exclusion);
        }


        public string GetUserNameByCompletenessStaffUserId(Guid id)
        {
            return Adapter.GetUserNameByCompletenessStaffUserId(id);
        }


        /// <summary>
        /// Update DateIn
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dateIn"></param>
        /// <returns></returns>
        public bool UpdateDateIn(int id, DateTime dateIn)
        {
            DocApp.DocAppDataTable dt = GetDocAppById(id);

            if (dt.Count == 0)
                return false;

            DocApp.DocAppRow dr = dt[0];

            dr.DateIn = dateIn;

            int rowsAffected = Adapter.Update(dt);

            if (rowsAffected > 0)
            {
                AuditTrailDb auditTrailDb = new AuditTrailDb();
                auditTrailDb.Record(TableNameEnum.DocApp, id.ToString(), OperationTypeEnum.Update);
            }

            return rowsAffected == 1;
        }



        public bool UpdateSentToCDBStatus(int id, SendToCDBStatusEnum status, int attempCount)
        {
            DocApp.DocAppDataTable dt = GetDocAppById(id);

            if (dt.Count == 0)
                return false;

            DocApp.DocAppRow dr = dt[0];

            //Even if the status is set to Sent, still requires to update the attemptCount
            // ie. Attempt 0, and Ready, means has not still sent 
            // ie. Attempt 1, and Ready, means has sent but has been unsuccessful,

            dr.SendToCDBStatus = status.ToString();
            dr.SendToCDBAttemptCount = attempCount;

            int rowsAffected = Adapter.Update(dt);

            if (rowsAffected > 0)
            {
                AuditTrailDb auditTrailDb = new AuditTrailDb();
                auditTrailDb.Record(TableNameEnum.DocApp, id.ToString(), OperationTypeEnum.Update);
            }

            return rowsAffected == 1;
        }

       





        /// <summary>
        /// Get DocApp by DocSetId 
        /// </summary>
        /// <param name="docSetId"></param>
        /// <returns></returns>
        public DocApp.DocAppDataTable GetDocAppByDocSetId(int docSetId)
        {
            return Adapter.GetDocAppByDocSetId(docSetId);
        }



        /// <summary>
        /// Update Ref Status
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <param name="isLogAction"></param>
        /// <returns></returns>
        public bool UpdateRefStatus(int id, Guid verificationOIC, AppStatusEnum status, Boolean isLogAction, Boolean isUserSectionChange, LogActionEnum? logAction)
        {
            DocApp.DocAppDataTable dt = GetDocAppById(id);

            if (dt.Count == 0)
                return false;

            DocApp.DocAppRow dr = dt[0];

            dr.Status = status.ToString();

            if (status.Equals(AppStatusEnum.Completeness_Checked))
                dr.DateOut = DateTime.Now;

            int rowsAffected = Adapter.Update(dt);

            if (rowsAffected > 0)
            {
                AuditTrailDb auditTrailDb = new AuditTrailDb();
                auditTrailDb.Record(TableNameEnum.DocApp, id.ToString(), OperationTypeEnum.Update);

                if (isLogAction && logAction != null)
                {
                    //if (isUserSectionChange)
                    //{
                    //    ProfileDb profileDb = new ProfileDb();
                    //    DocAppDb docAppDb = new DocAppDb();
                    //    DocApp.DocAppDataTable docApp = docAppDb.GetDocAppById(id);
                    //    DocApp.DocAppRow docAppRow = docApp[0];
                    //    if (!docAppRow.IsCompletenessStaffUserIdNull())
                    //        username = profileDb.GetUserFullName(docAppRow.CompletenessStaffUserId);
                    //}

                    ProfileDb profileDb = new ProfileDb();
                    string userName = profileDb.GetUserNameByUserId(verificationOIC);
                    LogActionDb logActionDb = new LogActionDb();

                    logActionDb.Insert(verificationOIC, logAction.Value.ToString(), userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.A, id);
                }
            }

            return rowsAffected == 1;
        }

        public bool UpdateSetSentToCDBStatus(int id, SendToCDBStatusEnum status, int count)
        {
            DocApp.DocAppDataTable dt = GetDocAppById(id);

            if (dt.Rows.Count == 0) return false;

            DocApp.DocAppRow r = dt[0];

            r.SendToCDBStatus = status.ToString();
            r.SendToCDBAttemptCount = count;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }


        public bool UpdateSetSentToCDBStatus(int id, SendToCDBStatusEnum status)
        {
            DocApp.DocAppDataTable dt = GetDocAppById(id);

            if (dt.Rows.Count == 0) return false;

            DocApp.DocAppRow r = dt[0];

            r.SendToCDBStatus = status.ToString();

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }



        /// <summary>
        /// Get the document set by id
        /// </summary>
        /// <returns></returns>
        public string GetDocAppRefNoById(int id)
        {
            string refNo = string.Empty;

            DocApp.DocAppDataTable dt = Adapter.GetDataById(id);

            if (dt.Rows.Count > 0)
            {
                DocApp.DocAppRow dr = dt[0];
                refNo = dr.RefNo;
            }

            return refNo;
        }

        /// <summary>
        /// Get the document app by ref no
        /// </summary>
        /// <returns></returns>
        public DocApp.DocAppDataTable GetDocAppByRefNo(string refNo)
        {
            return Adapter.GetDataByRefNo(refNo);
        }

        /// <summary>
        /// Check if the reference number exists
        /// </summary>
        /// <param name="refNo"></param>
        /// <returns></returns>
        public bool DoesReferenceExists(string refNo)
        {
            return GetDocAppByRefNo(refNo).Rows.Count > 0;
        }

        /// <summary>
        /// Get Id by reference number
        /// </summary>
        /// <param name="refNo"></param>
        /// <returns></returns>
        public int GetIdByRefNo(string refNo)
        {
            int result = -1;

            DocApp.DocAppDataTable dt = GetDocAppByRefNo(refNo);

            if (dt.Rows.Count > 0)
            {
                DocApp.DocAppRow dr = dt[0];
                result = dr.Id;
            }

            return result;
        }

        /// <summary>
        /// Get app details for Scanning/Uploading
        /// </summary>
        /// <param name="selectedDocAppId"></param>
        /// <param name="referenceNo"></param>
        /// <param name="referenceType"></param>
        /// <param name="newDocAppId"></param>
        /// <param name="caseOic"></param>
        public void GetAppDetails(int selectedDocAppId, string referenceNo, string referenceType, out int newDocAppId)
        {
            newDocAppId = 0;

            DocSetDb docSetDb = new DocSetDb();
            HleInterfaceDb hleInterfaceDb = new HleInterfaceDb();
            ResaleInterfaceDb resaleInterfaceDb = new ResaleInterfaceDb();

            if (selectedDocAppId == 0 && !string.IsNullOrEmpty(referenceNo))
            {
                DocApp.DocAppDataTable docAppDt = GetDocAppByRefNo(referenceNo);

                if (docAppDt.Rows.Count > 0)
                {
                    DocApp.DocAppRow docAppDr = docAppDt[0];

                    newDocAppId = docAppDr.Id;
                }
                else
                {
                    newDocAppId = Insert(referenceNo, referenceType, null, null, AppStatusEnum.Pending_Documents.ToString(), null);
                }
            }
            else
            {
                newDocAppId = selectedDocAppId;
            }
        }

        /// <summary>
        /// Get the Id and CaseOic of an application
        /// </summary>
        /// <param name="referenceNo"></param>
        /// <param name="referenceType"></param>
        /// <param name="docAppId"></param>
        /// <param name="caseOic"></param>
        public string GetCaseOic(string referenceNo)
        {
            string caseOic = string.Empty;

            DocApp.DocAppDataTable dt = GetDocAppByRefNo(referenceNo);

            if (dt.Rows.Count > 0)
            {
                DocApp.DocAppRow dr = dt[0];
                caseOic = (dr.IsCaseOICNull() ? string.Empty : dr.CaseOIC);
            }

            return caseOic;
        }

        //Added By Edward 22.11.2013 for Leas Service
        public DocApp.DocAppDataTable GetDocAppIncomeExtractionById(int id)
        {
            return Adapter.GetDocAppIncomeExtractionById(id);

        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert doc app
        /// </summary>
        /// <returns></returns>
        public int Insert(string refNo, string refType, DateTime? dateIn, DateTime? dateOut, string status, Guid? staffUserId)
        {
            DocApp.DocAppDataTable dt = new DocApp.DocAppDataTable();
            DocApp.DocAppRow r = dt.NewDocAppRow();

            r.RefNo = refNo.ToUpper();
            r.RefType = refType;

            if (dateIn.HasValue)
                r.DateIn = dateIn.Value;

            if (dateOut.HasValue)
                r.DateOut = dateOut.Value;

            r.Status = status;

            if (staffUserId.HasValue)
                r.CompletenessStaffUserId = staffUserId.Value;

            r.SendToCDBStatus = SendToCDBStatusEnum.NotReady.ToString();
            r.SendToCDBAttemptCount = 0;

            dt.AddDocAppRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }
        #endregion

        #region Update Methods
        /// <summary>
        /// Update the status of the set
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool UpdateSetStatus(int id, string status)
        {
            DocApp.DocAppDataTable dt = GetDocAppById(id);

            if (dt.Rows.Count == 0) return false;

            DocApp.DocAppRow r = dt[0];

            r.Status = status;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        public bool UpdateAppStatusOnNewSet(int id)
        {
            DocApp.DocAppDataTable dt = GetDocAppById(id);

            if (dt.Rows.Count == 0) return false;

            DocApp.DocAppRow r = dt[0];

            r.Status = AppStatusEnum.Completeness_In_Progress.ToString(); ;
            r.DownloadStatus = DownloadStatusEnum.Pending_Download.ToString();
            r.SetDownloadedByNull();
            r.SetDownloadedOnNull();

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }
        
        public bool UpdateSetApplicationStatus(int docSetId)
        {
            SetAppDb setAppDb = new SetAppDb();           

            SetApp.SetAppDataTable setAppDt = setAppDb.GetSetAppByDocSetId(docSetId);

            if (setAppDt.Rows.Count > 0)
            {
                foreach (SetApp.SetAppRow setAppDr in setAppDt.Rows)
                {
                    DocApp.DocAppDataTable appTable = GetDocAppById(setAppDr.DocAppId);

                    if (appTable.Rows.Count > 0)
                    {
                        DocApp.DocAppRow appDr = appTable[0];

                        // Update the status of the application.  If the current status is "Completeness Checked", 
                        // update it to "Completeness in Progress".  Besides that, update also the download status to
                        // "Pending Download", download on date to null and download by to null.
                        if (appDr.Status.Trim().ToUpper().Equals(AppStatusEnum.Completeness_Checked.ToString().ToUpper()))
                        {                            
                            UpdateAppStatusOnNewSet(appDr.Id);
                        }
                    }
                }
            }

            return true;
        }

        //Added By Edward 22.11.2013 Leas Service
        public bool UpdateSentToLeasStatus(int id, string SentToLeasStatus, int attempt)
        {
            DocApp.DocAppDataTable dt = GetDocAppById(id);

            if (dt.Rows.Count == 0) return false;

            DocApp.DocAppRow r = dt[0];

            r.SentToLEASStatus = SentToLeasStatus;
            r.SentToLeasAttemptCount = attempt;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }
        #endregion

        #region Delete Methods
        public bool Delete(int id)
        {
            //return Adapter.Delete(
            return false;
        }
        #endregion
    }
}
