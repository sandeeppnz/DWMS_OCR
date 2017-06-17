using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.AcceptDocWebRef;
using DWMS_OCR.App_Code.Bll;
using System.Data;
using DWMS_OCR.App_Code.Helper;
using System.Diagnostics;
using System.IO;

namespace DWMS_OCR.CdbService
{
    class CDBCompleteAccept : CDBAcceptUtil
    {
        public bool TriggerTest { get; set; }
        public bool TriggerUpdateResultToDatabase { get; set; }
        public bool TriggerSendToCDBCompleteAccept { get; set; }
        public bool XmlOutput { get; set; }
        public bool RunOnce { get; set; }

        public void SendAllDocsUponCompletenessChecked()
        {
            try
            {
                BE01JAcceptDocService webRef = new BE01JAcceptDocService();
                //DocSetDb docSetDb = new DocSetDb();
                DocAppDb docAppDb = new DocAppDb();
                bool logging = Util.Logging();
                bool detailLogging = Util.DetailLogging();

                using (DataTable dt = docAppDb.GetDocAppsReadyToSendToCDB(SendToCDBStatusEnum.Ready))//, "SYSTEM"))
                {
                    // #Util.CDBLog(string.Empty, String.Format("ACCEPT:  Found {0} DocApps(s) with SentToCDBStaus: " + SendToCDBStatusEnum.Ready.ToString() + " and DocAppStatus=" + CompletenessStatusEnum.Completeness_Checked.ToString(), dt.Rows.Count), EventLogEntryType.Information);
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow docAppRow in dt.Rows)
                        {
                            if (((int)docAppRow["SendToCDBAttemptCount"] < CDBAcceptUtil.GetMaxAttemptToSendCompletedDocApps()) || docAppRow.IsNull("SendToCDBAttemptCount"))
                            {
                                bool hasSentToCDB = false;
                                int attemptCount = 0;
                                for (attemptCount = 0; attemptCount < CDBAcceptUtil.GetMaxAttemptToSendCompletedDocApps(); attemptCount++)
                                {
                                    hasSentToCDB = SendCompletedApp(webRef, docAppRow, attemptCount + 1);
                                    if (hasSentToCDB)
                                        break;
                                }
                                if (TriggerUpdateResultToDatabase)
                                {
                                    if (hasSentToCDB)
                                        docAppDb.UpdateSetSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.Sent, attemptCount);
                                    else
                                    {
                                        docAppDb.UpdateSetSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.SentButFailed, attemptCount);
                                    }
                                }
                            }
                            else
                            {
                                //if (logging) Util.CDBLog(string.Empty, String.Format("ACCEPT:  DocApp: {0} has exceeded the max. number of attempts to sent to CDB, therefore marked not to be processed", (int)docAppRow["Id"]), EventLogEntryType.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.SendCompletedApplications", errorMessage, EventLogEntryType.Error);
            }
        }

