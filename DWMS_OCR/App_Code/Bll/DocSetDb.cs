using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.DocSetTableAdapters;
using DWMS_OCR.App_Code.Dal;
using System.Data;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Bll
{
    class DocSetDb
    {
        private DocSetTableAdapter _DocSetTableAdapter = null;

        private vDocSetTableAdapter _vDocSetTableAdapter = null;

        protected DocSetTableAdapter Adapter
        {
            get
            {
                if (_DocSetTableAdapter == null)
                    _DocSetTableAdapter = new DocSetTableAdapter();

                return _DocSetTableAdapter;
            }
        }

        protected vDocSetTableAdapter vAdapter
        {
            get
            {
                if (_vDocSetTableAdapter == null)
                    _vDocSetTableAdapter = new vDocSetTableAdapter();

                return _vDocSetTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get all the sets.
        /// </summary>
        /// <returns>Set table</returns>
        public DocSet.DocSetDataTable GetDocSets()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="docAppId"></param>
        /// <returns></returns>
        public Boolean IsNotAllDocSetsSentToCDB(int docAppId, SendToCDBStatusEnum sendToCDB)
        {
            DocSet.DocSetDataTable docSet = Adapter.GetAllDocSetsNotSentToCDB(sendToCDB.ToString(), docAppId);
            return docSet.Rows.Count > 0;
        }

        /// <summary>
        /// Get the set by id.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>Set table</returns>
        public DocSet.DocSetDataTable GetDocSetById(int id)
        {
            return Adapter.GetDataById(id);
        }

        /// <summary>
        /// Retrieve the documents by docSetId
        /// </summary>
        /// <returns></returns>
        public DocSet.DocSetDataTable GetDataByDocId(int DocId)
        {
            return Adapter.GetDataByDocId(DocId);
        }

        /// <summary>
        /// Get the set by status.
        /// </summary>
        /// <param name="status">Set status</param>
        /// <returns>Set table</returns>
        public DocSet.DocSetDataTable GetDocSetByStatus(string status)
        {
            return Adapter.GetDataByStatus(status);
        }


        public DataTable GetVerifiedReadyDocSets(SendToCDBStatusEnum status)//, string exclusion)
        {
            //return Adapter.GetDataBySentToCDBStatus(status.ToString(), SetStatusEnum.Verified.ToString());
            return DocSetDs.GetVerifiedReadyDocSets(status.ToString(), SetStatusEnum.Verified.ToString());//, exclusion);
        }

        public DataTable GetVerifiedAppAndDocData(int docSetId, int IsVerified, DocStatusEnum status, DocStatusEnum status1, ImageConditionEnum imageCondition, DocTypeEnum docTypesToAvoid, SendToCDBStatusEnum sendToCDBStatusToAvoid)
        {
            return DocSetDs.GetDocAppAndDocData(docSetId, IsVerified, status.ToString(), status1.ToString(), imageCondition.ToString(), docTypesToAvoid.ToString(), sendToCDBStatusToAvoid.ToString());
        }



        public string GetUserNameByVerificationStaffId(Guid id)
        {
            return Adapter.GetUserNameByVerificationStaffId(id);
        }


        public DataTable GetDocSetData(int docAppId, CompletenessStatusEnum status, SendToCDBStatusEnum sendToCDBStatus)
        {
            return DocSetDs.GetDocSetData(docAppId, status.ToString(), sendToCDBStatus.ToString());
        }



        /// <summary>
        /// Get the status of the set.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>Status of the set</returns>
        public string GetDocSetStatus(int id)
        {
            string status = string.Empty;

            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                status = dr.Status;
            }

            return status;
        }

        /// <summary>
        /// Get the status of the set.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>Status of the set</returns>
        public Guid GetUserIdByDocId(int id)
        {
            Guid verificationStaffUserId = Guid.Empty;

            DocSet.DocSetDataTable dt = GetDataByDocId(id);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                verificationStaffUserId = dr.VerificationStaffUserId;
            }

            return verificationStaffUserId;
        }

        public string GetSetNumber(int id)
        {
            string result = string.Empty;

            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                result = dr.SetNo;
            }

            return result;
        }

        /// <summary>
        /// Get the DocApp id of the set.
        /// </summary>
        /// <param name="docSetId">Set id</param>
        /// <returns>DocApp id</returns>
        public int GetDocAppId(int docSetId)
        {
            int? id = null;

            SetAppDb setAppDb = new SetAppDb();
            SetApp.SetAppDataTable setAppTable = setAppDb.GetSetAppByDocSetId(docSetId);

            if (setAppTable.Rows.Count > 0)
            {
                SetApp.SetAppRow setApp = setAppTable[0];

                id = setApp.DocAppId;
            }

            return (id.HasValue ? id.Value : -1);
        }

        /// <summary>
        /// Get all the sets by status.
        /// </summary>
        /// <param name="status">Set status</param>
        /// <param name="isConvertedToSampleDoc">True to retrieve sets that have been converted to SampleDoc.  False if otherwise.</param>
        /// <returns></returns>
        public DocSet.DocSetDataTable GetDocSetByStatusConvertedToSampleDoc(string status, bool isConvertedToSampleDoc)
        {
            return Adapter.GetDataByStatusConvertedToSampleDoc(status, isConvertedToSampleDoc);
        }

        /// <summary>
        /// Get the id of the last set.
        /// </summary>
        /// <returns>Set id</returns>
        public int GetLastIdNo()
        {
            int? id = Adapter.GetLastIdNo();
            return (id.HasValue ? id.Value : -1);
        }

        /// <summary>
        /// Get the next id for the set.
        /// </summary>
        /// <returns>Next set id</returns>
        public int GetNextIdNo()
        {
            int id = GetLastIdNo();

            return (id == -1 ? 1 : id);
        }

        /// <summary>
        /// Get the lastest DocAppId for the NRIC
        /// </summary>
        /// <param name="nric">Nric</param>
        /// <returns>DocApp id</returns>
        public int GetLatestDocAppIdForNric(string nric)
        {
            int result = -1;

            DataTable dt = DocSetDs.GetLatestSetForNric(nric);

            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                result = int.Parse(dr["DocAppId"].ToString());
            }

            return result;
        }


        public DataTable GetDocSetsByDocAppId(int docAppId)
        {
            DataTable dt = DocSetDs.GetDocSetsByDocAppId(docAppId);
            if (dt.Rows.Count > 0)
            {
                return dt;
            }
            return null;
        }


        /// <summary>
        /// Get the value of the ReadyForOcr flag for the set.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>ReadyForOcr flag</returns>
        public bool GetReadyForOcrFlag(int id)
        {
            bool result = false;

            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                result = dr.ReadyForOcr;
            }

            return result;
        }        

        /// <summary>
        /// Get the id of first set to be processed for OCR.
        /// </summary>
        /// <returns>Set id</returns>
        public int GetTopOneDocSetIdForOcrProcess()
        {
            int result = -1;

            // Retrieve the sets with Urgency set to true as the high priority sets.
            // Followed by those manually uploaded by the users. And Lastly those imported
            // from external systems.
            DocSet.DocSetDataTable dt = GetTopOneDocSetForOcrProcessWithUrgency();  //GetTopOneDocSetForOcrProcessFromManualUploadWithUrgency();

            if (dt.Rows.Count <= 0)
                dt = GetTopOneDataForOcrProcessByTime();

            if (dt.Rows.Count <= 0)
                dt = GetTopOneDocSetForOcrProcessFromManualUpload();

            if (dt.Rows.Count <= 0)
                dt = GetTopOneDocSetForOcrProcess();

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                result = dr.Id;
            }

            return result;
        }

        /// <summary>
        /// Get the first set to be processed for OCR.
        /// </summary>
        /// <returns>DocSet table</returns>
        public DocSet.DocSetDataTable GetTopOneDocSetForOcrProcess()
        {
            return Adapter.GetTopOneDataForOcrProcess();
        }

        /// <summary>
        /// Get the first set with Urgency to be processed by OCR.
        /// </summary>
        /// <returns>DocSet table</returns>
        public DocSet.DocSetDataTable GetTopOneDocSetForOcrProcessWithUrgency()
        {
            return Adapter.GetTopOneDataForOcrProcessWithUrgency();
        }

        /// <summary>
        /// Get the first set from external systems to be processed by OCR.
        /// </summary>
        /// <returns>DocSet table</returns>
        public DocSet.DocSetDataTable GetTopOneDocSetForOcrProcessFromManualUpload()
        {
            Guid importedBy = Guid.NewGuid();
            ProfileDb profileDb = new ProfileDb();
            Guid? systemGuid = profileDb.GetSystemGuid();
            if (systemGuid.HasValue)
            {
                importedBy = systemGuid.Value;
                return Adapter.GetTopOneDataForOcrProcessFromManualUpload(importedBy);
            }
            else
            {
                return new DocSet.DocSetDataTable();
            }
        }

        /// <summary>
        /// Get the first set from external systems with Urgency to be processed by OCR.
        /// </summary>
        /// <returns>DocSet id</returns>
        public DocSet.DocSetDataTable GetTopOneDataForOcrProcessByTime()
        {
            //Guid importedBy = Guid.NewGuid();
            //ProfileDb profileDb = new ProfileDb();
            //Guid? systemGuid = profileDb.GetSystemGuid();
            //if (systemGuid.HasValue)
            //{
            //    importedBy = systemGuid.Value;
                return Adapter.GetTopOneDataForOcrProcessByTime();
            //}
            //else
            //{
            //    return new DocSet.DocSetDataTable();
            //}
        }

        /// <summary>
        /// Check if the set has a Verfication Officer assigned.
        /// </summary>
        /// <param name="docSetId">Set id</param>
        /// <returns>True if a Verification Officer is assigned.  False if otherwise</returns>
        public bool HasVerificationOfficerAssigned(int docSetId)
        {
            bool result = false;

            DocSet.DocSetDataTable dt = GetDocSetById(docSetId);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                result = !dr.IsVerificationStaffUserIdNull();
            }

            return result;
        }


        /// <summary>
        /// Check if the set is verified already
        /// </summary>
        /// <param name="docSetId">Set id</param>
        /// <returns>True if a the set is verified.  False if otherwise</returns>
        public bool IsSetVerified(int docSetId)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(docSetId);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                return (string.Equals(dr.Status.Trim().ToUpper(), SetStatusEnum.Verified.ToString().Trim().ToUpper()));
            }

            return false;
        }



        /// <summary>
        /// Check if the categorization of the set is to be skipped.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>True if the categorization of the set is skipped.  False if otherwise</returns>
        public bool ToSkipCategorization(int id)
        {
            bool result = false;

            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                result = dr.SkipCategorization;
            }

            return result;
        }
        
        /// <summary>
        /// Check if the set from Web Service is set to skip categorization.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>True if the categorization of the set is skipped.  False if otherwise</returns>
        public bool ToSkipCategorizationFromWebService(int id)
        {
            bool result = false;

            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];

                Boolean hasWebServXmlContent = false;

                if (!dr.IsWebServXmlContentNull())
                    hasWebServXmlContent = !String.IsNullOrEmpty(dr.WebServXmlContent);

                result = dr.SkipCategorization && hasWebServXmlContent;
            }

            return result;
        }

        /// <summary>
        /// Get the set from the view by id.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>Set table</returns>
        public DocSet.vDocSetDataTable GetvDocSetById(int id)
        {
            return vAdapter.GetvDataById(id);
        }

        /// <summary>
        /// Get the XML contents of the Web Service XML file
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>Web Service XML content</returns>
        public string GetWebServiceXmlContents(int id)
        {
            string xmlContents = string.Empty;

            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count > 0)
            {
                DocSet.DocSetRow dr = dt[0];
                xmlContents = dr.WebServXmlContent;
            }

            return xmlContents;
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert set record.
        /// </summary>
        /// <param name="setNo">Set number</param>
        /// <param name="dateInFrom">Date in from</param>
        /// <param name="status">Status</param>
        /// <param name="block">Block</param>
        /// <param name="streetId">Street id</param>
        /// <param name="level">Level</param>
        /// <param name="unit">Unit</param>
        /// <param name="channel">Channel</param>
        /// <param name="importedBy">Imported by</param>
        /// <param name="verificationStaff">Verification officer</param>
        /// <param name="departmentId">Department id</param>
        /// <param name="sectionId">Section id</param>
        /// <param name="docAppId">DocApp id</param>
        /// <param name="acknowledgementNo">Acknowledgement number</param>
        /// <returns>Set id</returns>
        public int Insert(string setNo, DateTime dateInFrom, SetStatusEnum status, string block, int streetId, string level,
            string unit, string channel, Guid importedBy, Guid? verificationStaff, int departmentId, int sectionId, 
            int docAppId, string acknowledgementNo, string webServiceXmlContent, bool webServiceHasDocId)
        {
            DocSet.DocSetDataTable dt = new DocSet.DocSetDataTable();
            DocSet.DocSetRow r = dt.NewDocSetRow();

            r.SetNo = setNo;
            r.VerificationDateIn = dateInFrom;
            r.Status = status.ToString();
            r.Channel = channel;

            if (!String.IsNullOrEmpty(block))
                r.Block = block;

            if (streetId > -1)
                r.StreetId = streetId;

            if (!String.IsNullOrEmpty(level))
                r.Floor = level;

            if (!String.IsNullOrEmpty(unit))
                r.Unit = unit;

            if (verificationStaff.HasValue)
                r.VerificationStaffUserId = verificationStaff.Value;

            r.ImportedBy = importedBy;
            r.ImportedOn = dateInFrom;
            r.DepartmentId = departmentId;
            r.SectionId = sectionId;
            r.AcknowledgeNumber = acknowledgementNo.Substring(0, (acknowledgementNo.Length >= 50 ? 50 : acknowledgementNo.Length));
            r.ProcessingStartDate = DateTime.Now;
            r.ProcessingEndDate = DateTime.Now;
            r.WebServXmlContent = webServiceXmlContent;
            r.SendToCDBStatus = SendToCDBStatusEnum.NotReady.ToString();
            r.SendToCDBAttemptCount = 0;

            r.SkipCategorization = webServiceHasDocId;

            dt.AddDocSetRow(r);
            Adapter.Update(dt);
            int id = r.Id;

            if (id > 0)
            {
                // Update the set number to reflect the id of the record
                UpdateSetNumber(id);

                if (docAppId != -1 && docAppId != 0)
                {
                    try
                    {
                        // Create SetApp records
                        SetAppDb setAppDb = new SetAppDb();
                        int setAppId = setAppDb.Insert(id, docAppId);
                    }
                    catch (Exception)
                    {
                        // Delete id for failed inserts
                        Delete(id);
                    }
                }

                AuditTrailDb auditTrailDb = new AuditTrailDb();
                auditTrailDb.Record(TableNameEnum.DocSet, id.ToString(), OperationTypeEnum.Insert);
            }

            return id;
        }
        #endregion

        #region Update Methods
        /// <summary>
        /// Update the status of the set.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <param name="status">Status</param>
        /// <returns>True if update was successful.  False if otherwise</returns>
        public bool UpdateSetStatus(int id, SetStatusEnum status)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count == 0) return false;

            DocSet.DocSetRow r = dt[0];

            r.Status = status.ToString();

            if (status == SetStatusEnum.New)
                r.ProcessingEndDate = DateTime.Now;
            /////temp to close sets
            //if (status == SetStatusEnum.Verified)
            //    r.VerificationDateOut = DateTime.Now;

            int rowsAffected = Adapter.Update(dt);            

            return (rowsAffected > 0);
        }

        //Added By Edward 06.11.2013 Confirm All Acceptance
        public void SendMail(int setId)
        {            
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
                    //string refType = Util.GetReferenceType(refNo);
                    //    switch (refType.ToUpper().Trim())
                    //    {
                    //        case "HLE":
                    string hleStatus = HleInterfaceDb.GetHleStatusByRefNo(refNo);
                    hleStatus = String.IsNullOrEmpty(hleStatus) ? "N/A" : hleStatus;

                    peOIC = docAppRow.IsPeOICNull() ? string.Empty : docAppRow.PeOIC.Trim(); // get PeOIC (null or not)
                    caOIC = docAppRow.IsCaseOICNull() ? string.Empty : docAppRow.CaseOIC.Trim(); // get CaseOIC (null or not)

                    string OIC = string.Empty;

                    if (!string.IsNullOrEmpty(peOIC))
                        OIC = peOIC;
                    else if (!string.IsNullOrEmpty(caOIC))
                        OIC = caOIC;

                    bool OICfoundInUserList = ProfileDb.GetCountByEmailSetId(OIC, setId);       //Added By Edward 12/02/2014 Dont Send Email Outside

                    if (OICfoundInUserList)     //Added Condition by Edward 12/02/2014 Dont Send Email Outside
                    {
                        if (!String.IsNullOrEmpty(OIC.Trim()) && OIC.Trim() != "-" && OIC.Trim() != "COS" && OIC.Trim() != "cos")
                        {
                            recipientEmail = OIC.Trim() + "@" + Retrieve.GetEmailDomain();

                            subject = "(New) Documents (Set No. " + setNumber + ") for " + docAppRow.RefType.Trim() + " " + refNo + " (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                            //message = subject + "<br><br>You may view the image in DWMS using the link <a href='" + Request.Url.AbsoluteUri + "' target=_blank>here</a> and review the case, if necessary.";
                            message = "(New) Documents (Set No. <a href='" + Retrieve.GetDWMSDomain() + "Verification/View.aspx?id=" + setId + "' target='_blank'>" + setNumber + "</a>) for " + docAppRow.RefType.Trim() + " <a href='" + Retrieve.GetDWMSDomain() + "Completeness/View.aspx?id=" + docAppRow.Id.ToString() + "' target=_blank>" + refNo + "</a> (Status : " + hleStatus.ToString().Replace('_', ' ') + ") have been received";
                            message = message + "<br><br>You may view the image in DWMS using the link above and review the case, if necessary.";
                            if (!String.IsNullOrEmpty(recipientEmail.Trim()))
                            {
                                ParameterDb parameterDb = new ParameterDb();

                                bool emailSent = Util.SendMail(parameterDb.GetParameterValue(ParameterNameEnum.SystemName).Trim(), parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail).Trim(),
                                    recipientEmail, ccEmail, string.Empty, string.Empty, subject, message, pdfPath);
                            }
                        }
                    }


                }
            }
        }


        public bool UpdateSetSentToCDBStatus(int id, SendToCDBStatusEnum status, int count)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count == 0) return false;

            DocSet.DocSetRow r = dt[0];

            r.SendToCDBStatus = status.ToString();
            r.SendToCDBAttemptCount = count;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }


        public bool UpdateSetSentToCDBStatus(int id, SendToCDBStatusEnum status)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count == 0) return false;

            DocSet.DocSetRow r = dt[0];

            r.SendToCDBStatus = status.ToString();
           
            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }



        //2012-12-12
        /// <summary>
        /// Update Set Status
        /// </summary>
        /// <param name="id"></param>
        /// <param name="setStatus"></param>
        /// <returns></returns>
        public bool UpdateSetStatus(int id, Guid verificationOIC, SetStatusEnum setStatus, Boolean isLogAction, Boolean isUserSectionChange, LogActionEnum logAction)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Count == 0) return false;

            DocSet.DocSetRow dr = dt[0];

            dr.Status = setStatus.ToString();

            dr.VerificationStaffUserId = verificationOIC;


            if (setStatus == SetStatusEnum.Pending_Categorization)
                dr.ReadyForOcr = true;

            if (setStatus.Equals(SetStatusEnum.Verified))
                dr.VerificationDateOut = DateTime.Now;


            int rowsAffected = Adapter.Update(dt);

            if (rowsAffected > 0)
            {
                AuditTrailDb auditTrailDb = new AuditTrailDb();
                auditTrailDb.Record(TableNameEnum.DocSet, id.ToString(), OperationTypeEnum.Update);

                if (isLogAction)
                {
                    
                    ProfileDb profileDb = new ProfileDb();

                    

                    LogActionDb logActionDb = new LogActionDb();
                    string userName = profileDb.GetUserNameByUserId(verificationOIC);

                    logActionDb.Insert(verificationOIC, logAction.ToString(), userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, id);
                }
            }

            return rowsAffected == 1;
        }


        /// <summary>
        /// Get Earliest VerificationDateIn By DocAppId 
        /// </summary>
        /// <param name="docAppId"></param>
        /// <returns></returns>
        public DateTime GetEarliestVerificationDateInByDocAppId(int docAppId)
        {
            DateTime? verificationDateIn = Adapter.GetEarliestVerificationDateInByDocAppId(docAppId);

            return (verificationDateIn.HasValue ? verificationDateIn.Value : DateTime.Now);
        }

        /// <summary>
        /// Add remark for set.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <param name="message">Remark value</param>
        /// <returns>True if update was successful.  False if otherwise</returns>
        public bool AddRemark(int id, string message)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count == 0) return false;

            DocSet.DocSetRow r = dt[0];

            if (r.IsRemarkNull())
                r.Remark = message;
            else if (r.Remark.Length > 0)
                r.Remark = r.Remark + "\r\n" + message;
            else
                r.Remark = message;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }


        /// <summary>
        /// Update the ConvertedToSampleDoc flag.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <param name="flag">ConvertedToSampleDoc value</param>
        /// <returns>True if update was successful.  False if otherwise</returns>
        public bool UpdateCopyToSampleDocFlag(int id, bool flag)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count == 0) return false;

            DocSet.DocSetRow r = dt[0];

            r.ConvertedToSampleDoc = flag;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update the set number.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>True if update was successful.  False if otherwise</returns>
        public bool UpdateSetNumber(int id)
        {
            DocSet.DocSetDataTable dt = Adapter.GetDataById(id);

            if (dt.Count == 0)
                return false;

            DocSet.DocSetRow dr = dt[0];

            int temp1 = -1;
            int temp2 = -1;
            dr.SetNo = Util.FormulateSetNumber(id, string.Empty, string.Empty, out temp1, out temp2);

            int rowsAffected = Adapter.Update(dt);
            return rowsAffected == 1;
        }

        /// <summary>
        /// Update the ReadyForOcr flag.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <param name="readyForOcr">ReadyForOcr value</param>
        /// <returns>True if update was successful.  False if otherwise</returns>
        public bool SetReadyForOcr(int id, bool readyForOcr)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count == 0) return false;

            DocSet.DocSetRow r = dt[0];

            r.ReadyForOcr = readyForOcr;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }

        /// <summary>
        /// Update the processing dates of the set.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <param name="isStart">If true, update ProcessingStartDate.  If false, update ProcessingEndDate</param>
        /// <returns>True if update was successful.  False if otherwise</returns>
        public bool SetIsBeingProcessed(int id, bool isStart)
        {
            DocSet.DocSetDataTable dt = GetDocSetById(id);

            if (dt.Rows.Count == 0) return false;

            DocSet.DocSetRow r = dt[0];

            r.IsBeingProcessed = isStart;
            if (isStart)
                r.ProcessingStartDate = DateTime.Now;
            else
                r.ProcessingEndDate = DateTime.Now;

            int rowsAffected = Adapter.Update(dt);

            return (rowsAffected > 0);
        }
        #endregion

        #region Delete Methods
        /// <summary>
        /// Delete the set by id.
        /// </summary>
        /// <param name="id">Set id</param>
        /// <returns>True if delete was successful.  False if otherwise</returns>
        public bool Delete(int id)
        {
            int recordsEffected = 0;
            recordsEffected = Adapter.Delete(id);

            if (recordsEffected > 0)
            {
                AuditTrailDb auditTrailDb = new AuditTrailDb();
                auditTrailDb.Record(TableNameEnum.DocSet, id.ToString(), OperationTypeEnum.Delete);
            }

            return recordsEffected > 0;
        }
        #endregion


    }
}
