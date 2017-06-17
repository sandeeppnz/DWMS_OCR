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
    public class CDBAccept : CDBUtil
    {
        public bool TriggerTest { get; set; }
        public bool TriggerUpdateResultToDatabase { get; set; }
        public bool TriggerSendToCDBAccept { get; set; }
        public bool XmlOutput { get; set; }
        public bool RunOnce { get; set; }

        public void SendComplenessCheckedApplications()
        {
            try
            {
                BE01JVerifyDocService webRef = new BE01JVerifyDocService();
                //DocSetDb docSetDb = new DocSetDb();
                DocAppDb docAppDb = new DocAppDb();



                using (DataTable dt = docAppDb.GetCompletenessCheckedDocAppsReadyToSendToCDB(SendToCDBStatusEnum.Ready, CompletenessStatusEnum.Completeness_Checked, "SYSTEM"))
                {

                    Util.CDBLog(string.Empty, String.Format("ACCEPT: Found {0} DocApps(s) with SentToCDBStaus: " + SendToCDBStatusEnum.Ready.ToString() + " and DocAppStatus=" + CompletenessStatusEnum.Completeness_Checked.ToString(), dt.Rows.Count), EventLogEntryType.Information);
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow docAppRow in dt.Rows)
                        {
                            for (int attemptCount = 0; attemptCount < CDBUtil.GetMaxAttemptToSendComplenessCheckedDocApps(); attemptCount++)
                            {
                                bool success = SendComplenessCheckedApp(webRef, docAppRow, attemptCount);
                                if(success)
                                    break;
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

        private bool SendComplenessCheckedApp(BE01JVerifyDocService webRef, DataRow docAppRow, int attemptCount)
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


            //for (attemptCount = 0; attemptCount < Retrieve.GetMaxAttemptToSendToCDBEachDocAppAccept(); attemptCount++)
            //{
            Util.CDBLog(string.Empty, String.Format("ACCEPT: Processing: DocAppId({0})", docAppRow.Field<int>("Id")), EventLogEntryType.Information);

            hasInputInfo = GetCompletenessCheckedDocs(docAppRow, ref systemInfo, ref authentication, ref businessInfo);

            filePath = GetFilePathAndName(businessInfo, attemptCount + 1); //zero-based attempt count
            string checkedFilePath = GetFilePathAndNameInputErrorCheck(hasInputInfo, filePath);
            Util.CDBDetailLog(string.Empty, String.Format("ACCEPT: Start writing input to file: " + checkedFilePath), EventLogEntryType.Information);
            GenerateInputTextFile(systemInfo, businessInfo, authentication, checkedFilePath);
            Util.CDBDetailLog(string.Empty, String.Format("ACCEPT: End writing input to file"), EventLogEntryType.Information);


            #region send to CDB Verification
            // 3. Send data to CDB 
            if (!hasInputInfo)
            {
                Util.CDBLog(string.Empty, String.Format("ACCEPT: Retrieval of data for DocAppId({0}) failed, this DocApp will not be sent to CDB. Please check the CDB Log for details", docAppRow.Field<int>("Id")), EventLogEntryType.Error);
                hasDocAppSuccessfullySentToCDB = false;
            }
            else //if retrieval of data from DB is ok
            {
                if (TriggerSendToCDBAccept)
                {
                    Util.CDBDetailLog(string.Empty, String.Format("ACCEPT: Start sending to CDB"), EventLogEntryType.Information);
                    try
                    {
                        result = webRef.acceptDocument(authentication, systemInfo, businessInfo);
                        // result = webRef.acceptDocument(authentication, systemInfo, businessInfo);
                        connError = false;

                        //if atleast once failed, will be failed at DocSet level, 
                        //i.e. it the DocSet's SentToCDBStaus should not be set to 'Sent'
                        //this is the reason for adding this flag
                        hasDocAppSuccessfullySentToCDB = true;

                        Util.CDBDetailLog(string.Empty, String.Format("ACCEPT: Successfully sent to CDB"), EventLogEntryType.Information);

                    }
                    catch (Exception ex)
                    {
                        connError = true;
                        hasDocAppSuccessfullySentToCDB = false;
                        Util.CDBLog(string.Empty, String.Format("ACCEPT: Connection to CDB attempt failed, Message: " + ex.Message + ", StackTrace: " + ex.StackTrace), EventLogEntryType.Error);
                    }
                    #region commented out
                    //for (numOfAttempts = 0; numOfAttempts < Retrieve.GetMaxAttemptToSendToCDBVerifyEachDocApp(); numOfAttempts++)
                    //{
                    //    try
                    //    {
                    //        Util.CDBDetailLog(string.Empty, String.Format("Start sending to CDB"), EventLogEntryType.Information);
                    //        result = webRef.verifyDocument(authentication, systemInfo, businessInfo);
                    //        connError = false;
                    //        Util.CDBDetailLog(string.Empty, String.Format("Successfully sent to CDB"), EventLogEntryType.Information);
                    //        break; // if exception is not thrown

                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        connError = true;
                    //        docAppDb.UpdateSentToCDBStatus(docAppId, SendToCDBStatusEnum.Ready);
                    //        Util.CDBLog(string.Empty, String.Format("Connection to CDB attempt failed, Message: " + ex.Message + ", StackTrace: "+ex.StackTrace), EventLogEntryType.Error);

                    //    }
                    //}
                    #endregion
                }
                else
                {
                    connError = true; //connection is diabled
                    hasDocAppSuccessfullySentToCDB = false;
                    Util.CDBLog(string.Empty, String.Format("ACCEPT: Calling to CDB currently turned off in this service"), EventLogEntryType.Warning);
                }

                #region write connection error file
                if (connError)
                {
                    Util.CDBDetailLog(string.Empty, String.Format("ACCEPT: Start writing error to file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                    bool resultError = WriteConnectionErrorTextFile(checkedFilePath);
                    Util.CDBDetailLog(string.Empty, String.Format("ACCEPT: End writing error to file"), EventLogEntryType.Information);
                }
                #endregion
            }
            #endregion


            #region check the output, create output/err file, update DB
            // 4. GenerateOutputTextFile
            //the output result is returned
            if (!connError && hasInputInfo && TriggerSendToCDBAccept)
            {
                //Can be either an OutputError or Output file
                string filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                Util.CDBDetailLog(string.Empty, String.Format("ACCEPT: Start writing output result to file: " + filePathOut), EventLogEntryType.Information);
                bool resultWrite = GenerateOutputTextFile(filePathOut, checkedFilePath, result, businessInfo, systemInfo, authentication);


                //if atleast once failed, will be failed at DocSet level, 
                //i.e. it the DocSet's SentToCDBStaus should not be set to 'Sent'
                // if it is true, then check the status of validation
                // if it is false, then no need to false it again.
                if (hasDocAppSuccessfullySentToCDB)
                    hasDocAppSuccessfullySentToCDB = ValidateCDBForDocAppAccept(result);
                Util.CDBDetailLog(string.Empty, String.Format("ACCEPT: End writing output result to file sent from CDB"), EventLogEntryType.Information);
            }
            #endregion



            #region write output to xml
            if (XmlOutput)
            {
                GenerateXmlOutput(checkedFilePath, result, businessInfo, systemInfo, authentication);
            }
            #endregion


            if (TriggerUpdateResultToDatabase)
            {
                if (hasDocAppSuccessfullySentToCDB)
                    docAppDb.UpdateSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.Sent, attemptCount);
                else
                    docAppDb.UpdateSentToCDBStatus((int)docAppRow["Id"], SendToCDBStatusEnum.Ready, attemptCount);
            }

            return hasDocAppSuccessfullySentToCDB;

        }

        private string GetFilePathAndName(BE01JBusinessInfoDTO businessInfo, int attemptCount)
        {
            if (!TriggerTest)
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.AcceptDWMSToCDBLogFileName, businessInfo.businessRefNumber, Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_hhmmss), attemptCount));
            else
                return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.AcceptDWMSToCDBLogFileName, "Xml_Input", Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_hhmmss)));

        }

        public void RunXmlTest()
        {
            Util.CDBLog(string.Empty, String.Format("ACCEPT: Start RunXmlTest"), EventLogEntryType.Information);

            string inputFilePath = CDBUtil.GetTestInputXMLForDWMSToCDBFilePathAndNameAccept();
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
                Util.CDBLog(string.Empty, String.Format("ACCEPT: Start writing input file: " + checkedFilePath), EventLogEntryType.Information);
                writeData = GenerateInputTextFile(systemInfo, businessInfoTest, authentication, checkedFilePath);
                Util.CDBLog(string.Empty, String.Format("ACCEPT: End writing input file"), EventLogEntryType.Information);
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
                    if (TriggerSendToCDBAccept)
                    {
                        Util.CDBLog(string.Empty, String.Format("ACCEPT: Start sending to CDB"), EventLogEntryType.Information);
                        result = webRef.acceptDocument(authentication, systemInfo, businessInfoTest);
                        Util.CDBLog(string.Empty, String.Format("ACCEPT: End sending to CDB"), EventLogEntryType.Information);
                    }
                    else
                    {
                        result = null;
                        Util.CDBLog(string.Empty, String.Format("ACCEPT: Calling to CDB's VerifyDocument is currently configured to be OFF in this service"), EventLogEntryType.Warning);
                    }
                }
                catch
                {
                    errorAtAcceptDocument = true;
                    result = null;
                    Util.CDBLog(string.Empty, String.Format("ACCEPT: Start writing file: " + checkedFilePath + ".ConnErr"), EventLogEntryType.Information);
                    bool resultError = WriteConnectionErrorTextFile(checkedFilePath);
                    Util.CDBLog(string.Empty, String.Format("ACCEPT: End writing file"), EventLogEntryType.Information);
                    Util.CDBLog(string.Empty, String.Format("ACCEPT: Failed to connect to CDB. No output file is produced and the Err file being generated"), EventLogEntryType.Error);
                }
            }
            else
            {
                Util.CDBLog(string.Empty, String.Format("ACCEPT: Error at import data from text file, please correct the data. Log file(s) are not generated and no information has been sent to CDB"), EventLogEntryType.Error);
            }

            if (!errorAtAcceptDocument && !errorAtImport)
            {
                string filePathOut = GetOutputFilePathAndName(checkedFilePath, result);
                Util.CDBLog(string.Empty, String.Format("ACCEPT: Start writing result to file: " + filePathOut), EventLogEntryType.Information);
                bool resultWrite = GenerateOutputTextFile(filePathOut, checkedFilePath, result, businessInfoTest, systemInfo, authentication);
                Util.CDBLog(string.Empty, String.Format("ACCEPT: End writing result to file"), EventLogEntryType.Information);
            }
            if (XmlOutput)
            {
                GenerateXmlOutput(checkedFilePath, result, businessInfoTest, systemInfo, authentication);
            }

        }



        private bool GetCompletenessCheckedDocs(DataRow docAppRow, ref BE01JSystemInfoDTO systemInfo, ref BE01JAuthenticationDTO authentication, ref BE01JBusinessInfoDTO businessInfo)
        {

            try
            {
                int docAppId = (int)docAppRow["Id"];
                int docSetId;


                DocSetDb docSetDb = new DocSetDb();
                DocDb docDb = new DocDb();
                DocAppDb docAppDb = new DocAppDb();

                systemInfo.fileSystemId = "DWMS";
                systemInfo.updateSystemId = "DWMS";
                systemInfo.fileDate = DateTime.Now;

                systemInfo.updateDate = DateTime.Today;
                systemInfo.updateTime = DateTime.Now;

                //TODO: is it valid to send empty verificationId
                systemInfo.verificationUserId = string.Empty;

                Guid? staffId = (Guid?)docAppRow["CompletenessStaffUserId"];
                if (staffId.HasValue)
                    systemInfo.completenessUserId = docAppDb.GetUserNameByCompletenessStaffUserId(staffId.Value);
                else
                    systemInfo.completenessUserId = string.Empty;

                authentication.userName = CDBUtil.GetUserNameDWMSToCDB();
                authentication.passWord = CDBUtil.GetPasswordDWMSToCDB();

                if (docAppRow != null)
                {
                    businessInfo.businessRefNumber = docAppRow["RefNo"] as string;
                    businessInfo.businessTransactionNumber = docAppRow["RefType"] as string;
                }


                DataTable dtDocAppAndDocSetData = docSetDb.GetDocSetData(docAppId, CompletenessStatusEnum.Completeness_Checked, SendToCDBStatusEnum.Ready);
                List<BE01JCustomerInfoDTO> customerList = new List<BE01JCustomerInfoDTO>();



                if (dtDocAppAndDocSetData.Rows.Count > 0)
                {
                    Util.CDBLog(string.Empty, String.Format("Found {0} DocSet(s)", dtDocAppAndDocSetData.Rows.Count), EventLogEntryType.Information);

                    //For each DocSet

                    foreach (DataRow row in dtDocAppAndDocSetData.Rows)
                    {

                        docSetId = (int)row["DocSetId"];

                        Util.CDBLog(string.Empty, String.Format("Processing: DocApp({0}), DocSet({1}) ", row.Field<int>("Id"), row.Field<int>("DocSetId")), EventLogEntryType.Information);
                        DataTable docData = docDb.GetCompletedDocDetails(docSetId, DocStatusEnum.Completed.ToString(), ImageConditionEnum.NA.ToString(), DocTypeEnum.Miscellaneous.ToString(), SendToCDBStatusEnum.Sent.ToString());

                        


                        if (docData.Rows.Count > 0)
                        {
                            var distinctNrics = (from r in docData.AsEnumerable()
                                                 select r.Field<string>("Nric")).Distinct();

                            Util.CDBDetailLog(string.Empty, String.Format("Found {0} Customer(s) ", distinctNrics.Count()), EventLogEntryType.Information);

                            foreach (string nric in distinctNrics)
                            {
                                string nric_blank = "-"; //to display if customer NRIC is blank as -
                                if (!string.IsNullOrEmpty(nric))
                                    nric_blank = nric;

                                Util.CDBDetailLog(string.Empty, String.Format("Processing Customer: {0} ", nric_blank), EventLogEntryType.Information);


                                //2013-01-11, to ensure that unique docId are counted
                                int docCounter = (from r in docData.AsEnumerable()
                                                  where r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId && r.Field<int>("DocSetId") == docSetId
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
                                    DataTable dtDocsForNric = docData.AsEnumerable().Where(r => r.Field<string>("Nric") == nric && r.Field<int>("DocAppId") == docAppId && r.Field<int>("DocSetId") == docSetId).AsDataView().ToTable();
                                    if (dtDocsForNric.Rows.Count > 0)
                                    {

                                        //For certain documents/images there are no metadata, like the requestor info,
                                        //therefore this is used to retain the customer (requester) info
                                        RequestorCustomer requestedCustomer = new RequestorCustomer();
                                        requestedCustomer.customerName = customer.customerName;
                                        requestedCustomer.identityNo = customer.identityNo;
                                        requestedCustomer.identityType = customer.identityType;
                                        requestedCustomer.customerIdFromSource = customer.customerIdFromSource;

                                        //Entry point to populating the Documents for each Customer
                                        customer.documentInfoList = GetDocumentInfo(dtDocsForNric, requestedCustomer, docAppId);

                                    }
                                    else
                                    {
                                        customer.documentInfoList = null;
                                    }

                                    customerList.Add(customer);
                                }
                            }
                            //businessInfo.customerInfoList = customerList.ToArray();
                            //return true;
                        }
                    }
                }
                businessInfo.customerInfoList = customerList.ToArray();
                return true;
            }
            catch (Exception ex)
            {

                string errorMessage = String.Format("Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);

                Util.CDBLog("DWMS_CDB_Service.GetCompletenessCheckedDocs()", errorMessage, EventLogEntryType.Error);

                return false;
            }
        }



        private string GetOutputFilePathAndName(string filePath, BE01JOutputDTO output)
        {
            if (ValidateCDBForDocAppAccept(output))
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


        private static bool ValidateCDBForDocAppAccept(BE01JOutputDTO output)
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