        private bool SendCompletedApp(BE01JAcceptDocService webRef, DataRow docAppRow, int attemptCount)
        {
            DocAppDb docAppDb = new DocAppDb();

            BE01JSystemInfoDTO systemInfo = new BE01JSystemInfoDTO();
            BE01JBusinessInfoDTO businessInfo = new BE01JBusinessInfoDTO();
            BE01JOutputDTO result = new BE01JOutputDTO();
            BE01JAuthenticationDTO authentication = new BE01JAuthenticationDTO();

            string filePath = string.Empty;
            string checkedFilePath = string.Empty;
            bool connError = false;
            bool hasDocAppSuccessfullySentToCDB = false;



            //if the docset failed (i.e. the data retrieval, input, connection, or output had any problem), will be marked as 'Ready' 
            bool hasInputInfo = false;
            //bool isAllDocsSetsSent = true;
            //bool isAllDocsStatusIsSent = true;

            
            hasInputInfo = GetDocs(docAppRow, ref systemInfo, ref authentication, ref businessInfo);//, ref isAllDocsSetsSent, ref isAllDocsStatusIsSent);

            //if (isAllDocsStatusIsSent)
            //{
                Util.CDBLog(string.Empty, String.Format("ACCEPT:  Processing: DocAppId({0})", docAppRow.Field<int>("Id")), EventLogEntryType.Information);

                filePath = GetFilePathAndName(businessInfo, attemptCount); //zero-based attempt count
                //string checkedFilePath = GetFilePathAndNameInputErrorCheck(hasInputInfo, filePath, isAllDocsSetsSent);
                //Util.CDBDetailLog(string.Empty, String.Format("ACCEPT:  Start writing input to file: " + filePath), EventLogEntryType.Information);
                //GenerateInputTextFile(systemInfo, businessInfo, authentication, filePath + "-inp.txt");
                //Util.CDBDetailLog(string.Empty, String.Format("ACCEPT:  End writing input to file"), EventLogEntryType.Information);


                #region send to CDB Completed List
                // 3. Send data to CDB 
                if (!hasInputInfo)
                {
                    checkedFilePath = filePath + ".InpErr";
                    //if (isAllDocsSetsSent)
                        Util.CDBLog(string.Empty, String.Format("ACCEPT:  Retrieval of data for DocAppId({0}) failed, all DocSets maynot be sent to CDB", docAppRow.Field<int>("Id")), EventLogEntryType.Error);
                    //else
                    //    Util.CDBLog(string.Empty, String.Format("ACCEPT:  All DocSets are not sent to CDB for the DocAppId: {0}", docAppRow.Field<int>("Id")), EventLogEntryType.Information);

                    hasDocAppSuccessfullySentToCDB = false;
                }
                else //if retrieval of data from DB is ok
                {
                    if (TriggerSendToCDBCompleteAccept)
                    {
                        Util.CDBDetailLog(string.Empty, String.Format("ACCEPT:  Start sending to CDB"), EventLogEntryType.Information);
                        try
                        {
                            for (int cnt = 0; cnt < 3; cnt++)
                            {
                                webRef.Timeout = 300000;//add timeout of 5min
                                result = webRef.acceptDocument(authentication, systemInfo, businessInfo);
                                if (result.ToString().Length > 0)
                                {
                                    connError = false;

                                    hasDocAppSuccessfullySentToCDB = true;

                                    Util.CDBDetailLog(string.Empty, String.Format("ACCEPT:  Successfully sent to CDB"), EventLogEntryType.Information);
                                    break;
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            connError = true;
                            hasDocAppSuccessfullySentToCDB = false;
                            Util.CDBLog(string.Empty, String.Format("ACCEPT:  Connection to CDB attempt failed, Message: " + ex.Message + ", StackTrace: " + ex.StackTrace), EventLogEntryType.Error);
                        }
                    }
                    else
                    {
                        connError = true; //connection is diabled
                        hasDocAppSuccessfullySentToCDB = false;
                        Util.CDBLog(string.Empty, String.Format("ACCEPT:  Calling to CDB currently turned off in this service"), EventLogEntryType.Warning);
                    }

                    #region write connection error file
                    if (connError)
                    {
                        Util.CDBDetailLog(string.Empty, String.Format("ACCEPT:  Start writing error to file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                        //bool resultError = WriteConnectionErrorFile(checkedFilePath);
                        checkedFilePath = filePath + ".ConnErr";
                        Util.CDBDetailLog(string.Empty, String.Format("ACCEPT:  End writing error to file"), EventLogEntryType.Information);
                    }
                    #endregion
                }
                #endregion


                #region check the output, create output/err file, update DB
                // 4. GenerateOutputTextFile
                //the output result is returned
                if (!connError && hasInputInfo && TriggerSendToCDBCompleteAccept)
                {
                    //Can be either an OutputError or Output file
                    //string filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                    if (ValidateCDBForDocAppCompletedAccept(result))
                        checkedFilePath = filePath;
                    else
                        checkedFilePath = filePath + ".OutErr";
                    Util.CDBDetailLog(string.Empty, String.Format("ACCEPT:  Start writing output result to file: " + checkedFilePath), EventLogEntryType.Information);
                    //bool resultWrite = GenerateOutputTextFile(filePathOut, checkedFilePath, result, businessInfo, systemInfo, authentication, SendToCDBStageEnum.Accept);
                    bool resultWrite = ProcessOutput(result, businessInfo, systemInfo, authentication, SendToCDBStageEnum.Accept);

                    if (hasDocAppSuccessfullySentToCDB)
                        hasDocAppSuccessfullySentToCDB = ValidateCDBForDocAppCompletedAccept(result);
                    Util.CDBDetailLog(string.Empty, String.Format("ACCEPT:  End writing output result to file sent from CDB"), EventLogEntryType.Information);
                }
                #endregion



                #region write output to xml
                if (XmlOutput && hasInputInfo)
                {
                    GenerateXmlOutput(checkedFilePath, result, businessInfo, systemInfo);
                }
                #endregion


                if (TriggerUpdateResultToDatabase)
                {
                    if (hasDocAppSuccessfullySentToCDB)
                    {
                        docAppDb.UpdateSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.Sent, attemptCount + 1);
                    }
                    else
                    {
                        docAppDb.UpdateSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.SentButFailed, attemptCount + 1);
                    }
                }
            //}
            return hasDocAppSuccessfullySentToCDB;

        }



        private string GetFilePathAndName(BE01JBusinessInfoDTO businessInfo, int attemptCount)
        {
            if (!TriggerTest)
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.CompleteAcceptDWMSToCDBLogFileName, businessInfo.businessRefNumber, Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss), attemptCount));
            else
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.CompleteAcceptDWMSToCDBLogFileName, "Xml_Input", Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss), attemptCount));

        }

        public void RunXmlTest()
        {
            Util.CDBLog(string.Empty, String.Format("ACCEPT:  Start RunXmlTest"), EventLogEntryType.Information);

            string inputFilePath = CDBAcceptUtil.GetTestInputXMLForDWMSToCDBFilePathAndNameCompleteAccept();
            BE01JSystemInfoDTO systemInfo = new BE01JSystemInfoDTO();
            BE01JAuthenticationDTO authentication = new BE01JAuthenticationDTO();
            BE01JAcceptDocService webRef = new BE01JAcceptDocService();
            bool errorAtImport = false;
            BE01JBusinessInfoDTO businessInfoTest = XmlInput(inputFilePath, ref systemInfo, ref authentication);
            string filePath = string.Empty;
            BE01JOutputDTO result = new BE01JOutputDTO();
            bool writeData = false;
            string checkedFilePath = string.Empty;
            if (businessInfoTest != null)
            {
                filePath = GetFilePathAndName(businessInfoTest, 1);
                checkedFilePath = GetFilePathAndNameInputErrorCheck(true, filePath, true);
                Util.CDBLog(string.Empty, String.Format("ACCEPT:  Start writing input file: " + checkedFilePath), EventLogEntryType.Information);
                writeData = GenerateInputTextFile(systemInfo, businessInfoTest, authentication, checkedFilePath);
                Util.CDBLog(string.Empty, String.Format("ACCEPT:  End writing input file"), EventLogEntryType.Information);
            }
            else
            {
                errorAtImport = true;
            }
            bool errorAtAcceptDocument = false;
            if (!errorAtImport)
            {
                try
                {
                    if (TriggerSendToCDBCompleteAccept)
                    {
                        Util.CDBLog(string.Empty, String.Format("ACCEPT:  Start sending to CDB"), EventLogEntryType.Information);
                        //result = webRef.acceptDocument(authentication, systemInfo, businessInfoTest);
                        Util.CDBLog(string.Empty, String.Format("ACCEPT:  End sending to CDB"), EventLogEntryType.Information);
                    }
                    else
                    {
                        result = null;
                        Util.CDBLog(string.Empty, String.Format("ACCEPT:  Calling to CDB's VerifyDocument is currently configured to be OFF in this service"), EventLogEntryType.Warning);
                    }
                }
                catch
                {
                    errorAtAcceptDocument = true;
                    result = null;
                    Util.CDBLog(string.Empty, String.Format("ACCEPT:  Start writing file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                    bool resultError = WriteConnectionErrorFile(checkedFilePath);
                    Util.CDBLog(string.Empty, String.Format("ACCEPT:  End writing file"), EventLogEntryType.Information);
                    Util.CDBLog(string.Empty, String.Format("ACCEPT:  Failed to connect to CDB. No output file is produced and the Err file being generated"), EventLogEntryType.Error);
                }
            }
            else
            {
                Util.CDBLog(string.Empty, String.Format("ACCEPT:  Error at import data from text file, please correct the data. Log file(s) are not generated and no information has been sent to CDB"), EventLogEntryType.Error);
            }

            if (!errorAtAcceptDocument && !errorAtImport)
            {
                string filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                Util.CDBLog(string.Empty, String.Format("ACCEPT:  Start writing result to file: " + filePathOut), EventLogEntryType.Information);
                bool resultWrite = GenerateOutputTextFile(filePathOut, checkedFilePath, result, businessInfoTest, systemInfo, authentication, SendToCDBStageEnum.Accept);
                Util.CDBLog(string.Empty, String.Format("ACCEPT:  End writing result to file"), EventLogEntryType.Information);
            }
            if (XmlOutput)
            {
                GenerateXmlOutput(checkedFilePath, result, businessInfoTest, systemInfo);
            }

        }


        private bool GetDocs(DataRow docAppRow, ref BE01JSystemInfoDTO systemInfo, ref BE01JAuthenticationDTO authentication, ref BE01JBusinessInfoDTO businessInfo)//, ref bool isAllDocsSetsSent, ref bool isAllDocsStatusIsSent)
        {
            try
            {
                int docAppId = (int)docAppRow["Id"];
                DocSetDb docSetDb = new DocSetDb();
                DocDb docDb = new DocDb();
                DocAppDb docAppDb = new DocAppDb();

                systemInfo.fileSystemId = "DWMS";
                systemInfo.updateSystemId = "DWMS";
                systemInfo.fileDate = DateTime.Now;

                systemInfo.updateDate = DateTime.Today;
                systemInfo.updateTime = DateTime.Now;

                systemInfo.verificationUserId = string.Empty;

                bool hasDoc = false;
                int count;

                authentication.userName = CDBAcceptUtil.GetUserNameDWMSToCDB();
                authentication.passWord = CDBAcceptUtil.GetPasswordDWMSToCDB();

                if (docAppRow != null)
                {
                    businessInfo.businessRefNumber = docAppRow["RefNo"] as string;
                    businessInfo.businessTransactionNumber = docAppRow["RefType"] as string;
                }


                // TODO: Check if all docsets are sent to CDB
                // Check if all the docsets belong to this DocApp are Sent to CDB 
                // Proceed only if all the docSets are Sent to CDB
                //if (docSetDb.IsNotAllDocSetsSentToCDB(docAppId, SendToCDBStatusEnum.Sent))
                //{
                //    isAllDocsSetsSent = false;
                //    return false;
                //}

                DataTable docData = docDb.GetCompletedDocDetails(docAppId, DocStatusEnum.Verified.ToString(), DocStatusEnum.Completed.ToString(), ImageConditionEnum.NA.ToString(), DocTypeEnum.Miscellaneous.ToString(), SendToCDBStatusEnum.Sent.ToString(), SendToCDBStatusEnum.Sent.ToString());


                if (docData.Rows.Count > 0)
                {
                    // TODO: check if all the docs are sent to CDB, if then start processing
                    int haveVerifiedDoc = (from r in docData.AsEnumerable()
                                           where r.Field<string>("Status") == "Verified"
                                           select r).Count();
                    if (haveVerifiedDoc > 0 || string.IsNullOrEmpty(docAppRow["CompletenessStaffUserId"].ToString()))
                    {
                        foreach (DataRow dr in docData.Rows)
                        {

                            Guid? staffId = docSetDb.GetUserIdByDocId(int.Parse(dr["DocId"].ToString()));
                            Util.CDBDetailLog(string.Empty, String.Format("Docid: {0} - {1} ", int.Parse(dr["DocId"].ToString()), staffId.HasValue), EventLogEntryType.Warning);
                            if (staffId.HasValue)
                                systemInfo.completenessUserId = docSetDb.GetUserNameByVerificationStaffId(staffId.Value);
                            //}
                            else
                                systemInfo.completenessUserId = string.Empty;
                            break;
                        }
                    }
                    else
                    {
                        //if (!string.IsNullOrEmpty(docAppRow["CompletenessStaffUserId"].ToString()))
                        //{
                            Guid? staffId = (Guid?)docAppRow["CompletenessStaffUserId"];
                            Util.CDBDetailLog(string.Empty, String.Format("StaffUserId: {0} - {1}", docAppRow["CompletenessStaffUserId"].ToString(), staffId.HasValue), EventLogEntryType.Warning);
                            if (staffId.HasValue)
                                systemInfo.completenessUserId = docAppDb.GetUserNameByCompletenessStaffUserId(staffId.Value);
                            //}
                            else
                                systemInfo.completenessUserId = string.Empty;
                    }

                    var distinctNrics = (from r in docData.AsEnumerable()
                                         select r.Field<string>("Nric")).Distinct();

                    Util.CDBDetailLog(string.Empty, String.Format("Found {0} Customer(s) ", distinctNrics.Count()), EventLogEntryType.Information);
                    List<BE01JCustomerInfoDTO> customerList = new List<BE01JCustomerInfoDTO>();

                    foreach (string nric in distinctNrics)
                    {
                        DataTable dtDocsForNric;
                        int docCounter = 0;
                        bool isMainApplicant = false;
                        string nric_blank = "-"; //to display if customer NRIC is blank as -
                        if (!string.IsNullOrEmpty(nric))
                            nric_blank = nric;

                        Util.CDBDetailLog(string.Empty, String.Format("Processing Customer: {0} ", nric_blank), EventLogEntryType.Information);

                        foreach (DataRow dr in docData.Rows)
                        {
                            if ((string)dr["Nric"] == nric && ((string)dr["PersonalType"] == "HA" || (string)dr["PersonalType"] == "BU") && (int)dr["OrderNo"] == 1)
                            {
                                isMainApplicant = true;
                                break;
                            }
                            else
                                isMainApplicant = false;
                        }

                        //2013-01-11, to ensure that unique docId are counted
                        if (isMainApplicant)
                        {
                            docCounter = (from r in docData.AsEnumerable()
                                          where r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId && r.Field<string>("CmDocumentId").Substring(0, 3) != "XXX"
                                          group r by r["DocId"] into g
                                          select new { DocAppId = g.Key, DocCount = g.Count() }).Count();
                        }
                        else
                        {
                            docCounter = (from r in docData.AsEnumerable()
                                          where r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId && r.Field<string>("CmDocumentId").Substring(0, 3) != "XXX" &&
                                          (r.Field<string>("DocTypeCode").ToUpper() != "HLE" && r.Field<string>("DocTypeCode").ToUpper() != "SERS" && r.Field<string>("DocTypeCode").ToUpper() != "SALES" && r.Field<string>("DocTypeCode").ToUpper() != "RESALE")
                                          group r by r["DocId"] into g
                                          select new { DocAppId = g.Key, DocCount = g.Count() }).Count();
                        }
                        //2013-01-11, to get the top row to form the customer info 
                        var customerRow = (from r in docData.AsEnumerable() where r.Field<string>("Nric") == nric select r).FirstOrDefault();

                        if (customerRow != null && docCounter > 0)
                        {

                            BE01JCustomerInfoDTO customer = new BE01JCustomerInfoDTO();

                            // Customer Name
                            if (!String.IsNullOrEmpty(customerRow["Name"] as string))
                            {
                                customer.customerName = customerRow["Name"].ToString();
                            }
                            else
                            {
                                customer.customerName = string.Empty;
                            }

                            // Customer ID No
                            if (!String.IsNullOrEmpty(customerRow["Nric"] as string))
                            {
                                customer.identityNo = customerRow["Nric"].ToString();
                            }
                            else
                            {
                                customer.identityNo = string.Empty;
                            }

                            // Customer ID Type
                            if (!String.IsNullOrEmpty(customerRow["IdType"] as string))
                            {
                                customer.identityType = customerRow["IdType"].ToString();
                            }
                            else
                            {
                                customer.identityType = string.Empty;
                            }

                            //CustomerSourceId
                            if (!String.IsNullOrEmpty(customerRow["CustomerSourceId"] as string))
                            {
                                customer.customerIdFromSource = customerRow["CustomerSourceId"].ToString();
                            }
                            else
                            {
                                customer.customerIdFromSource = string.Empty;
                            }

                            //Customer Type
                            if (!String.IsNullOrEmpty(customerRow["CustomerType"] as string))
                            {
                                customer.customerType = customerRow["CustomerType"].ToString();
                            }
                            else
                            {
                                customer.customerType = string.Empty;
                            }

                            //DocCounter
                            customer.docCounter = docCounter;

                            //Get all the rows for each NRIC
                            if (isMainApplicant)
                            {
                                dtDocsForNric = docData.AsEnumerable().Where(r => r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId && r.Field<string>("CmDocumentId").Substring(0, 3) != "XXX").AsDataView().ToTable();
                            }
                            else
                            {
                                dtDocsForNric = docData.AsEnumerable().Where(r => r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId && r.Field<string>("CmDocumentId").Substring(0, 3) != "XXX" &&
                                    (r.Field<string>("DocTypeCode").ToUpper() != "HLE" && r.Field<string>("DocTypeCode").ToUpper() != "SERS" && r.Field<string>("DocTypeCode").ToUpper() != "SALES" && r.Field<string>("DocTypeCode").ToUpper() != "RESALE")).AsDataView().ToTable();
                            }
                            if (dtDocsForNric.Rows.Count > 0)
                            {

                                //For certain documents/images there are no metadata, like the requestor info,
                                //therefore this is used to retain the customer (requester) info
                                AcceptRequestorCustomer requestedCustomer = new AcceptRequestorCustomer();
                                requestedCustomer.customerName = customer.customerName;
                                requestedCustomer.identityNo = customer.identityNo;
                                requestedCustomer.identityType = customer.identityType;
                                requestedCustomer.customerIdFromSource = customer.customerIdFromSource;

                                //Entry point to populating the Documents for each Customer
                                customer.documentInfoList = GetDocumentInfo(dtDocsForNric, requestedCustomer, docAppId, SendToCDBStageEnum.Accept, out count);
                                customer.docCounter = count;
                                customerList.Add(customer);
                                hasDoc = true;
                            }
                            else
                            {
                                customer.documentInfoList = null;
                            }
                        }
                    }
                    businessInfo.customerInfoList = customerList.ToArray();
                    return hasDoc;
                }
                else
                {
                    Util.CDBDetailLog(string.Empty, String.Format("DocApp: {0} has no documents to send ", docAppId), EventLogEntryType.Information);
                    return false;
                }
                //return false;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);

                Util.CDBLog("CompleteAccept.GetDocs()", errorMessage, EventLogEntryType.Error);

                return false;
            }
        }


        private string GetOutputFilePathAndName(string filePath, BE01JOutputDTO output)
        {
            if (ValidateCDBForDocAppCompletedAccept(output))
                return filePath + ".Out";
            else
                return filePath + ".OutErr";
        }

        private string GetFilePathAndNameInputErrorCheck(bool hasBusinessInfo, string filePath, bool isAllDocsSetsSent)
        {
            if (hasBusinessInfo)
                return filePath;
            else
            {
                if (!isAllDocsSetsSent)
                    return filePath + ".AllDocSetsNotSentToCDB";
                else
                    return filePath + ".InpErr";
            }
        }

        private static bool ValidateCDBForDocAppCompletedAccept(BE01JOutputDTO output)
        {
            if (output != null)
            {
                //Check if the OutputDTO result flag,
                if (output.obsResultFlag.Trim().ToUpper() == CDBAcceptOutputStatus.A.ToString())
                {
                    return true;
                }
                else if (output.obsResultFlag.Trim().ToUpper() == CDBAcceptOutputStatus.R.ToString())
                {
                    return false;
                }
                else if (output.obsResultFlag.Trim().ToUpper() == CDBAcceptOutputStatus.W.ToString())
                {

                    return false;
                }
                else
                    return false;

            }
            else
            {
                return false;
            }
        }
    }
}
