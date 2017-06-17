using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.VerifyDocWebRef;
using System.Data;
using DWMS_OCR.App_Code.Bll;
using System.Diagnostics;
using DWMS_OCR.App_Code.Helper;
using DWMS_OCR.App_Code.Dal;
using System.IO;


namespace DWMS_OCR.CdbService
{

    class CDBVerify : CDBVerifyUtil
    {
        public bool TriggerTest { get; set; }
        public bool TriggerUpdateResultToDatabase { get; set; }
        public bool TriggerSendToCDBVerify { get; set; }
        public bool XmlOutput { get; set; }
        public bool RunOnce { get; set; }


        public void SendAllDocsUponVerificationVerified()
        {
            try
            {
                BE01JVerifyDocService webRef = new BE01JVerifyDocService();
                DocSetDb docSetDb = new DocSetDb();
                DocAppDb docAppDb = new DocAppDb();
                bool logging = Util.Logging();
                bool detailLogging = Util.DetailLogging();

                using (DataTable dt = docSetDb.GetVerifiedReadyDocSets(SendToCDBStatusEnum.Ready))//, "SYSTEM"))
                {
                    if (dt.Rows.Count > 0)
                    {
                        if (logging) Util.CDBLog(string.Empty, String.Format("VERIFY: Found {0} Document Set(s) with " + SendToCDBStatusEnum.Ready.ToString() + " to send to CDB status", dt.Rows.Count), EventLogEntryType.Information);
                        foreach (DataRow docSetRow in dt.Rows)
                        {
                            if (((int)docSetRow["SendToCDBAttemptCount"] < CDBVerifyUtil.GetMaxAttemptAllowedForVerifiedDocSets()) || docSetRow.IsNull("SendToCDBAttemptCount"))
                            {
                                //if the docset failed (i.e. the data retrieval, input, connection, or output had any problem), will be marked as 'Ready' 
                                bool hasSentToCDB = false;
                                int attemptCount = 0;
                                DataTable docappt = docAppDb.GetDocAppByDocSetId((int)docSetRow["Id"]);
                                DataRow docAppRow = docappt.Rows[0];

                                for (attemptCount = 0; attemptCount < CDBVerifyUtil.GetMaxAttemptAllowedForVerifiedDocSets(); attemptCount++)
                                {
                                    hasSentToCDB = SendVerifiedDocSet(webRef, docSetDb, docSetRow, attemptCount + 1);
                                    if (hasSentToCDB)
                                        break;
                                }

                                if (TriggerUpdateResultToDatabase)
                                {
                                    if (hasSentToCDB)
                                    {
                                        docSetDb.UpdateSetSentToCDBStatus((int)docSetRow["Id"], SendToCDBStatusEnum.Sent, attemptCount);
                                        docAppDb.UpdateSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.Ready, attemptCount);
                                    }
                                    else
                                    {
                                        docSetDb.UpdateSetSentToCDBStatus((int)docSetRow["Id"], SendToCDBStatusEnum.SentButFailed, attemptCount);
                                        docAppDb.UpdateSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.NotReady, attemptCount);
                                    }
                                }
                            }
                            else
                            {
                                //if (logging) Util.CDBLog(string.Empty, String.Format("VERIFY: DocSet: {0} has exceeded the max. number of attempts to sent to CDB, therefore marked not to be processed", (int)docSetRow["Id"]), EventLogEntryType.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.SendVerifiedDocSets()", errorMessage, EventLogEntryType.Error);
            }
        }

        public void RunXmlTest()
        {
            Util.CDBLog(string.Empty, String.Format("VERIFY: Start RunXmlTest"), EventLogEntryType.Information);

            string inputFilePath = CDBVerifyUtil.GetTestInputXMLForDWMSToCDBFilePathAndNameVerify();
            BE01JSystemInfoDTO systemInfo = new BE01JSystemInfoDTO();
            BE01JAuthenticationDTO authentication = new BE01JAuthenticationDTO();
            BE01JVerifyDocService webRef = new BE01JVerifyDocService();
            bool errorAtImport = false;
            BE01JBusinessInfoDTO businessInfoTest = XmlInput(inputFilePath, ref systemInfo, ref authentication);
            string filePath = string.Empty;
            BE01JOutputDTO result = new BE01JOutputDTO();
            //bool writeData = false;
            string checkedFilePath = string.Empty;
            if (businessInfoTest != null)
            {
                filePath = GetFilePathAndName(businessInfoTest, 1);
                checkedFilePath = filePath;
                Util.CDBLog(string.Empty, String.Format("VERIFY: Start input file: " + checkedFilePath), EventLogEntryType.Information);
                //writeData = GenerateInputTextFile(systemInfo, businessInfoTest, authentication, checkedFilePath);
                //Util.CDBLog(string.Empty, String.Format("VERIFY: End writing input file"), EventLogEntryType.Information);
            }
            else
            {
                checkedFilePath = filePath + ".InpErr";
                errorAtImport = true;
            }
            bool errorAtVerifyDocument = false;
            if (!errorAtImport)
            {
                try
                {
                    if (TriggerSendToCDBVerify)
                    {
                        Util.CDBLog(string.Empty, String.Format("VERIFY: Start sending to CDB"), EventLogEntryType.Information);
                        result = webRef.verifyDocument(authentication, systemInfo, businessInfoTest);
                        Util.CDBLog(string.Empty, String.Format("VERIFY: End sending to CDB"), EventLogEntryType.Information);
                    }
                    else
                    {
                        result = null;
                        Util.CDBLog(string.Empty, String.Format("VERIFY: Calling CDB currently configured to be OFF in this service"), EventLogEntryType.Warning);
                    }
                }
                catch
                {
                    errorAtVerifyDocument = true;
                    result = null;
                    Util.CDBLog(string.Empty, String.Format("VERIFY: Start writing error to file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                    checkedFilePath = filePath + ".ConnErr";
                    //bool resultError = WriteConnectionErrorTextFile(checkedFilePath);
                    Util.CDBLog(string.Empty, String.Format("VERIFY: End writing error to file"), EventLogEntryType.Information);
                    Util.CDBLog(string.Empty, String.Format("VERIFY: Failed to connect to CDB. No output file is produced and the Err file being generated"), EventLogEntryType.Error);
                }
            }
            else
            {
                Util.CDBLog(string.Empty, String.Format("VERIFY: Error at import data from text file, please correct the data. Log file(s) are not generated and no information has been sent to CDB"), EventLogEntryType.Error);
            }
            if (!errorAtVerifyDocument && !errorAtImport)
            {
                string filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                //Util.CDBLog(string.Empty, String.Format("VERIFY: Start writing result to file: " + filePathOut), EventLogEntryType.Information);
                //bool resultWrite = GenerateOutputTextFile(filePathOut, checkedFilePath, result, businessInfoTest, systemInfo, authentication, SendToCDBStageEnum.Verified);
                Util.CDBLog(string.Empty, String.Format("VERIFY: End writing result to file"), EventLogEntryType.Information);
                if (ValidateCDBVerifyForDocSet(result))
                    checkedFilePath = filePath;
                else
                    checkedFilePath = filePath + ".OutErr";
            }
            if (XmlOutput)
            {
                GenerateXmlOutput(checkedFilePath, result, businessInfoTest, systemInfo);
            }

        }

        private bool SendVerifiedDocSet(BE01JVerifyDocService webRef, DocSetDb docSetDb, DataRow docSetRow, int currAttempt)
        {
            BE01JSystemInfoDTO systemInfo = new BE01JSystemInfoDTO();
            BE01JBusinessInfoDTO businessInfo = new BE01JBusinessInfoDTO();
            BE01JOutputDTO result = new BE01JOutputDTO();
            BE01JAuthenticationDTO authentication = new BE01JAuthenticationDTO();
            bool hasDocSetSuccessfullySentToCDB = true;
            
            //Get the Main Data
            DataTable dtDocAppAndDocData = docSetDb.GetVerifiedAppAndDocData((int)docSetRow["Id"], 0, DocStatusEnum.Verified, DocStatusEnum.Completed, ImageConditionEnum.NA, DocTypeEnum.Miscellaneous, SendToCDBStatusEnum.Sent);
            ProfileDb profileDb = new ProfileDb();
            string userName = profileDb.GetUserNameByUserId((Guid)docSetRow["VerificationStaffUserId"]);
            
            if (dtDocAppAndDocData.Rows.Count > 0)
            {
                var distinctDocAppIds = (from row in dtDocAppAndDocData.AsEnumerable()
                                         select row.Field<int>("DocAppId")).Distinct();

                foreach (int docAppId in distinctDocAppIds)
                {
                    string filePath = string.Empty;
                    string errorPath = string.Empty;
                    string checkedFilePath = string.Empty;
                    bool hasInputInfo = false;
                    businessInfo.customerInfoList = null;
                    bool connError = false;
                    LogActionDb logActionDb = new LogActionDb();

                    Util.CDBLog(string.Empty, String.Format("VERIFY: Processing: DocSet({0}), Document App ID({1}) ", docSetRow.Field<int>("Id"), docAppId), EventLogEntryType.Information);

                    #region Modified by Edward 5.12.2013 for ResaleBuy/ResaleSell

                    if (!CheckRefTypeIsResale(docAppId))
                    {

                        // 1. Retrieve Data
                        hasInputInfo = GetVerifiedDocs(docAppId, dtDocAppAndDocData, ref businessInfo, ref systemInfo, ref authentication, (Guid)docSetRow["VerificationStaffUserId"]);
                        filePath = GetFilePathAndName(businessInfo, currAttempt);

                        #region write the input file
                        // 2. Write the Input file
                        //if (!hasInputInfo) checkedFilePath = filePath + ".InpErr";
                        //string checkedFilePath = GetFilePathAndNameInputErrorCheck(hasInputInfo, filePath);
                        //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start writing input to file: " + checkedFilePath), EventLogEntryType.Information);
                        //GenerateInputTextFile(systemInfo, businessInfo, authentication, filePath + "-inp.txt");
                        //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: End writing input to file"), EventLogEntryType.Information);
                        #endregion
                        //result = webRef.verifyDocument(authentication, systemInfo, businessInfo); //commented by Edward 07.01.2014
                        #region send to CDB Verification
                        // 3. Send data to CDB 
                        if (!hasInputInfo)
                        {
                            Util.CDBLog(string.Empty, String.Format("VERIFY: Retrieval of data for DocSetId({0}), DocAppId({1}) failed, this DocApp will not be sent to CDB.", docAppId, (int)docSetRow["Id"]), EventLogEntryType.Error);
                            checkedFilePath = filePath + ".InpErr";
                            hasDocSetSuccessfullySentToCDB = false;
                        }
                        else //if retrieval of data from DB is ok
                        {
                            if (TriggerSendToCDBVerify)
                            {
                                try
                                {
                                    for (int cnt = 0; cnt < 3; cnt++)
                                    {
                                        webRef.Timeout = 300000;//add timeout of 5min
                                        Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start sending to CDB attempt " + cnt + 1), EventLogEntryType.Warning);
                                        result = webRef.verifyDocument(authentication, systemInfo, businessInfo);
                                        if (result.ToString().Length > 0)
                                        {
                                            connError = false;

                                            //if atleast once failed, will be failed at DocSet level, 
                                            //i.e. it the DocSet's SentToCDBStaus should not be set to 'Sent'
                                            //this is the reason for adding this flag. Check that all doc is sent.
                                            //if (hasDocSetSuccessfullySentToCDB)
                                            //    hasDocSetSuccessfullySentToCDB = true;

                                            Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Successfully sent to CDB"), EventLogEntryType.Information);
                                            break;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    connError = true;
                                    hasDocSetSuccessfullySentToCDB = false;
                                    Util.CDBLog(string.Empty, String.Format("VERIFY: Connection to CDB attempt failed, Message: " + ex.Message + ", StackTrace: " + ex.StackTrace), EventLogEntryType.Error);
                                    logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                                }
                            }
                            else
                            {
                                connError = true; //connection is diabled
                                hasDocSetSuccessfullySentToCDB = false;
                                Util.CDBLog(string.Empty, String.Format("VERIFY: Calling to CDB's currently turned off in this service"), EventLogEntryType.Warning);
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            }

                            #region write connection error file
                            if (connError)
                            {
                                //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start writing error to file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                                //bool resultError = WriteConnectionErrorFile(checkedFilePath);
                                checkedFilePath = filePath + ".ConnErr";
                                //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: End writing error to file"), EventLogEntryType.Information);
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            }
                            #endregion
                        }
                        #endregion


                        #region check the output, create output/err file, update DB
                        // 4. GenerateOutputTextFile //the output result is returned from CDB
                        //string filePathOut = string.Empty;
                        if (!connError && hasInputInfo && TriggerSendToCDBVerify)
                        {
                            //Can be either an OutputError or Output file
                            //filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                            if (ValidateCDBVerifyForDocSet(result))
                                checkedFilePath = filePath;
                            else
                                checkedFilePath = filePath + ".OutErr";

                            //checkedFilePath = filePath;
                            Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start writing output result to file: " + checkedFilePath), EventLogEntryType.Information);
                            bool resultWrite = ProcessOutput(result, businessInfo, systemInfo, authentication, SendToCDBStageEnum.Verified);
                            //bool resultWrite = ProcessOutput(result, businessInfo, systemInfo, authentication, SendToCDBStageEnum.Verified);


                            //if atleast once failed, will be failed at DocSet level, 
                            //i.e. it the DocSet's SentToCDBStaus should not be set to 'Sent'
                            // if it is true, then check the status of validation
                            // if it is false, then no need to false it again.
                            if (hasDocSetSuccessfullySentToCDB)
                                hasDocSetSuccessfullySentToCDB = ValidateCDBVerifyForDocSet(result);
                            if (hasDocSetSuccessfullySentToCDB)
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB successfully", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            else
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB failed", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: End writing output result to file sent from CDB"), EventLogEntryType.Information);
                        }
                        #endregion

                        #region write output to xml
                        if (XmlOutput)
                            GenerateXmlOutput(checkedFilePath, result, businessInfo, systemInfo);
                        #endregion
                    }
                    else
                    {
                        #region if Resale, it will push 2 times one for ResaleSell and for ResaleBuy

                        bool WithError = false; //checks for error by try catch
                        hasDocSetSuccessfullySentToCDB = true;


                        bool hasResaleSellInfo;
                        bool IsResaleSellSuccess = false;
                        hasResaleSellInfo = GetVerifiedDocsForResale(docAppId, dtDocAppAndDocData, "SE", ref businessInfo, ref systemInfo, ref authentication,
                            (Guid)docSetRow["VerificationStaffUserId"], out WithError);
                        filePath = GetFilePathAndName(businessInfo,"ResaleSell", currAttempt);
                        Util.CDBDetailLog(string.Empty, String.Format("AFTER : Getting ResaleSellInfo {0}", hasResaleSellInfo.ToString()), EventLogEntryType.Information);
                        //result = webRef.verifyDocument(authentication, systemInfo, businessInfo);

                        #region send to CDB Verification
                        if (!hasResaleSellInfo && !WithError)
                        {
                            //Util.CDBLog(string.Empty, String.Format("VERIFY: No Documents for ResaleSell DocSetId({0}), DocAppId({1})", docAppId, (int)docSetRow["Id"]), EventLogEntryType.Error);
                            //checkedFilePath = filePath + ".InpErr";
                            IsResaleSellSuccess = true;
                        }
                        else //if retrieval of data from DB is ok
                        {
                            if (TriggerSendToCDBVerify)
                            {
                                try
                                {
                                    for (int cnt = 0; cnt < 3; cnt++)
                                    {
                                        webRef.Timeout = 300000;//add timeout of 5min
                                        Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start sending to CDB attempt " + cnt + 1), EventLogEntryType.Warning);
                                        result = webRef.verifyDocument(authentication, systemInfo, businessInfo);
                                        if (result.ToString().Length > 0)
                                        {
                                            connError = false;
                                            Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Successfully sent to CDB"), EventLogEntryType.Information);
                                            IsResaleSellSuccess = true;
                                            break;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    connError = true;
                                    hasDocSetSuccessfullySentToCDB = false;
                                    Util.CDBLog(string.Empty, String.Format("VERIFY: Connection to CDB attempt failed, Message: " + ex.Message + ", StackTrace: " + ex.StackTrace), EventLogEntryType.Error);
                                    logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                                }
                            }
                            else
                            {
                                connError = true; //connection is diabled
                                hasDocSetSuccessfullySentToCDB = false;
                                Util.CDBLog(string.Empty, String.Format("VERIFY: Calling to CDB's currently turned off in this service"), EventLogEntryType.Warning);
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            }

                            #region write connection error file
                            if (connError)
                            {
                                checkedFilePath = filePath + ".ConnErr";
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            }
                            #endregion
                        }
                        #endregion

                        #region check the output, create output/err file, update DB
                        // 4. GenerateOutputTextFile //the output result is returned from CDB
                        if (!connError && (hasResaleSellInfo) && TriggerSendToCDBVerify)
                        {
                            //Can be either an OutputError or Output file
                            if (ValidateCDBVerifyForDocSet(result))
                                checkedFilePath = filePath;
                            else
                                checkedFilePath = filePath + ".OutErr";

                            Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start writing output result to file: " + checkedFilePath), EventLogEntryType.Information);
                            bool resultWrite = ProcessOutput(result, businessInfo, systemInfo, authentication, SendToCDBStageEnum.Verified);

                            if (IsResaleSellSuccess)
                                IsResaleSellSuccess = ValidateCDBVerifyForDocSet(result);

                        }
                        #endregion

                        #region write output to xml
                        if (XmlOutput)
                            GenerateXmlOutput(checkedFilePath, result, businessInfo, systemInfo);
                        #endregion




                        bool hasResaleBuyInfo;
                        bool IsResaleBuySuccess = false;

                        #region initialize for BU
                        BE01JSystemInfoDTO systemInfo1 = new BE01JSystemInfoDTO();
                        BE01JBusinessInfoDTO businessInfo1 = new BE01JBusinessInfoDTO();
                        BE01JOutputDTO result1 = new BE01JOutputDTO();
                        BE01JAuthenticationDTO authentication1 = new BE01JAuthenticationDTO();
                        businessInfo1.customerInfoList = null;
                        #endregion



                        hasResaleBuyInfo = GetVerifiedDocsForResale(docAppId, dtDocAppAndDocData, "BU", ref businessInfo1, ref systemInfo1,
                            ref authentication1, (Guid)docSetRow["VerificationStaffUserId"], out WithError);
                        filePath = GetFilePathAndName(businessInfo1, "ResaleBuy", currAttempt);
                        Util.CDBDetailLog(string.Empty, String.Format("AFTER : Getting ResaleBuyInfo {0}", hasResaleBuyInfo.ToString()), EventLogEntryType.Information);
                        //result1 = webRef.verifyDocument(authentication1, systemInfo1, businessInfo1);

                        #region send to CDB Verification
                        // 3. Send data to CDB 
                        if (!hasResaleBuyInfo && !WithError)
                        {
                            //Util.CDBLog(string.Empty, String.Format("VERIFY: No documents for ResaleBuy DocSetId({0}), DocAppId({1})", docAppId, (int)docSetRow["Id"]), EventLogEntryType.Error);
                            //checkedFilePath = filePath + ".InpErr";
                            IsResaleBuySuccess = true;
                        }
                        else //if retrieval of data from DB is ok
                        {
                            if (TriggerSendToCDBVerify)
                            {
                                try
                                {
                                    for (int cnt = 0; cnt < 3; cnt++)
                                    {
                                        webRef.Timeout = 300000;//add timeout of 5min
                                        Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start sending to CDB attempt " + cnt + 1), EventLogEntryType.Warning);
                                        result1 = webRef.verifyDocument(authentication1, systemInfo1, businessInfo1);
                                        if (result.ToString().Length > 0)
                                        {
                                            connError = false;
                                            Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Successfully sent to CDB"), EventLogEntryType.Information);
                                            IsResaleBuySuccess = true;
                                            break;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    connError = true;
                                    hasDocSetSuccessfullySentToCDB = false;
                                    Util.CDBLog(string.Empty, String.Format("VERIFY: Connection to CDB attempt failed, Message: " + ex.Message + ", StackTrace: " + ex.StackTrace), EventLogEntryType.Error);
                                    logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                                }
                            }
                            else
                            {
                                connError = true; //connection is diabled
                                hasDocSetSuccessfullySentToCDB = false;
                                Util.CDBLog(string.Empty, String.Format("VERIFY: Calling to CDB's currently turned off in this service"), EventLogEntryType.Warning);
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            }

                            #region write connection error file
                            if (connError)
                            {
                                checkedFilePath = filePath + ".ConnErr";
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            }
                            #endregion
                        }
                        #endregion

                        #region check the output, create output/err file, update DB
                        // 4. GenerateOutputTextFile //the output result is returned from CDB
                        if (!connError && (hasResaleBuyInfo) && TriggerSendToCDBVerify)
                        {
                            //Can be either an OutputError or Output file
                            if (ValidateCDBVerifyForDocSet(result1))
                                checkedFilePath = filePath;
                            else
                                checkedFilePath = filePath + ".OutErr";

                            Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start writing output result to file: " + checkedFilePath), EventLogEntryType.Information);
                            bool resultWrite = ProcessOutput(result1, businessInfo1, systemInfo1, authentication1, SendToCDBStageEnum.Verified);

                            if (IsResaleBuySuccess)
                                IsResaleBuySuccess = ValidateCDBVerifyForDocSet(result1);

                        }
                        #endregion

                        #region write output to xml
                        if (XmlOutput)
                            GenerateXmlOutput(checkedFilePath, result1, businessInfo1, systemInfo1);
                        #endregion



                        if (hasDocSetSuccessfullySentToCDB)
                        {
                            if ((IsResaleBuySuccess && IsResaleSellSuccess) && (hasResaleBuyInfo || hasResaleSellInfo))
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB successfully", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            else
                            {
                                logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB failed", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                                hasDocSetSuccessfullySentToCDB = false;
                            }
                        }
                        else
                        {
                            logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB failed", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                            hasDocSetSuccessfullySentToCDB = false;
                        }
                        #endregion
                    }
                                                            
                    #endregion


                    #region Commented By Edward 5.12.2013 for ResaleBuy/ResaleSell

                    //// 1. Retrieve Data
                    //hasInputInfo = GetVerifiedDocs(docAppId, dtDocAppAndDocData, ref businessInfo, ref systemInfo, ref authentication, (Guid)docSetRow["VerificationStaffUserId"]);
                    //filePath = GetFilePathAndName(businessInfo, currAttempt);

                    //#region write the input file
                    //// 2. Write the Input file
                    ////if (!hasInputInfo) checkedFilePath = filePath + ".InpErr";
                    ////string checkedFilePath = GetFilePathAndNameInputErrorCheck(hasInputInfo, filePath);
                    ////Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start writing input to file: " + checkedFilePath), EventLogEntryType.Information);
                    ////GenerateInputTextFile(systemInfo, businessInfo, authentication, filePath + "-inp.txt");
                    ////Util.CDBDetailLog(string.Empty, String.Format("VERIFY: End writing input to file"), EventLogEntryType.Information);
                    //#endregion

                    //#region send to CDB Verification
                    //// 3. Send data to CDB 
                    //if (!hasInputInfo)
                    //{
                    //    Util.CDBLog(string.Empty, String.Format("VERIFY: Retrieval of data for DocSetId({0}), DocAppId({1}) failed, this DocApp will not be sent to CDB.", docAppId, (int)docSetRow["Id"]), EventLogEntryType.Error);
                    //    checkedFilePath = filePath + ".InpErr"; 
                    //    hasDocSetSuccessfullySentToCDB = false;
                    //}
                    //else //if retrieval of data from DB is ok
                    //{
                    //    if (TriggerSendToCDBVerify)
                    //    {
                    //        try
                    //        {
                    //            for (int cnt = 0; cnt < 3; cnt++)
                    //            {
                    //                webRef.Timeout = 300000;//add timeout of 5min
                    //                Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start sending to CDB attempt " + cnt + 1), EventLogEntryType.Warning);
                    //                result = webRef.verifyDocument(authentication, systemInfo, businessInfo);
                    //                if (result.ToString().Length > 0)
                    //                {
                    //                    connError = false;

                    //                    //if atleast once failed, will be failed at DocSet level, 
                    //                    //i.e. it the DocSet's SentToCDBStaus should not be set to 'Sent'
                    //                    //this is the reason for adding this flag. Check that all doc is sent.
                    //                    //if (hasDocSetSuccessfullySentToCDB)
                    //                    //    hasDocSetSuccessfullySentToCDB = true;

                    //                    Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Successfully sent to CDB"), EventLogEntryType.Information);
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            connError = true;
                    //            hasDocSetSuccessfullySentToCDB = false;
                    //            Util.CDBLog(string.Empty, String.Format("VERIFY: Connection to CDB attempt failed, Message: " + ex.Message + ", StackTrace: " + ex.StackTrace), EventLogEntryType.Error);
                    //            logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        connError = true; //connection is diabled
                    //        hasDocSetSuccessfullySentToCDB = false;
                    //        Util.CDBLog(string.Empty, String.Format("VERIFY: Calling to CDB's currently turned off in this service"), EventLogEntryType.Warning);
                    //        logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                    //    }

                    //    #region write connection error file
                    //    if (connError)
                    //    {
                    //        //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start writing error to file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                    //        //bool resultError = WriteConnectionErrorFile(checkedFilePath);
                    //        checkedFilePath = filePath + ".ConnErr";
                    //        //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: End writing error to file"), EventLogEntryType.Information);
                    //        logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                    //    }
                    //    #endregion
                    //}
                    //#endregion


                    //#region check the output, create output/err file, update DB
                    //// 4. GenerateOutputTextFile //the output result is returned from CDB
                    ////string filePathOut = string.Empty;
                    //if (!connError && hasInputInfo && TriggerSendToCDBVerify)
                    //{
                    //    //Can be either an OutputError or Output file
                    //    //filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                    //    if (ValidateCDBVerifyForDocSet(result))
                    //        checkedFilePath = filePath;
                    //    else
                    //        checkedFilePath = filePath + ".OutErr";

                    //    //checkedFilePath = filePath;
                    //    Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Start writing output result to file: " + checkedFilePath), EventLogEntryType.Information);
                    //    bool resultWrite = ProcessOutput(result, businessInfo, systemInfo, authentication, SendToCDBStageEnum.Verified);
                    //    //bool resultWrite = ProcessOutput(result, businessInfo, systemInfo, authentication, SendToCDBStageEnum.Verified);


                    //    //if atleast once failed, will be failed at DocSet level, 
                    //    //i.e. it the DocSet's SentToCDBStaus should not be set to 'Sent'
                    //    // if it is true, then check the status of validation
                    //    // if it is false, then no need to false it again.
                    //    if (hasDocSetSuccessfullySentToCDB)
                    //        hasDocSetSuccessfullySentToCDB = ValidateCDBVerifyForDocSet(result);
                    //    if (hasDocSetSuccessfullySentToCDB)
                    //        logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB successfully", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                    //    else
                    //        logActionDb.Insert((Guid)docSetRow["VerificationStaffUserId"], "Set sent to CDB failed", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)docSetRow["Id"]);
                    //    //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: End writing output result to file sent from CDB"), EventLogEntryType.Information);
                    //}
                    //#endregion

                    //#region write output to xml
                    //if (XmlOutput)
                    //    GenerateXmlOutput(checkedFilePath, result, businessInfo, systemInfo);
                    //#endregion

                    #endregion
                }
            }
            else
            {
                hasDocSetSuccessfullySentToCDB = true;
            }

            return hasDocSetSuccessfullySentToCDB;

        }

        private bool GetVerifiedDocs(int docAppId, DataTable dataTable, ref BE01JBusinessInfoDTO businessInfo, ref BE01JSystemInfoDTO systemInfo, ref BE01JAuthenticationDTO authentication, Guid verificationStaffUserId)
        {
            try
            {
                DocSetDb docSetDb = new DocSetDb();

                systemInfo.fileSystemId = "DWMS";
                systemInfo.updateSystemId = "DWMS";
                systemInfo.fileDate = DateTime.Now;

                systemInfo.updateDate = DateTime.Today;
                systemInfo.updateTime = DateTime.Now;

                systemInfo.verificationUserId = docSetDb.GetUserNameByVerificationStaffId(verificationStaffUserId);
                systemInfo.completenessUserId = string.Empty;

                authentication.userName = CDBVerifyUtil.GetUserNameDWMSToCDB();
                authentication.passWord = CDBVerifyUtil.GetPasswordDWMSToCDB();

                bool hasDoc = false;
                int count;

                //Populate BusinessInfo fields
                DocAppDb docApp = new DocAppDb();
                DocApp.DocAppDataTable docAppDt = docApp.GetDocAppById(docAppId);

                if (docAppDt.Rows.Count > 0)
                {
                    DocApp.DocAppRow row = docAppDt.Rows[0] as DocApp.DocAppRow;
                    businessInfo.businessRefNumber = row.RefNo;
                    businessInfo.businessTransactionNumber = row.RefType;
                }

                //copy to new datatable to include the records with the current docAppId only
                DataTable dtDocAppAndData = dataTable.AsEnumerable().Where(row => row.Field<int>("DocAppId") == docAppId).AsDataView().ToTable();

                List<BE01JCustomerInfoDTO> customerList = new List<BE01JCustomerInfoDTO>();

                if (dtDocAppAndData.Rows.Count > 0)
                {
                    BE01JCustomerInfoDTO customer;

                    var distinctNrics = (from row in dtDocAppAndData.AsEnumerable()
                                         select row.Field<string>("Nric")).Distinct();

                    Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Found {0} Customer(s) ", distinctNrics.Count()), EventLogEntryType.Information);


                    foreach (string nric in distinctNrics)
                    {
                        DataTable dtDocsForNric;
                        int docCounter = 0;
                        bool isMainApplicant = false;
                        string nric_blank = "-"; //to display if customer NRIC is blank as -
                        if (!string.IsNullOrEmpty(nric))
                            nric_blank = nric;

                        Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Processing Customer: {0} ", nric_blank), EventLogEntryType.Information);

                        //2013-01-11, to ensure that unique docId are counted
                        foreach (DataRow dr in dtDocAppAndData.Rows)
                        {
                            if ((string)dr["Nric"] == nric && ((string)dr["PersonalType"] == "HA" || (string)dr["PersonalType"] == "BU") && (int)dr["OrderNo"] == 1)
                            {
                                isMainApplicant = true;
                                break;
                            }
                            else
                                isMainApplicant = false;
                        }
                        //isMainApplicant = false;
                        if (isMainApplicant)
                        {
                            docCounter = (from r in dtDocAppAndData.AsEnumerable()
                                          where r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId
                                          group r by r["DocId"] into g
                                          select new { DocAppId = g.Key, DocCount = g.Count() }).Count();
                        }
                        else
                        {
                            docCounter = (from r in dtDocAppAndData.AsEnumerable()
                                          where r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId &&
                                          (r.Field<string>("DocTypeCode").ToUpper() != "HLE" && r.Field<string>("DocTypeCode").ToUpper() != "SERS" && r.Field<string>("DocTypeCode").ToUpper() != "SALES" && r.Field<string>("DocTypeCode").ToUpper() != "RESALE")
                                          group r by r["DocId"] into g
                                          select new { DocAppId = g.Key, DocCount = g.Count() }).Count();
                        }
                            
                        
                        //2013-01-11, to get the top row to form the customer info 
                        var row = (from r in dtDocAppAndData.AsEnumerable() where r.Field<string>("Nric") == nric select r).FirstOrDefault();


                        if (row != null && docCounter > 0)
                        {

                            customer = new BE01JCustomerInfoDTO();

                            // Customer Name
                            if (!String.IsNullOrEmpty(row["Name"] as string))
                            {
                                customer.customerName = row["Name"].ToString();
                            }
                            else
                            {
                                customer.customerName = string.Empty;
                            }

                            // Customer ID No
                            if (!String.IsNullOrEmpty(row["Nric"] as string))
                            {
                                customer.identityNo = row["Nric"].ToString();
                            }
                            else
                            {
                                customer.identityNo = string.Empty;
                            }

                            // Customer ID Type
                            if (!String.IsNullOrEmpty(row["IdType"] as string))
                            {
                                customer.identityType = row["IdType"].ToString();
                            }
                            else
                            {
                                customer.identityType = string.Empty;
                            }

                            //CustomerSourceId
                            if (!String.IsNullOrEmpty(row["CustomerSourceId"] as string))
                            {
                                customer.customerIdFromSource = row["CustomerSourceId"].ToString();
                            }
                            else
                            {
                                customer.customerIdFromSource = string.Empty;
                            }

                            //Customer Type
                            if (!String.IsNullOrEmpty(row["CustomerType"] as string))
                            {
                                customer.customerType = row["CustomerType"].ToString();
                            }
                            else
                            {
                                customer.customerType = CustomerTypeEnum.P.ToString();
                            }


                            //DocCounter
                            customer.docCounter = docCounter;

                            //Get all the rows for each NRIC
                            if (isMainApplicant)
                            {
                                dtDocsForNric = dataTable.AsEnumerable().Where(r => r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId).AsDataView().ToTable();
                            }
                            else
                            {
                                dtDocsForNric = dataTable.AsEnumerable().Where(r => r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId &&
                                    (r.Field<string>("DocTypeCode").ToUpper() != "HLE" && r.Field<string>("DocTypeCode").ToUpper() != "SERS" && r.Field<string>("DocTypeCode").ToUpper() != "SALES" && r.Field<string>("DocTypeCode").ToUpper() != "RESALE")).AsDataView().ToTable();
                            }

                            if (dtDocsForNric.Rows.Count > 0)
                            {

                                //For certain documents/images there are no metadata, like the requestor info,
                                //therefore this is used to retain the customer (requester) info
                                VerifyRequestorCustomer requestedCustomer = new VerifyRequestorCustomer();
                                requestedCustomer.customerName = customer.customerName;
                                requestedCustomer.identityNo = customer.identityNo;
                                requestedCustomer.identityType = customer.identityType;
                                requestedCustomer.customerIdFromSource = customer.customerIdFromSource;

                                //Entry point to populating the Documents for each Customer
                                customer.documentInfoList = GetDocumentInfo(dtDocsForNric, requestedCustomer, docAppId, SendToCDBStageEnum.Verified, out count);
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

                return false;
            }
            catch (Exception ex)
            {

                string errorMessage = String.Format("VERIFY: Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);

                Util.CDBLog("DWMS_CDB_Service.GetVerifiedDocs()", errorMessage, EventLogEntryType.Error);

                return false;
            }
        }

        private string GetFilePathAndName(BE01JBusinessInfoDTO businessInfo, int attemptCount)
        {
            if (!TriggerTest)
            {
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.VerifyDWMSToCDBLogFileName, businessInfo.businessRefNumber, Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss), attemptCount));
            }
            else
            {
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.VerifyDWMSToCDBLogFileName, "Xml_Input", Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss),attemptCount));
            }
        }

        private string GetFilePathAndNameInputErrorCheck(bool hasBusinessInfo, string filePath)
        {
            if (hasBusinessInfo)
                return filePath;
            else
                return filePath + ".InpErr";

        }

        private string GetOutputFilePathAndName(string filePath, BE01JOutputDTO output)
        {
            if (ValidateCDBVerifyForDocSet(output))
                return filePath + ".Out";
            else
                return filePath + ".OutErr";
        }

        private static bool ValidateCDBVerifyForDocSet(BE01JOutputDTO output)
        {
            if (output != null)
            {
                //Check if the OutputDTO result flag,
                if (output.obsResultFlag.Trim().ToUpper() == CDBVerifyOutputStatus.A.ToString())
                {
                    return true;
                }
                else if (output.obsResultFlag.Trim().ToUpper() == CDBVerifyOutputStatus.R.ToString())
                {
                    return false;
                }
                else if (output.obsResultFlag.Trim().ToUpper() == CDBVerifyOutputStatus.W.ToString())
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

        #region ResaleBuy or ResaleSell Added By Edward ResaleBuy/ResaleSell 04.12.2013

        private string GetFilePathAndName(BE01JBusinessInfoDTO businessInfo,string ResaleType, int attemptCount)
        {
            if (!TriggerTest)
            {
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.VerifyDWMSToCDBLogFileName, ResaleType + "_" + businessInfo.businessRefNumber, Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss), attemptCount));
            }
            else
            {
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.VerifyDWMSToCDBLogFileName, "Xml_Input", Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss), attemptCount));
            }
        }


        private static bool CheckRefTypeIsResale(int docAppId)
        {
            bool result = false;
            DocAppDb docApp = new DocAppDb();
            DocApp.DocAppDataTable docAppDt = docApp.GetDocAppById(docAppId);
            if (docAppDt.Rows.Count > 0)
            {
                DocApp.DocAppRow row = docAppDt.Rows[0] as DocApp.DocAppRow;

                if (row.RefType.ToUpper().Equals("RESALE"))
                    result = true;
            }

            return result;
        }

        private bool GetVerifiedDocsForResale(int docAppId, DataTable dataTable, string resaleType, ref BE01JBusinessInfoDTO businessInfo, 
            ref BE01JSystemInfoDTO systemInfo, ref BE01JAuthenticationDTO authentication, Guid verificationStaffUserId, out bool WithError)
        {
            try
            {
                WithError = false;
                DocSetDb docSetDb = new DocSetDb();

                systemInfo.fileSystemId = "DWMS";
                systemInfo.updateSystemId = "DWMS";
                systemInfo.fileDate = DateTime.Now;

                systemInfo.updateDate = DateTime.Today;
                systemInfo.updateTime = DateTime.Now;

                systemInfo.verificationUserId = docSetDb.GetUserNameByVerificationStaffId(verificationStaffUserId);
                systemInfo.completenessUserId = string.Empty;

                authentication.userName = CDBVerifyUtil.GetUserNameDWMSToCDB();
                authentication.passWord = CDBVerifyUtil.GetPasswordDWMSToCDB();

                bool hasDoc = false;
                int count;

                //Populate BusinessInfo fields
                DocAppDb docApp = new DocAppDb();
                DocApp.DocAppDataTable docAppDt = docApp.GetDocAppById(docAppId);

                if (docAppDt.Rows.Count > 0)
                {
                    DocApp.DocAppRow row = docAppDt.Rows[0] as DocApp.DocAppRow;
                    businessInfo.businessRefNumber = row.RefNo;
                    businessInfo.businessTransactionNumber = resaleType == "SE" ? "ResaleSell" : "ResaleBuy";
                }

                //copy to new datatable to include the records with the current docAppId only
                DataTable dtDocAppAndData;
                if (resaleType == "SE")
                    dtDocAppAndData = dataTable.AsEnumerable().Where(row => row.Field<string>("PersonalType") == "SE").AsDataView().ToTable();
                else
                    dtDocAppAndData = dataTable.AsEnumerable().Where(row => row.Field<string>("PersonalType") != "SE").AsDataView().ToTable();

                List<BE01JCustomerInfoDTO> customerList = new List<BE01JCustomerInfoDTO>();

                if (dtDocAppAndData.Rows.Count > 0)
                {
                    BE01JCustomerInfoDTO customer;

                    var distinctNrics = (from row in dtDocAppAndData.AsEnumerable()
                                         select row.Field<string>("Nric")).Distinct();

                    Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Found {0} Customer(s) ", distinctNrics.Count()), EventLogEntryType.Information);


                    foreach (string nric in distinctNrics)
                    {
                        DataTable dtDocsForNric;
                        int docCounter = 0;
                        bool isMainApplicant = false;
                        string nric_blank = "-"; //to display if customer NRIC is blank as -
                        if (!string.IsNullOrEmpty(nric))
                            nric_blank = nric;

                        Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Processing Customer: {0} ", nric_blank), EventLogEntryType.Information);

                        //2013-01-11, to ensure that unique docId are counted
                        foreach (DataRow dr in dtDocAppAndData.Rows)
                        {
                            if ((string)dr["Nric"] == nric && ((string)dr["PersonalType"] == "HA" || (string)dr["PersonalType"] == "BU") && (int)dr["OrderNo"] == 1)
                            {
                                isMainApplicant = true;
                                break;
                            }
                            else
                                isMainApplicant = false;
                        }
                        //isMainApplicant = false;
                        if (isMainApplicant)
                        {
                            docCounter = (from r in dtDocAppAndData.AsEnumerable()
                                          where r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId
                                          group r by r["DocId"] into g
                                          select new { DocAppId = g.Key, DocCount = g.Count() }).Count();
                        }
                        else
                        {
                            docCounter = (from r in dtDocAppAndData.AsEnumerable()
                                          where r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId &&
                                          (r.Field<string>("DocTypeCode").ToUpper() != "HLE" && r.Field<string>("DocTypeCode").ToUpper() != "SERS" && r.Field<string>("DocTypeCode").ToUpper() != "SALES" && r.Field<string>("DocTypeCode").ToUpper() != "RESALE")
                                          group r by r["DocId"] into g
                                          select new { DocAppId = g.Key, DocCount = g.Count() }).Count();
                        }


                        //2013-01-11, to get the top row to form the customer info 
                        var row = (from r in dtDocAppAndData.AsEnumerable() where r.Field<string>("Nric") == nric select r).FirstOrDefault();


                        if (row != null && docCounter > 0)
                        {

                            customer = new BE01JCustomerInfoDTO();

                            // Customer Name
                            if (!String.IsNullOrEmpty(row["Name"] as string))
                            {
                                customer.customerName = row["Name"].ToString();
                            }
                            else
                            {
                                customer.customerName = string.Empty;
                            }

                            // Customer ID No
                            if (!String.IsNullOrEmpty(row["Nric"] as string))
                            {
                                customer.identityNo = row["Nric"].ToString();
                            }
                            else
                            {
                                customer.identityNo = string.Empty;
                            }

                            // Customer ID Type
                            if (!String.IsNullOrEmpty(row["IdType"] as string))
                            {
                                customer.identityType = row["IdType"].ToString();
                            }
                            else
                            {
                                customer.identityType = string.Empty;
                            }

                            //CustomerSourceId
                            if (!String.IsNullOrEmpty(row["CustomerSourceId"] as string))
                            {
                                customer.customerIdFromSource = row["CustomerSourceId"].ToString();
                            }
                            else
                            {
                                customer.customerIdFromSource = string.Empty;
                            }

                            //Customer Type
                            if (!String.IsNullOrEmpty(row["CustomerType"] as string))
                            {
                                customer.customerType = row["CustomerType"].ToString();
                            }
                            else
                            {
                                customer.customerType = CustomerTypeEnum.P.ToString();
                            }


                            //DocCounter
                            customer.docCounter = docCounter;

                            //Get all the rows for each NRIC
                            if (isMainApplicant)
                            {
                                dtDocsForNric = dataTable.AsEnumerable().Where(r => r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId).AsDataView().ToTable();
                            }
                            else
                            {
                                dtDocsForNric = dataTable.AsEnumerable().Where(r => r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId &&
                                    (r.Field<string>("DocTypeCode").ToUpper() != "HLE" && r.Field<string>("DocTypeCode").ToUpper() != "SERS" && r.Field<string>("DocTypeCode").ToUpper() != "SALES" && r.Field<string>("DocTypeCode").ToUpper() != "RESALE")).AsDataView().ToTable();
                            }

                            if (dtDocsForNric.Rows.Count > 0)
                            {

                                //For certain documents/images there are no metadata, like the requestor info,
                                //therefore this is used to retain the customer (requester) info
                                VerifyRequestorCustomer requestedCustomer = new VerifyRequestorCustomer();
                                requestedCustomer.customerName = customer.customerName;
                                requestedCustomer.identityNo = customer.identityNo;
                                requestedCustomer.identityType = customer.identityType;
                                requestedCustomer.customerIdFromSource = customer.customerIdFromSource;

                                //Entry point to populating the Documents for each Customer
                                customer.documentInfoList = GetDocumentInfo(dtDocsForNric, requestedCustomer, docAppId, SendToCDBStageEnum.Verified, out count);
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

                return false;
            }
            catch (Exception ex)
            {
                WithError = true;
                string errorMessage = String.Format("VERIFY: Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);

                Util.CDBLog("DWMS_CDB_Service.GetVerifiedDocs()", errorMessage, EventLogEntryType.Error);

                return false;
            }
        }

        #endregion
    }
}
