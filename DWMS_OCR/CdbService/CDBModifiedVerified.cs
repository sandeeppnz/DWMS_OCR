using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Helper;
using DWMS_OCR.App_Code.Bll;
using System.Data;
using DWMS_OCR.VerifyDocWebRef;
using System.Diagnostics;
using System.IO;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.CdbService
{
    public class CDBModifiedVerified : CDBVerifyUtil
    {
        public bool TriggerTest { get; set; }
        public bool TriggerUpdateResultToDatabase { get; set; }
        public bool TriggerSendToCDBModifiedVerifiedDocs { get; set; }
        public bool XmlOutput { get; set; }
        public bool RunOnce { get; set; }

        public void SendModifiedDocsUponCompletenessChecked()
        {
            try
            {
                BE01JVerifyDocService webRef = new BE01JVerifyDocService();
              
                DocAppDb docAppDb = new DocAppDb();



                using (DataTable dt = docAppDb.GetDocAppsReadyToSendToCDB(SendToCDBStatusEnum.ModifiedInCompleteness))//, "SYSTEM"))
                {
                    if (dt.Rows.Count > 0)
                    {
                        Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Found {0} DocApps(s) with SentToCDBStaus: " + SendToCDBStatusEnum.Ready.ToString() + " and DocAppStatus=" + CompletenessStatusEnum.Completeness_Checked.ToString(), dt.Rows.Count), EventLogEntryType.Information);
                        foreach (DataRow docAppRow in dt.Rows)
                        {
                            if (((int)docAppRow["SendToCDBAttemptCount"] < CDBVerifyUtil.GetMaxAttemptToSendComplenessCheckedDocApps()) || docAppRow.IsNull("SendToCDBAttemptCount"))
                            {

                                for (int attemptCount = 0; attemptCount < CDBVerifyUtil.GetMaxAttemptToSendComplenessCheckedDocApps(); attemptCount++)
                                {
                                    bool success = SendDocAppToCDB(webRef, docAppRow, attemptCount);
                                    if (success)
                                        break;
                                }
                            }
                            else
                            {
                                Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: DocApp: {0} has exceeded the max. number of attempts to sent to CDB, therefore marked not to be processed", (int)docAppRow["Id"]), EventLogEntryType.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.SendComplenessCheckedApplications", errorMessage, EventLogEntryType.Error);
            }
        }

        private bool SendDocAppToCDB(BE01JVerifyDocService webRef, DataRow docAppRow, int attemptCount)
        {
            DocAppDb docAppDb = new DocAppDb();

            BE01JSystemInfoDTO systemInfo = new BE01JSystemInfoDTO();
            BE01JBusinessInfoDTO businessInfo = new BE01JBusinessInfoDTO();
            BE01JOutputDTO result = new BE01JOutputDTO();
            BE01JAuthenticationDTO authentication = new BE01JAuthenticationDTO();

            string filePath = string.Empty;
            bool connError = false;
            bool hasDocAppSuccessfullySentToCDB = false;



            //if the docset failed (i.e. the data retrieval, input, connection, or output had any problem), will be marked as 'Ready' 
            bool hasInputInfo = false;
            bool isDocsModifiedVerifiedFound = true;


            hasInputInfo = GetDocs(docAppRow, ref systemInfo, ref authentication, ref businessInfo, ref isDocsModifiedVerifiedFound);



            if (isDocsModifiedVerifiedFound)
            {
                Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Processing: DocAppId({0})", docAppRow.Field<int>("Id")), EventLogEntryType.Information);

                filePath = GetFilePathAndName(businessInfo, attemptCount + 1); //zero-based attempt count
                string checkedFilePath = GetFilePathAndNameInputErrorCheck(hasInputInfo, filePath);
                Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start writing input to file: " + checkedFilePath), EventLogEntryType.Information);
                GenerateInputTextFile(systemInfo, businessInfo, authentication, checkedFilePath);
                Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: End writing input to file"), EventLogEntryType.Information);


                #region send to CDB Verification
                // 3. Send data to CDB 
                if (!hasInputInfo)
                {
                    Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Retrieval of data for DocAppId({0}) failed, this DocApp will not be sent to CDB. Please check the CDB Log for details", docAppRow.Field<int>("Id")), EventLogEntryType.Error);
                    hasDocAppSuccessfullySentToCDB = false;
                }
                else //if retrieval of data from DB is ok
                {
                    if (TriggerSendToCDBModifiedVerifiedDocs)
                    {
                        Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start sending to CDB"), EventLogEntryType.Information);
                        try
                        {
                            result = webRef.verifyDocument(authentication, systemInfo, businessInfo);
                            // result = webRef.acceptDocument(authentication, systemInfo, businessInfo);
                            connError = false;

                            //if atleast once failed, will be failed at DocSet level, 
                            //i.e. it the DocSet's SentToCDBStaus should not be set to 'Sent'
                            //this is the reason for adding this flag
                            hasDocAppSuccessfullySentToCDB = true;

                            Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: Successfully sent to CDB"), EventLogEntryType.Information);

                        }
                        catch (Exception ex)
                        {
                            connError = true;
                            hasDocAppSuccessfullySentToCDB = false;
                            Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Connection to CDB attempt failed, Message: " + ex.Message + ", StackTrace: " + ex.StackTrace), EventLogEntryType.Error);
                        }
                    }
                    else
                    {
                        connError = true; //connection is diabled
                        hasDocAppSuccessfullySentToCDB = false;
                        Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Calling to CDB currently turned off in this service"), EventLogEntryType.Warning);
                    }

                    #region write connection error file
                    if (connError)
                    {
                        Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start writing error to file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                        bool resultError = WriteConnectionErrorFile(checkedFilePath);
                        Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: End writing error to file"), EventLogEntryType.Information);
                    }
                    #endregion
                }
                #endregion


                #region check the output, create output/err file, update DB
                // 4. GenerateOutputTextFile
                //the output result is returned
                if (!connError && hasInputInfo && TriggerSendToCDBModifiedVerifiedDocs)
                {
                    //Can be either an OutputError or Output file
                    string filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                    Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start writing output result to file: " + filePathOut), EventLogEntryType.Information);
                    bool resultWrite = GenerateOutputTextFile(filePathOut, checkedFilePath, result, businessInfo, systemInfo, authentication, SendToCDBStageEnum.ModifiedVerified);


                    if (hasDocAppSuccessfullySentToCDB)
                        hasDocAppSuccessfullySentToCDB = ValidateCDBForModifiedVerified(result);
                    Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: End writing output result to file sent from CDB"), EventLogEntryType.Information);
                }
                #endregion



                #region write output to xml
                if (XmlOutput)
                {
                    GenerateXmlOutput(checkedFilePath, result, businessInfo, systemInfo);
                }
                #endregion

                if (TriggerUpdateResultToDatabase)
                {
                    if (!hasDocAppSuccessfullySentToCDB)
                    {
                        docAppDb.UpdateSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.Ready, attemptCount + 1);

                    }

                    //Get the DocSets for this DocApp
                    DocSetDb docSetDb = new DocSetDb();
                    DocDb docDb = new DocDb();
                    DataTable dt = docSetDb.GetDocSetsByDocAppId((int)docAppRow["Id"]);
                    if (dt != null)
                    {
                        foreach (DataRow r in dt.Rows)
                        {
                            DataTable docTable = docDb.GetDocNotSentToCDB(r.Field<int>("DocSetId"), SendToCDBStatusEnum.Sent);
                            if (docTable.Rows.Count <= 0 || docTable == null)
                            {
                                docSetDb.UpdateSetSentToCDBStatus(r.Field<int>("DocSetId"), SendToCDBStatusEnum.Sent);
                            }

                        }
                    }
                }
            }
            else
            {
                Util.CDBDetailLog(string.Empty, String.Format("MODIFIED-VERIFIED: DocAppId({0}) has no documents to send", docAppRow.Field<int>("Id")), EventLogEntryType.Information);
            }
            return hasDocAppSuccessfullySentToCDB;

        }

        private string GetFilePathAndName(BE01JBusinessInfoDTO businessInfo, int attemptCount)
        {
            if (!TriggerTest)
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.AcceptDWMSToCDBLogFileName, businessInfo.businessRefNumber, Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss), attemptCount));
            else
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.AcceptDWMSToCDBLogFileName, "Xml_Input", Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss), attemptCount));

        }

        public void RunXmlTest()
        {
            Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start RunXmlTest"), EventLogEntryType.Information);

            string inputFilePath = CDBVerifyUtil.GetTestInputXMLForDWMSToCDBFilePathAndNameAccept();
            BE01JSystemInfoDTO systemInfo = new BE01JSystemInfoDTO();
            BE01JAuthenticationDTO authentication = new BE01JAuthenticationDTO();
            BE01JVerifyDocService webRef = new BE01JVerifyDocService();
            bool errorAtImport = false;
            BE01JBusinessInfoDTO businessInfoTest = XmlInput(inputFilePath, ref systemInfo, ref authentication);
            string filePath = string.Empty;
            BE01JOutputDTO result = new BE01JOutputDTO();
            bool writeData = false;
            string checkedFilePath = string.Empty;
            if (businessInfoTest != null)
            {
                filePath = GetFilePathAndName(businessInfoTest, 1);
                checkedFilePath = GetFilePathAndNameInputErrorCheck(true, filePath);
                Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start writing input file: " + checkedFilePath), EventLogEntryType.Information);
                writeData = GenerateInputTextFile(systemInfo, businessInfoTest, authentication, checkedFilePath);
                Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: End writing input file"), EventLogEntryType.Information);
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
                    if (TriggerSendToCDBModifiedVerifiedDocs)
                    {
                        Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start sending to CDB"), EventLogEntryType.Information);
                        result = webRef.verifyDocument(authentication, systemInfo, businessInfoTest);
                        Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: End sending to CDB"), EventLogEntryType.Information);
                    }
                    else
                    {
                        result = null;
                        Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Calling to CDB's VerifyDocument is currently configured to be OFF in this service"), EventLogEntryType.Warning);
                    }
                }
                catch
                {
                    errorAtAcceptDocument = true;
                    result = null;
                    Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start writing file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                    bool resultError = WriteConnectionErrorFile(checkedFilePath);
                    Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: End writing file"), EventLogEntryType.Information);
                    Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Failed to connect to CDB. No output file is produced and the Err file being generated"), EventLogEntryType.Error);
                }
            }
            else
            {
                Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Error at import data from text file, please correct the data. Log file(s) are not generated and no information has been sent to CDB"), EventLogEntryType.Error);
            }

            if (!errorAtAcceptDocument && !errorAtImport)
            {
                string filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: Start writing result to file: " + filePathOut), EventLogEntryType.Information);
                bool resultWrite = GenerateOutputTextFile(filePathOut, checkedFilePath, result, businessInfoTest, systemInfo, authentication, SendToCDBStageEnum.ModifiedVerified);
                Util.CDBLog(string.Empty, String.Format("MODIFIED-VERIFIED: End writing result to file"), EventLogEntryType.Information);
            }
            if (XmlOutput)
            {
                GenerateXmlOutput(checkedFilePath, result, businessInfoTest, systemInfo);
            }

        }



        private bool GetDocs(DataRow docAppRow, ref BE01JSystemInfoDTO systemInfo, ref BE01JAuthenticationDTO authentication, ref BE01JBusinessInfoDTO businessInfo, ref bool isDocsModifiedVerifiedFound)
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

                int count;
                if (!string.IsNullOrEmpty(docAppRow["CompletenessStaffUserId"].ToString()))
                {
                    Guid? staffId = (Guid?)docAppRow["CompletenessStaffUserId"];
                    if (staffId.HasValue)
                        systemInfo.verificationUserId = docAppDb.GetUserNameByCompletenessStaffUserId(staffId.Value);
                }
                else
                    systemInfo.verificationUserId = string.Empty;

                systemInfo.completenessUserId = string.Empty;



                authentication.userName = CDBVerifyUtil.GetUserNameDWMSToCDB();
                authentication.passWord = CDBVerifyUtil.GetPasswordDWMSToCDB();

                if (docAppRow != null)
                {
                    businessInfo.businessRefNumber = docAppRow["RefNo"] as string;
                    businessInfo.businessTransactionNumber = docAppRow["RefType"] as string;
                }

                DataTable docData = docDb.GetModifiedDocDetails(docAppId, DocStatusEnum.Verified.ToString(), DocStatusEnum.Completed.ToString(), ImageConditionEnum.NA.ToString(), DocTypeEnum.Miscellaneous.ToString(), SendToCDBStatusEnum.ModifiedInCompleteness.ToString(), SendToCDBStatusEnum.ModifiedInCompleteness.ToString(), SendToCDBStatusEnum.ModifiedInCompleteness.ToString());


                if (docData.Rows.Count > 0)
                {
                    var distinctNrics = (from r in docData.AsEnumerable()
                                         select r.Field<string>("Nric")).Distinct();

                    Util.CDBDetailLog(string.Empty, String.Format("Found {0} Customer(s) ", distinctNrics.Count()), EventLogEntryType.Information);
                    List<BE01JCustomerInfoDTO> customerList = new List<BE01JCustomerInfoDTO>();

                    foreach (string nric in distinctNrics)
                    {
                        string nric_blank = "-"; //to display if customer NRIC is blank as -
                        if (!string.IsNullOrEmpty(nric))
                            nric_blank = nric;

                        Util.CDBDetailLog(string.Empty, String.Format("Processing Customer: {0} ", nric_blank), EventLogEntryType.Information);


                        //2013-01-11, to ensure that unique docId are counted
                        int docCounter = (from r in docData.AsEnumerable()
                                          where r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId
                                          group r by r["DocId"] into g
                                          select new { DocAppId = g.Key, DocCount = g.Count() }).Count();
                        //2013-01-11, to get the top row to form the customer info 
                        var customerRow = (from r in docData.AsEnumerable() where r.Field<string>("Nric") == nric select r).FirstOrDefault();

                        if (customerRow != null)
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
                            DataTable dtDocsForNric = docData.AsEnumerable().Where(r => r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId).AsDataView().ToTable();
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
                                customer.documentInfoList = GetDocumentInfo(dtDocsForNric, requestedCustomer, docAppId, SendToCDBStageEnum.ModifiedVerified, out count);
                                customer.docCounter = count;
                            }
                            else
                            {
                                customer.documentInfoList = null;
                            }

                            customerList.Add(customer);
                        }
                    }
                    businessInfo.customerInfoList = customerList.ToArray();
                    return true;
                }

                isDocsModifiedVerifiedFound = false;
                return false;


            }
            catch (Exception ex)
            {

                string errorMessage = String.Format("Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);

                Util.CDBLog("ModifiedVerified.GetDocs()", errorMessage, EventLogEntryType.Error);
                isDocsModifiedVerifiedFound = false;
                return false;
            }
        }



        private string GetOutputFilePathAndName(string filePath, BE01JOutputDTO output)
        {
            if (ValidateCDBForModifiedVerified(output))
                return filePath + ".Out";
            else
                return filePath + ".OutErr";
        }

        private string GetFilePathAndNameInputErrorCheck(bool hasBusinessInfo, string filePath)
        {
            if (hasBusinessInfo)
                return filePath;
            else
                return filePath + ".InpErr";

        }


        private static bool ValidateCDBForModifiedVerified(BE01JOutputDTO output)
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
