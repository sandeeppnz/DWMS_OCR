using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using DWMS_OCR.App_Code.Helper;
using DWMS_OCR.VerifyDocWebRef;
//using DWMS_OCR.AcceptDocWebRef;
using DWMS_OCR.App_Code.Bll;
using DWMS_OCR.App_Code.Dal;
using System.Xml;
using System.IO;

namespace DWMS_OCR.CdbService
{
    public class RequestorCustomer
    {
        public string customerName { get; set; }
        public string identityNo { get; set; }
        public string identityType { get; set; }
        public string customerIdFromSource { get; set; }
    }

    public class SendToCDB
    {
        //public BE01JAuthenticationDTO BE01JAuthenticationDTO;
        public BE01JSystemInfoDTO BE01JSystemInfoDTO;
        public BE01JBusinessInfoDTO BE01JBusinessInfoDTO;
        public BE01JOutputDTO BE01JOutputDTO;
    }

    public class CDBUtil
    {
        #region CDB->DWMS configurations settings

        public static bool RunVerify()
        {
            return (ConfigurationManager.AppSettings["RunVerify"].Trim().ToUpper() == "TRUE");
        }
        public static bool ModifiedVerified()
        {
            return (ConfigurationManager.AppSettings["RunModifiedVerify"].Trim().ToUpper() == "TRUE");
        }
        public static bool RunCompleteAccept()
        {
            return (ConfigurationManager.AppSettings["RunCompleteAccept"].Trim().ToUpper() == "TRUE");
        }


        public static string GetUserNameDWMSToCDB()
        {
            return ConfigurationManager.AppSettings["UserNameDWMSToCDB"].Trim();
        }
        public static string GetPasswordDWMSToCDB()
        {
            return ConfigurationManager.AppSettings["PasswordDWMSToCDB"].Trim();
        }

        public static bool isTestRun()
        {
            return (ConfigurationManager.AppSettings["isTestRun"].Trim().ToUpper() == "TRUE");
        }

        public static bool isSendToCDBVerify()
        {
            return (ConfigurationManager.AppSettings["isSendToCDBVerify"].Trim().ToUpper() == "TRUE");
        }
        public static bool isSendToCDBAccept()
        {
            return (ConfigurationManager.AppSettings["isSendToCDBModifiedVerify"].Trim().ToUpper() == "TRUE");
        }
        public static bool isSendToCDBCompleteAccept()
        {
            return (ConfigurationManager.AppSettings["isSendToCDBCompleteAccept"].Trim().ToUpper() == "TRUE");
        }



        public static bool isWriteXMLOuput()
        {
            return (ConfigurationManager.AppSettings["isWriteXMLOuput"].Trim().ToUpper() == "TRUE");
        }


        public static string isDetailLog()
        {
            return ConfigurationManager.AppSettings["isDetailLog"].ToString();
        }


        public static string GetDownloadImagePageURL()
        {
            return ConfigurationManager.AppSettings["DownloadImagePageURL"].ToString();
        }


        public static bool isTestXMLInputVerify()
        {
            return (ConfigurationManager.AppSettings["isTestXMLInputVerify"].Trim().ToUpper() == "TRUE");
        }
        public static bool isTestXMLInputAccept()
        {
            return (ConfigurationManager.AppSettings["isTestXMLInputModifiedVerify"].Trim().ToUpper() == "TRUE");
        }

        public static bool isTestXMLInputCompleteAccept()
        {
            return (ConfigurationManager.AppSettings["isTestXMLInputCompleteAccept"].Trim().ToUpper() == "TRUE");
        }

        public static string GetTestInputXMLForDWMSToCDBFilePathAndNameVerify()
        {
            return ConfigurationManager.AppSettings["TestInputXMLForDWMSToCDBFilePathAndNameVerify"].ToString();
        }
        public static string GetTestInputXMLForDWMSToCDBFilePathAndNameAccept()
        {
            return ConfigurationManager.AppSettings["TestInputXMLForDWMSToCDBFilePathAndNameModifiedVerify"].ToString();
        }

        public static string GetTestInputXMLForDWMSToCDBFilePathAndNameCompleteAccept()
        {
            return ConfigurationManager.AppSettings["TestInputXMLForDWMSToCDBFilePathAndNameCompleteAccept"].ToString();
        }



        public static bool isCDBUpdateResultToDBVerify()
        {
            return (ConfigurationManager.AppSettings["isCDBUpdateResultToDBVerify"].Trim().ToUpper() == "TRUE");
        }

        public static bool isCDBUpdateResultToDBAccept()
        {
            return (ConfigurationManager.AppSettings["isCDBUpdateResultToDBModifiedVerify"].Trim().ToUpper() == "TRUE");
        }

        public static bool isCDBUpdateResultToDBCompleteAccept()
        {
            return (ConfigurationManager.AppSettings["isCDBUpdateResultToDBCompleteAccept"].Trim().ToUpper() == "TRUE");
        }





        public static int GetMaxAttemptAllowedForVerifiedDocSets()
        {
            return Convert.ToInt32(ConfigurationManager.AppSettings["maxAttempstToSendToCDBEachDocAppVerify"].ToString());
        }
        public static int GetMaxAttemptToSendComplenessCheckedDocApps()
        {
            return Convert.ToInt32(ConfigurationManager.AppSettings["maxAttempstToSendToCDBEachDocAppModifiedVerify"].ToString());
        }
        public static int GetMaxAttemptToSendCompletedDocApps()
        {
            return Convert.ToInt32(ConfigurationManager.AppSettings["maxAttempstToSendToCDBEachDocAppCompleteAccept"].ToString());
        }

        #endregion



        protected bool WriteConnectionErrorFile(string filePath)
        {
            try
            {
                // will be renaming the original input file to .ConnErr 
                File.Move(filePath, filePath + ".ConnErr");
                return true;
            }
            catch (Exception ex)
            {

                string errorMessage = String.Format("WriteErrorToTextFile() Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.WriteErrorToTextFile()", errorMessage, EventLogEntryType.Error);

                return false;
            }
        }

        protected bool ProcessOutput(BE01JOutputDTO response, BE01JBusinessInfoDTO businessInfo, BE01JSystemInfoDTO systemInfo, BE01JAuthenticationDTO authentication, SendToCDBStageEnum stage)
        {
            DocDb docDb = new DocDb();
            DocSetDb docSetDb = new DocSetDb();
            DocAppDb docAppDb = new DocAppDb();
            //bool haserror = false;
            string channelType = "";

            //string datePattern = "dd MMM yyyy hh:mm:ss tt";
            try
            {
                if (response != null)
                {
                    //these flags need to be removed, but is used for logging purposes
                    //bool isDocAppSuccessfullyFilled = true;

                    if (response.businessOutput != null)
                    {
                        BE01BusinessOutput businessOutput = response.businessOutput;

                        //Util.CDBLog("HAHA", "in business start", EventLogEntryType.Warning);
                        //Util.CDBLog("HAHA", "in business end", EventLogEntryType.Warning);
                        if (businessOutput.customerOutputList != null)
                        {
                            int customerCount = 0;

                            //Util.CDBLog("HAHA", "in customer start", EventLogEntryType.Warning);
                            foreach (BE01CustomerOutput customerOutput in businessOutput.customerOutputList)
                            {
                                //Util.CDBLog("HAHA", "in customer loop", EventLogEntryType.Warning);

                                BE01JCustomerInfoDTO customerInfo = businessInfo.customerInfoList[customerCount];
                                if (customerOutput.documentOutputList != null)
                                {
                                    //Business >> Customer >> documents
                                    int documentCount = 0;

                                    //Util.CDBLog("HAHA", "in document start", EventLogEntryType.Warning);
                                    foreach (BE01DocumentOutput documentOutput in customerOutput.documentOutputList)
                                    {
                                        try
                                        {
                                            //Util.CDBLog("HAHA", "in document loop customer: " + customerCount + "Document: " + documentCount, EventLogEntryType.Warning);
                                            BE01JDocumentInfoDTO documentInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount];
                                            //bool isAllImagesSuccessfullyFilled = true;
                                            if (documentOutput.imageOutputList != null)
                                            {
                                                if (documentOutput.imageOutputList.Count() != documentInfo.imageInfoList.Count())
                                                {
                                                    // TODO
                                                    // ExceptionLogDb exceptionDb = new ExceptionLogDb();
                                                    //exceptionDb.LogException(null,
                                                    Util.CDBLog("", "Number of documents sent is not same as recieved", EventLogEntryType.Error);
                                                }

                                                int imageCount = 0;
                                                //Business >> Customer >> documents >> Image
                                                foreach (BE01ImageOutput imageOutput in documentOutput.imageOutputList)
                                                {
                                                    try
                                                    {
                                                        switch (documentInfo.docChannel)
                                                        {
                                                            //Only requested Customer
                                                            case "001":
                                                                channelType = "MyDoc";
                                                                break;
                                                            case "002":
                                                                channelType = "MyHDBPage";
                                                                break;
                                                            case "003":
                                                                channelType = "Scan";
                                                                break;
                                                            case "004":
                                                                channelType = "Fax";
                                                                break;
                                                            case "005":
                                                                channelType = "Email";
                                                                break;
                                                            case "006":
                                                                channelType = "Deposit Box";
                                                                break;
                                                            case "007":
                                                                channelType = "Hardcopy Mail";
                                                                break;
                                                            case "008":
                                                                channelType = "Counter";
                                                                break;
                                                            case "009":
                                                                channelType = "Mixed";
                                                                break;
                                                            default:
                                                                channelType = "Err No Doc Channel";
                                                                break;
                                                        }
                                                        BE01JImageInfoDTO imageInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount];

                                                        //Util.CDBLog(string.Empty, "Update status", EventLogEntryType.Warning);
                                                        #region update the status based on image output
                                                        // TODO: Email 
                                                        if (stage.ToString().ToUpper() == "VERIFIED" && isCDBUpdateResultToDBVerify())
                                                        {
                                                            if (imageOutput.resultFlg)
                                                            {
                                                                int docId = int.Parse(imageOutput.dwmsDocumentId);
                                                                //Util.CDBLog("HAHA", "in image loop document: " + docId + "cmDocumentId: " + imageOutput.cmDocumentId, EventLogEntryType.Warning);
                                                                docDb.UpdateCmDocumentId(docId, imageOutput.cmDocumentId);
                                                                docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.Sent);
                                                            }
                                                            else
                                                            {
                                                                int docId = int.Parse(imageOutput.dwmsDocumentId);
                                                                //haserror = true;
                                                                //Util.CDBLog("HAHA", "in image loop document: " + docId + "cmDocumentId: " + imageOutput.cmDocumentId, EventLogEntryType.Warning);
                                                                //docDb.UpdateCmDocumentId(docId, string.Empty);

                                                                #region Email
                                                                // TODO: Email part
                                                                //ExceptionLogDb exceptionLogDb = new ExceptionLogDb();

                                                                // TODO : to be removed
                                                                //string channelType = "";

                                                                //Util.CDBLog(string.Empty, "Call exception", EventLogEntryType.Warning);
                                                                //exceptionLogDb.LogCDBException(null, channelType, businessInfo.businessRefNumber, channelType, imageOutput.errorCode, imageOutput.errorMessage, true, SendToCDBStageEnum.Verified);
                                                                #endregion

                                                                Util.CDBDetailLog(string.Empty, String.Format("Stage " + stage.ToString() + " Document Id: " + imageOutput.dwmsDocumentId + " imageOutput Result:" + imageOutput.resultFlg + " updated to " + SendToCDBStatusEnum.SentButFailed.ToString()), EventLogEntryType.Information);
                                                                docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.SentButFailed);
                                                            }
                                                        }
                                                        else if (stage.ToString().ToUpper() == "MODIFIEDVERIFIED" && isCDBUpdateResultToDBAccept())
                                                        {
                                                            if (imageOutput.resultFlg)
                                                            {
                                                                int docId = int.Parse(imageOutput.dwmsDocumentId);
                                                                docDb.UpdateCmDocumentId(docId, imageOutput.cmDocumentId);
                                                                docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.Sent);
                                                            }
                                                            else
                                                            {
                                                                int docId = int.Parse(imageOutput.dwmsDocumentId);
                                                                //haserror = true;
                                                                //docDb.UpdateCmDocumentId(docId, string.Empty);

                                                                // TODO: Email part
                                                                //ExceptionLogDb exceptionLogDb = new ExceptionLogDb();

                                                                // TODO : to be removed
                                                                //switch (documentInfo.docChannel)
                                                                //{
                                                                //    //Only requested Customer
                                                                //    case "001":
                                                                //        channelType = "MyDoc";
                                                                //        break;
                                                                //    case "002":
                                                                //        channelType = "MyHDBPage";
                                                                //        break;
                                                                //    case "003":
                                                                //        channelType = "Scan";
                                                                //        break;
                                                                //    case "004":
                                                                //        channelType = "Fax";
                                                                //        break;
                                                                //    case "005":
                                                                //        channelType = "Email";
                                                                //        break;
                                                                //    case "006":
                                                                //        channelType = "Deposit Box";
                                                                //        break;
                                                                //    case "007":
                                                                //        channelType = "Hardcopy Mail";
                                                                //        break;
                                                                //    case "008":
                                                                //        channelType = "Counter";
                                                                //        break;
                                                                //    case "009":
                                                                //        channelType = "Mixed";
                                                                //        break;
                                                                //    default:
                                                                //        channelType = "Err No Doc Channel";
                                                                //        break;
                                                                //}
                                                                //exceptionLogDb.LogCDBException(null, channelType, businessInfo.businessRefNumber, channelType, imageOutput.errorCode, imageOutput.errorMessage, true, SendToCDBStageEnum.ModifiedVerified);

                                                                Util.CDBDetailLog(string.Empty, String.Format("Stage " + stage.ToString() + " Document Id: " + imageOutput.dwmsDocumentId + " imageOutput Result:" + imageOutput.resultFlg + " updated to " + SendToCDBStatusEnum.ModifiedSetSentButFailed.ToString()), EventLogEntryType.Information);
                                                                docDb.UpdateSentToCDBStatus(Convert.ToInt32(documentInfo.docId.Trim()), SendToCDBStatusEnum.ModifiedSetSentButFailed);

                                                                int? docSetId = docDb.GetDocSetIdByDocId(Convert.ToInt32(imageOutput.dwmsDocumentId.Trim()));
                                                                if (docSetId.HasValue)
                                                                    docSetDb.UpdateSetSentToCDBStatus(docSetId.Value, SendToCDBStatusEnum.ModifiedSetSentButFailed);
                                                            }
                                                        }
                                                        else if (stage.ToString().ToUpper() == "ACCEPT" && isCDBUpdateResultToDBCompleteAccept())
                                                        {
                                                            if (imageOutput.resultFlg)
                                                            {
                                                                // TODO: Email part
                                                                //int docId = int.Parse(documentInfo.docId);
                                                                //docDb.UpdateCmDocumentId(docId, string.Empty);
                                                                //    // TODO: 
                                                                //    //ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                                                                //    //exceptionLogDb.LogCDBException(null,  ,refNo, subDirInfo.Name, error, exception, true);
                                                                //docDb.UpdateSentToCDBStatus(Convert.ToInt32(documentInfo.docId.Trim()), SendToCDBStatusEnum.Sent);
                                                            }
                                                            else
                                                            {

                                                            }

                                                        }
                                                        #endregion


                                                        #region person
                                                        if (imageOutput.personOutputList != null)
                                                        {
                                                            int personCount = 0;
                                                            //Business >> Customer >> documents >> Image >> Person
                                                            //Util.CDBLog("HAHA", "in personIdentity start", EventLogEntryType.Warning);

                                                            foreach (BE01PersonOutput personOutput in imageOutput.personOutputList)
                                                            {
                                                                //Util.CDBLog("HAHA", "in personIdentity loop", EventLogEntryType.Warning);

                                                                BE01JPersonIdentityInfoDTO personIdentityInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount].personInfoList[personCount];
                                                                if (personIdentityInfo.personInfo != null)
                                                                {
                                                                    //Util.CDBLog("HAHA", "in person start", EventLogEntryType.Warning);

                                                                    BE01JPersonInfoDTO personInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount].personInfoList[personCount].personInfo;

                                                                    //Util.CDBLog("HAHA", "in person end", EventLogEntryType.Warning);
                                                                }
                                                                else
                                                                {
                                                                }

                                                                personCount++;
                                                                //Util.CDBLog("HAHA", "in personIdentity end", EventLogEntryType.Warning);
                                                            }
                                                        }
                                                        else
                                                        {
                                                        }
                                                        #endregion
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Util.CDBLog(string.Empty, String.Format("Error while deleting the input file, both input and output file will be available, Message: " + ex.Message), EventLogEntryType.Warning);
                                                    }
                                                    imageCount++;
                                                }
                                            }
                                            else
                                            {
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Util.CDBLog(string.Empty, String.Format("Error while deleting the input file, both input and output file will be available, Message: " + ex.Message), EventLogEntryType.Warning);
                                        }
                                        documentCount++;
                                    }
                                }
                                else
                                {
                                }
                                customerCount++;
                            }
                        }
                        else
                        {
                        }
                    }

                    try
                    {
                        //delete the input file, because the Output file has both input and output info. This has been done in order to ease tracing
                        //As the either and OutErr or Out has been created, the input file is deleted
                        //File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Util.CDBLog(string.Empty, String.Format("Error while deleting the input file, both input and output file will be available, Message: " + ex.Message), EventLogEntryType.Warning);
                    }
                    if (response.obsResultFlag.Trim().ToUpper() != CDBVerifyOutputStatus.A.ToString())
                    {
                        // TODO: Email part
                        ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                        string errorReason = "Doc send to CDB failed.";
                        string errorMessage = response.obsErrorCode + " - " + response.obsServerErrorMessage;

                        // TODO : to be removed
                        exceptionLogDb.LogCDBException(null, channelType, businessInfo.businessRefNumber, channelType, errorReason, errorMessage, true, stage);
                    }
                    return true; // successfuly processesd
                }
                else
                {
                    Util.CDBLog(string.Empty, String.Format("No output file can be produced, connection to CDB is configured OFF"), EventLogEntryType.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {

                string errorMessage = String.Format("GenerateOutputTextFile() Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.GenerateOutputTextFile()", errorMessage, EventLogEntryType.Error);
                return false;
            }
        }

        //protected bool ProcessOutput(string filePathOut, string filePath, BE01JOutputDTO response, BE01JBusinessInfoDTO businessInfo, BE01JSystemInfoDTO systemInfo, BE01JAuthenticationDTO authentication, SendToCDBStageEnum stage)
        //{

        //    DocDb docDb = new DocDb();
        //    DocSetDb docSetDb = new DocSetDb();
        //    DocAppDb docAppDb = new DocAppDb();

        //    string datePattern = "dd MMM yyyy hh:mm:ss tt";
        //    try
        //    {

        //        if (response != null)
        //        {
        //            //these flags need to be removed, but is used for logging purposes
        //            //bool isDocAppSuccessfullyFilled = true;

        //            StreamWriter w;
        //            //Create either a Output or OutputError file

        //            w = File.CreateText(filePathOut); //will create either a .Out or .OutErr depending on the filepath

        //            w.Write("---------START-----------------------" + "\r\n");
        //            w.Write("\r\n");
        //            w.Write("--------------------------------" + "\r\n");
        //            w.Write("BE01JAuthenticationDTO" + "\r\n");
        //            w.Write("--------------------------------" + "\r\n");
        //            w.Write("userName: " + authentication.userName + "\r\n");
        //            w.Write("passWord: " + authentication.passWord + "\r\n");
        //            w.Write("--------------------------------" + "\r\n");
        //            w.Write("\r\n");

        //            w.Write("--------------------------------\r\n");
        //            w.Write("BE01JSystemInfoDTO:" + "\r\n");
        //            w.Write("--------------------------------\r\n");
        //            w.Write("fileSystemId: " + systemInfo.fileSystemId + "\r\n");
        //            w.Write("fileDate: " + systemInfo.fileDate.Value.ToString(datePattern) + "\r\n");
        //            w.Write("completenessUserId: " + systemInfo.completenessUserId + "\r\n");
        //            w.Write("updateDate: " + systemInfo.updateDate.Value.ToString(datePattern) + "\r\n");
        //            w.Write("updateSystemId: " + systemInfo.updateSystemId + "\r\n");
        //            w.Write("updateTime: " + systemInfo.updateTime.Value.ToString(datePattern) + "\r\n");
        //            w.Write("verificationUserId: " + systemInfo.verificationUserId + "\r\n");
        //            w.Write("--------------------------------" + "\r\n");
        //            w.Write("BE01JOutputDTO:" + "\r\n");
        //            w.Write("--------------------------------" + "\r\n");
        //            w.Write("obsResultFlag: " + response.obsResultFlag.ToString() + "\r\n");
        //            w.Write("obsErrorCode: " + response.obsErrorCode.ToString() + "\r\n");
        //            w.Write("obsServerErrorMessage: " + response.obsServerErrorMessage.ToString() + "\r\n");
        //            w.Write("--------------------------------\r\n");
        //            w.Write("--------------------------------\r\n");
        //            w.Write("\r\n");
        //            w.Write("\r\n");

        //            w.Write("--------------------------------\r\n");
        //            w.Write("BE01JBusinessInfoDTO" + "\r\n");
        //            w.Write("--------------------------------\r\n");

        //            if (response.businessOutput != null)
        //            {


        //                BE01BusinessOutput businessOutput = response.businessOutput;

        //                //Util.CDBLog("HAHA", "in business start", EventLogEntryType.Warning);


        //                w.Write("businessRefNumber: " + businessInfo.businessRefNumber + "\r\n");
        //                w.Write("businessTransactionNumber: " + businessInfo.businessTransactionNumber + "\r\n");
        //                w.Write("--------------------------------" + "\r\n");
        //                w.Write("BE01BusinessOutput" + "\r\n");
        //                w.Write("--------------------------------" + "\r\n");
        //                w.Write("resultFlg: " + businessOutput.resultFlg + "\r\n");
        //                w.Write("errorCode: " + businessOutput.errorCode + "\r\n");
        //                w.Write("errorMessage: " + businessOutput.errorMessage + "\r\n");

        //                //Util.CDBLog("HAHA", "in business end", EventLogEntryType.Warning);


        //                if (businessOutput.customerOutputList != null)
        //                {
        //                    int customerCount = 0;

        //                    //Util.CDBLog("HAHA", "in customer start", EventLogEntryType.Warning);

        //                    foreach (BE01CustomerOutput customerOutput in businessOutput.customerOutputList)
        //                    {
        //                        //Util.CDBLog("HAHA", "in customer loop", EventLogEntryType.Warning);


        //                        w.Write("~~~~~~~~~BE01JCustomerInfoDTO~~~~~~~~~\r\n");
        //                        BE01JCustomerInfoDTO customerInfo = businessInfo.customerInfoList[customerCount];

        //                        w.Write("customerIdFromSource: " + customerInfo.customerIdFromSource + "\r\n");
        //                        w.Write("customerName: " + customerInfo.customerName + "\r\n");
        //                        w.Write("docCounter: " + customerInfo.docCounter + "\r\n");
        //                        w.Write("identityNo: " + customerInfo.identityNo + "\r\n");
        //                        w.Write("identityType: " + customerInfo.identityType + "\r\n");
        //                        w.Write("customerType: " + customerInfo.customerType + "\r\n");

        //                        w.Write("~~~~~~~~~BE01CustomerOutput~~~~~~~~~\r\n");
        //                        w.Write("customerIdFromSource: " + customerOutput.customerIdFromSource + "\r\n"); //
        //                        w.Write("resultFlg: " + customerOutput.resultFlg + "\r\n");
        //                        w.Write("errorCode: " + customerOutput.errorCode + "\r\n");
        //                        w.Write("errorMessage: " + customerOutput.errorMessage + "\r\n");
        //                        w.Write("\r\n");

        //                        if (customerOutput.documentOutputList != null)
        //                        {
        //                            //Business >> Customer >> documents
        //                            int documentCount = 0;

        //                            //Util.CDBLog("HAHA", "in document start", EventLogEntryType.Warning);


        //                            foreach (BE01DocumentOutput documentOutput in customerOutput.documentOutputList)
        //                            {

        //                                w.Write("~~~~~~~~BE01JDocumentInfoDTO~~~~~~~~~~~" + "\r\n");

        //                                //Util.CDBLog("HAHA", "in document loop customer: " + customerCount + "Document: " + documentCount, EventLogEntryType.Warning);

        //                                BE01JDocumentInfoDTO documentInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount];

        //                                w.Write("docId: " + documentInfo.docId + "\r\n");
        //                                w.Write("docIdSub: " + documentInfo.docIdSub + "\r\n");
        //                                w.Write("docDescription: " + documentInfo.docDescription + "\r\n");
        //                                w.Write("docStartDate: " + documentInfo.docStartDate.Value.ToString(datePattern) + "\r\n");
        //                                w.Write("docEndDate: " + documentInfo.docEndDate.Value.ToString(datePattern) + "\r\n");
        //                                w.Write("identityNoSub: " + documentInfo.identityNoSub + "\r\n");
        //                                w.Write("docChannel: " + documentInfo.docChannel + "\r\n");
        //                                w.Write("customerIdSubFromSource: " + documentInfo.customerIdSubFromSource + "\r\n");

        //                                w.Write("~~~~~~~~BE01DocumentOutput~~~~~~~~~~~" + "\r\n");
        //                                w.Write("resultFlg: " + documentOutput.resultFlg + "\r\n");
        //                                w.Write("errorCode: " + documentOutput.errorCode + "\r\n");
        //                                w.Write("errorMessage: " + documentOutput.errorMessage + "\r\n");
        //                                w.Write("\r\n");


        //                                //bool isAllImagesSuccessfullyFilled = true;

        //                                if (documentOutput.imageOutputList != null)
        //                                {

        //                                    if (documentOutput.imageOutputList.Count() != documentInfo.imageInfoList.Count())
        //                                    {

        //                                        // TODO
        //                                        // ExceptionLogDb exceptionDb = new ExceptionLogDb();
        //                                        //exceptionDb.LogException(null,
        //                                        Util.CDBLog("", "Number of documents sent is not same as recieved", EventLogEntryType.Error);


        //                                    }

        //                                    int imageCount = 0;
        //                                    //Business >> Customer >> documents >> Image
        //                                    foreach (BE01ImageOutput imageOutput in documentOutput.imageOutputList)
        //                                    {


        //                                        w.Write("~~~~~~~~~BE01JImageInfoDTO~~~~~~~~~~" + "\r\n");
        //                                        BE01JImageInfoDTO imageInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount];

        //                                        w.Write("imageUrl: " + imageInfo.imageUrl + "\r\n");
        //                                        w.Write("imageName: " + imageInfo.imageName + "\r\n");
        //                                        w.Write("imageSize: " + imageInfo.imageSize + "\r\n");
        //                                        w.Write("docRecievedSourceDate: " + imageInfo.docRecievedSourceDate.Value.ToString(datePattern) + "\r\n");
        //                                        w.Write("cmDocumentId: " + imageInfo.cmDocumentId + "\r\n");
        //                                        w.Write("dwmsDocumentId: " + imageInfo.dwmsDocumentId + "\r\n");
        //                                        w.Write("certificateNumber: " + imageInfo.certificateNumber + "\r\n");
        //                                        w.Write("certificateDate: " + imageInfo.certificateDate.Value.ToString(datePattern) + "\r\n");
        //                                        w.Write("localForeign: " + imageInfo.localForeign.ToString() + "\r\n");
        //                                        w.Write("marriageType: " + imageInfo.marriageType.ToString() + "\r\n");
        //                                        w.Write("vrfdWithOriginal: " + imageInfo.vrfdWithOriginal.ToString() + "\r\n");
        //                                        w.Write("imageCondition: " + imageInfo.imageCondition + "\r\n");

        //                                        w.Write("~~~~~~~~~BE01ImageOutput~~~~~~~~~~" + "\r\n");
        //                                        w.Write("dwmsDocumentId: " + imageOutput.dwmsDocumentId + "\r\n");
        //                                        w.Write("cmDocumentId: " + imageOutput.cmDocumentId + "\r\n");

        //                                        w.Write("resultFlg: " + imageOutput.resultFlg + "\r\n");
        //                                        w.Write("errorCode: " + imageOutput.errorCode + "\r\n");
        //                                        w.Write("errorMessage: " + imageOutput.errorMessage + "\r\n");
        //                                        w.Write("\r\n");

        //                                        //Util.CDBLog(string.Empty, "Update status", EventLogEntryType.Warning);
        //                                        #region update the status based on image output
        //                                        // TODO: Email 
        //                                        if (stage.ToString().ToUpper() == "VERIFIED" && isCDBUpdateResultToDBVerify())
        //                                        {
        //                                            if (imageOutput.resultFlg)
        //                                            {
        //                                                int docId = int.Parse(imageOutput.dwmsDocumentId);
        //                                                docDb.UpdateCmDocumentId(docId, imageOutput.cmDocumentId);
        //                                                docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.Sent);
        //                                            }
        //                                            else
        //                                            {

        //                                                int docId = int.Parse(imageOutput.dwmsDocumentId);
        //                                                //docDb.UpdateCmDocumentId(docId, string.Empty);

        //                                                #region Email
        //                                                // TODO: Email part
        //                                                ExceptionLogDb exceptionLogDb = new ExceptionLogDb();

        //                                                // TODO : to be removed
        //                                                string channelType = "";
        //                                                switch (documentInfo.docChannel)
        //                                                {

        //                                                    //Only requested Customer
        //                                                    case "001":
        //                                                        channelType = "MyDoc";
        //                                                        break;
        //                                                    case "002":
        //                                                        channelType = "MyHDBPage";
        //                                                        break;
        //                                                    case "003":
        //                                                        channelType = "Scan";
        //                                                        break;
        //                                                    case "004":
        //                                                        channelType = "Fax";
        //                                                        break;
        //                                                    case "005":
        //                                                        channelType = "Email";
        //                                                        break;
        //                                                    case "006":
        //                                                        channelType = "Deposit Box";
        //                                                        break;
        //                                                    case "007":
        //                                                        channelType = "Hardcopy Mail";
        //                                                        break;
        //                                                    case "008":
        //                                                        channelType = "Counter";
        //                                                        break;
        //                                                    case "009":
        //                                                        channelType = "Mixed";
        //                                                        break;
        //                                                    default:
        //                                                        channelType = "Err No Doc Channel";
        //                                                        break;
        //                                                }

        //                                                //Util.CDBLog(string.Empty, "Call exception", EventLogEntryType.Warning);
        //                                                exceptionLogDb.LogCDBException(null, channelType, businessInfo.businessRefNumber, channelType, imageOutput.errorCode, imageOutput.errorMessage, true, SendToCDBStageEnum.Verified);
        //                                                #endregion


        //                                                Util.CDBDetailLog(string.Empty, String.Format("Stage " + stage.ToString() + " Document Id: " + imageOutput.dwmsDocumentId + " imageOutput Result:" + imageOutput.resultFlg + " updated to " + SendToCDBStatusEnum.SentButFailed.ToString()), EventLogEntryType.Information);
        //                                                docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.SentButFailed);

        //                                            }
        //                                        }
        //                                        else if (stage.ToString().ToUpper() == "MODIFIEDVERIFIED" && isCDBUpdateResultToDBAccept())
        //                                        {
        //                                            if (imageOutput.resultFlg)
        //                                            {
        //                                                int docId = int.Parse(imageOutput.dwmsDocumentId);
        //                                                docDb.UpdateCmDocumentId(docId, imageOutput.cmDocumentId);
        //                                                docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.Sent);
        //                                            }
        //                                            else
        //                                            {

        //                                                int docId = int.Parse(imageOutput.dwmsDocumentId);
        //                                                //docDb.UpdateCmDocumentId(docId, string.Empty);

        //                                                // TODO: Email part
        //                                                ExceptionLogDb exceptionLogDb = new ExceptionLogDb();

        //                                                // TODO : to be removed
        //                                                string channelType = "";
        //                                                switch (documentInfo.docChannel)
        //                                                {

        //                                                    //Only requested Customer
        //                                                    case "001":
        //                                                        channelType = "MyDoc";
        //                                                        break;
        //                                                    case "002":
        //                                                        channelType = "MyHDBPage";
        //                                                        break;
        //                                                    case "003":
        //                                                        channelType = "Scan";
        //                                                        break;
        //                                                    case "004":
        //                                                        channelType = "Fax";
        //                                                        break;
        //                                                    case "005":
        //                                                        channelType = "Email";
        //                                                        break;
        //                                                    case "006":
        //                                                        channelType = "Deposit Box";
        //                                                        break;
        //                                                    case "007":
        //                                                        channelType = "Hardcopy Mail";
        //                                                        break;
        //                                                    case "008":
        //                                                        channelType = "Counter";
        //                                                        break;
        //                                                    case "009":
        //                                                        channelType = "Mixed";
        //                                                        break;
        //                                                    default:
        //                                                        channelType = "Err No Doc Channel";
        //                                                        break;
        //                                                }


        //                                                exceptionLogDb.LogCDBException(null, channelType, businessInfo.businessRefNumber, channelType, imageOutput.errorCode, imageOutput.errorMessage, true, SendToCDBStageEnum.ModifiedVerified);

        //                                                Util.CDBDetailLog(string.Empty, String.Format("Stage " + stage.ToString() + " Document Id: " + imageOutput.dwmsDocumentId + " imageOutput Result:" + imageOutput.resultFlg + " updated to " + SendToCDBStatusEnum.ModifiedSetSentButFailed.ToString()), EventLogEntryType.Information);
        //                                                docDb.UpdateSentToCDBStatus(Convert.ToInt32(documentInfo.docId.Trim()), SendToCDBStatusEnum.ModifiedSetSentButFailed);


        //                                                int? docSetId = docDb.GetDocSetIdByDocId(Convert.ToInt32(imageOutput.dwmsDocumentId.Trim()));
        //                                                if (docSetId.HasValue)
        //                                                    docSetDb.UpdateSetSentToCDBStatus(docSetId.Value, SendToCDBStatusEnum.ModifiedSetSentButFailed);

        //                                            }

        //                                        }
        //                                        else if (stage.ToString().ToUpper() == "ACCEPT" && isCDBUpdateResultToDBCompleteAccept())
        //                                        {
        //                                            if (imageOutput.resultFlg)
        //                                            {
        //                                                // TODO: Email part
        //                                                //int docId = int.Parse(documentInfo.docId);
        //                                                //docDb.UpdateCmDocumentId(docId, string.Empty);
        //                                                //    // TODO: 
        //                                                //    //ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
        //                                                //    //exceptionLogDb.LogCDBException(null,  ,refNo, subDirInfo.Name, error, exception, true);
        //                                                //docDb.UpdateSentToCDBStatus(Convert.ToInt32(documentInfo.docId.Trim()), SendToCDBStatusEnum.Sent);
        //                                            }
        //                                            else
        //                                            {

        //                                            }

        //                                        }
        //                                        #endregion


        //                                        #region image
        //                                        if (imageOutput.personOutputList != null)
        //                                        {
        //                                            int personCount = 0;
        //                                            //Business >> Customer >> documents >> Image >> Person
        //                                            //Util.CDBLog("HAHA", "in personIdentity start", EventLogEntryType.Warning);

        //                                            foreach (BE01PersonOutput personOutput in imageOutput.personOutputList)
        //                                            {

        //                                                //Util.CDBLog("HAHA", "in personIdentity loop", EventLogEntryType.Warning);


        //                                                w.Write("~~~~~~BE01JPersonIdentityInfoDTO~~~~~~~~~~" + "\r\n");

        //                                                BE01JPersonIdentityInfoDTO personIdentityInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount].personInfoList[personCount];

        //                                                w.Write("customerIdFromSource: " + personIdentityInfo.customerIdFromSource + "\r\n");
        //                                                w.Write("identityNo: " + personIdentityInfo.identityNo + "\r\n");
        //                                                w.Write("identityType: " + personIdentityInfo.identityType + "\r\n");
        //                                                w.Write("customerName: " + personIdentityInfo.customerName + "\r\n");

        //                                                w.Write("~~~~~~BE01JPersonInfoDTO~~~~~~~~~~" + "\r\n");
        //                                                if (personIdentityInfo.personInfo != null)
        //                                                {
        //                                                    //Util.CDBLog("HAHA", "in person start", EventLogEntryType.Warning);

        //                                                    BE01JPersonInfoDTO personInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount].personInfoList[personCount].personInfo;

        //                                                    w.Write("identityNo: " + personInfo.identityNo + "\r\n");
        //                                                    w.Write("identityType: " + personInfo.identityType + "\r\n");
        //                                                    w.Write("customerName: " + personInfo.customerName + "\r\n");

        //                                                    //Util.CDBLog("HAHA", "in person end", EventLogEntryType.Warning);

        //                                                }
        //                                                else
        //                                                {
        //                                                    w.Write("personInfo data is not avaialble \r\n");
        //                                                }

        //                                                w.Write("~~~~~~~~BE01PersonOutput~~~~~~~~~~" + "\r\n");
        //                                                w.Write("resultFlg: " + personOutput.customerIdFromSource + "\r\n");
        //                                                w.Write("resultFlg: " + personOutput.resultFlg + "\r\n");
        //                                                w.Write("errorCode: " + personOutput.errorCode + "\r\n");
        //                                                w.Write("errorMessage: " + personOutput.errorMessage + "\r\n");
        //                                                w.Write("\r\n");

        //                                                personCount++;

        //                                                //Util.CDBLog("HAHA", "in personIdentity end", EventLogEntryType.Warning);
        //                                            }
        //                                        }
        //                                        else
        //                                        {
        //                                            w.Write("personOutputList is missing \r\n");
        //                                        }
        //                                        #endregion
        //                                        imageCount++;
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    w.Write("imageOutputList is missing \r\n");
        //                                }
        //                                documentCount++;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            w.Write("documentOutputList is missing \r\n");
        //                        }
        //                        w.Write("\r\n");

        //                        customerCount++;
        //                    }
        //                }
        //                else
        //                {
        //                    w.Write("No customerOutputList missing \r\n");

        //                }
        //            }

        //            w.Write("--------------END-------------------------------" + "\r\n");
        //            w.Flush();
        //            w.Close();

        //            try
        //            {
        //                //delete the input file, because the Output file has both input and output info. This has been done in order to ease tracing
        //                //As the either and OutErr or Out has been created, the input file is deleted
        //                //File.Delete(filePath);
        //            }
        //            catch (Exception ex)
        //            {
        //                Util.CDBLog(string.Empty, String.Format("Error while deleting the input file, both input and output file will be available, Message: " + ex.Message), EventLogEntryType.Warning);
        //            }

        //            return true; // successfuly processesd
        //        }
        //        else
        //        {
        //            Util.CDBLog(string.Empty, String.Format("No output file can be produced, connection to CDB is configured OFF"), EventLogEntryType.Warning);
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        string errorMessage = String.Format("GenerateOutputTextFile() Message={0}, StackTrace={1}",
        //            ex.Message, ex.StackTrace);
        //        Util.CDBLog("DWMS_CDB_Service.GenerateOutputTextFile()", errorMessage, EventLogEntryType.Error);
        //        return false;
        //    }
        //}

        protected bool GenerateOutputTextFile(string filePathOut, string filePath, BE01JOutputDTO response, BE01JBusinessInfoDTO businessInfo, BE01JSystemInfoDTO systemInfo, BE01JAuthenticationDTO authentication, SendToCDBStageEnum stage)
        {

            DocDb docDb = new DocDb();
            DocSetDb docSetDb = new DocSetDb();
            DocAppDb docAppDb = new DocAppDb();

            string datePattern = "dd MMM yyyy hh:mm:ss tt";
            try
            {

                if (response != null)
                {
                    //these flags need to be removed, but is used for logging purposes
                    //bool isDocAppSuccessfullyFilled = true;

                    StreamWriter w;
                    //Create either a Output or OutputError file

                    w = File.CreateText(filePathOut); //will create either a .Out or .OutErr depending on the filepath

                    w.Write("---------START-----------------------" + "\r\n");
                    w.Write("\r\n");
                    w.Write("--------------------------------" + "\r\n");
                    w.Write("BE01JAuthenticationDTO" + "\r\n");
                    w.Write("--------------------------------" + "\r\n");
                    w.Write("userName: " + authentication.userName + "\r\n");
                    w.Write("passWord: " + authentication.passWord + "\r\n");
                    w.Write("--------------------------------" + "\r\n");
                    w.Write("\r\n");

                    w.Write("--------------------------------\r\n");
                    w.Write("BE01JSystemInfoDTO:" + "\r\n");
                    w.Write("--------------------------------\r\n");
                    w.Write("fileSystemId: " + systemInfo.fileSystemId + "\r\n");
                    w.Write("fileDate: " + systemInfo.fileDate.Value.ToString(datePattern) + "\r\n");
                    w.Write("completenessUserId: " + systemInfo.completenessUserId + "\r\n");
                    w.Write("updateDate: " + systemInfo.updateDate.Value.ToString(datePattern) + "\r\n");
                    w.Write("updateSystemId: " + systemInfo.updateSystemId + "\r\n");
                    w.Write("updateTime: " + systemInfo.updateTime.Value.ToString(datePattern) + "\r\n");
                    w.Write("verificationUserId: " + systemInfo.verificationUserId + "\r\n");
                    w.Write("--------------------------------" + "\r\n");
                    w.Write("BE01JOutputDTO:" + "\r\n");
                    w.Write("--------------------------------" + "\r\n");
                    w.Write("obsResultFlag: " + response.obsResultFlag.ToString() + "\r\n");
                    w.Write("obsErrorCode: " + response.obsErrorCode.ToString() + "\r\n");
                    w.Write("obsServerErrorMessage: " + response.obsServerErrorMessage.ToString() + "\r\n");
                    w.Write("--------------------------------\r\n");
                    w.Write("--------------------------------\r\n");
                    w.Write("\r\n");
                    w.Write("\r\n");

                    w.Write("--------------------------------\r\n");
                    w.Write("BE01JBusinessInfoDTO" + "\r\n");
                    w.Write("--------------------------------\r\n");

                    if (response.businessOutput != null)
                    {


                        BE01BusinessOutput businessOutput = response.businessOutput;

                        //Util.CDBLog("HAHA", "in business start", EventLogEntryType.Warning);


                        w.Write("businessRefNumber: " + businessInfo.businessRefNumber + "\r\n");
                        w.Write("businessTransactionNumber: " + businessInfo.businessTransactionNumber + "\r\n");
                        w.Write("--------------------------------" + "\r\n");
                        w.Write("BE01BusinessOutput" + "\r\n");
                        w.Write("--------------------------------" + "\r\n");
                        w.Write("resultFlg: " + businessOutput.resultFlg + "\r\n");
                        w.Write("errorCode: " + businessOutput.errorCode + "\r\n");
                        w.Write("errorMessage: " + businessOutput.errorMessage + "\r\n");

                        //Util.CDBLog("HAHA", "in business end", EventLogEntryType.Warning);


                        if (businessOutput.customerOutputList != null)
                        {
                            int customerCount = 0;

                            //Util.CDBLog("HAHA", "in customer start", EventLogEntryType.Warning);

                            foreach (BE01CustomerOutput customerOutput in businessOutput.customerOutputList)
                            {
                                //Util.CDBLog("HAHA", "in customer loop", EventLogEntryType.Warning);


                                w.Write("~~~~~~~~~BE01JCustomerInfoDTO~~~~~~~~~\r\n");
                                BE01JCustomerInfoDTO customerInfo = businessInfo.customerInfoList[customerCount];

                                w.Write("customerIdFromSource: " + customerInfo.customerIdFromSource + "\r\n");
                                w.Write("customerName: " + customerInfo.customerName + "\r\n");
                                w.Write("docCounter: " + customerInfo.docCounter + "\r\n");
                                w.Write("identityNo: " + customerInfo.identityNo + "\r\n");
                                w.Write("identityType: " + customerInfo.identityType + "\r\n");
                                w.Write("customerType: " + customerInfo.customerType + "\r\n");

                                w.Write("~~~~~~~~~BE01CustomerOutput~~~~~~~~~\r\n");
                                w.Write("customerIdFromSource: " + customerOutput.customerIdFromSource + "\r\n"); //
                                w.Write("resultFlg: " + customerOutput.resultFlg + "\r\n");
                                w.Write("errorCode: " + customerOutput.errorCode + "\r\n");
                                w.Write("errorMessage: " + customerOutput.errorMessage + "\r\n");
                                w.Write("\r\n");

                                if (customerOutput.documentOutputList != null)
                                {
                                    //Business >> Customer >> documents
                                    int documentCount = 0;

                                    //Util.CDBLog("HAHA", "in document start", EventLogEntryType.Warning);


                                    foreach (BE01DocumentOutput documentOutput in customerOutput.documentOutputList)
                                    {

                                        w.Write("~~~~~~~~BE01JDocumentInfoDTO~~~~~~~~~~~" + "\r\n");

                                        //Util.CDBLog("HAHA", "in document loop customer: " + customerCount + "Document: " + documentCount, EventLogEntryType.Warning);

                                        BE01JDocumentInfoDTO documentInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount];

                                        w.Write("docId: " + documentInfo.docId + "\r\n");
                                        w.Write("docIdSub: " + documentInfo.docIdSub + "\r\n");
                                        w.Write("docDescription: " + documentInfo.docDescription + "\r\n");
                                        w.Write("docStartDate: " + documentInfo.docStartDate.Value.ToString(datePattern) + "\r\n");
                                        w.Write("docEndDate: " + documentInfo.docEndDate.Value.ToString(datePattern) + "\r\n");
                                        w.Write("identityNoSub: " + documentInfo.identityNoSub + "\r\n");
                                        w.Write("docChannel: " + documentInfo.docChannel + "\r\n");
                                        w.Write("customerIdSubFromSource: " + documentInfo.customerIdSubFromSource + "\r\n");

                                        w.Write("~~~~~~~~BE01DocumentOutput~~~~~~~~~~~" + "\r\n");
                                        w.Write("resultFlg: " + documentOutput.resultFlg + "\r\n");
                                        w.Write("errorCode: " + documentOutput.errorCode + "\r\n");
                                        w.Write("errorMessage: " + documentOutput.errorMessage + "\r\n");
                                        w.Write("\r\n");


                                        //bool isAllImagesSuccessfullyFilled = true;

                                        if (documentOutput.imageOutputList != null)
                                        {

                                            if (documentOutput.imageOutputList.Count() != documentInfo.imageInfoList.Count())
                                            {

                                                // TODO
                                                // ExceptionLogDb exceptionDb = new ExceptionLogDb();
                                                //exceptionDb.LogException(null,
                                                Util.CDBLog("", "Number of documents sent is not same as recieved", EventLogEntryType.Error);


                                            }

                                            int imageCount = 0;
                                            //Business >> Customer >> documents >> Image
                                            foreach (BE01ImageOutput imageOutput in documentOutput.imageOutputList)
                                            {


                                                w.Write("~~~~~~~~~BE01JImageInfoDTO~~~~~~~~~~" + "\r\n");
                                                BE01JImageInfoDTO imageInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount];

                                                w.Write("imageUrl: " + imageInfo.imageUrl + "\r\n");
                                                w.Write("imageName: " + imageInfo.imageName + "\r\n");
                                                w.Write("imageSize: " + imageInfo.imageSize + "\r\n");
                                                w.Write("docRecievedSourceDate: " + imageInfo.docRecievedSourceDate.Value.ToString(datePattern) + "\r\n");
                                                w.Write("cmDocumentId: " + imageInfo.cmDocumentId + "\r\n");
                                                w.Write("dwmsDocumentId: " + imageInfo.dwmsDocumentId + "\r\n");
                                                w.Write("certificateNumber: " + imageInfo.certificateNumber + "\r\n");
                                                w.Write("certificateDate: " + imageInfo.certificateDate.Value.ToString(datePattern) + "\r\n");
                                                w.Write("localForeign: " + imageInfo.localForeign.ToString() + "\r\n");
                                                w.Write("marriageType: " + imageInfo.marriageType.ToString() + "\r\n");
                                                w.Write("vrfdWithOriginal: " + imageInfo.vrfdWithOriginal.ToString() + "\r\n");
                                                w.Write("imageCondition: " + imageInfo.imageCondition + "\r\n");

                                                w.Write("~~~~~~~~~BE01ImageOutput~~~~~~~~~~" + "\r\n");
                                                w.Write("dwmsDocumentId: " + imageOutput.dwmsDocumentId + "\r\n");
                                                w.Write("cmDocumentId: " + imageOutput.cmDocumentId + "\r\n");

                                                w.Write("resultFlg: " + imageOutput.resultFlg + "\r\n");
                                                w.Write("errorCode: " + imageOutput.errorCode + "\r\n");
                                                w.Write("errorMessage: " + imageOutput.errorMessage + "\r\n");
                                                w.Write("\r\n");

                                                //Util.CDBLog(string.Empty, "Update status", EventLogEntryType.Warning);
                                                #region update the status based on image output
                                                // TODO: Email 
                                                if (stage.ToString().ToUpper() == "VERIFIED" && isCDBUpdateResultToDBVerify())
                                                {
                                                    if (imageOutput.resultFlg)
                                                    {
                                                        int docId = int.Parse(imageOutput.dwmsDocumentId);
                                                        docDb.UpdateCmDocumentId(docId, imageOutput.cmDocumentId);
                                                        docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.Sent);
                                                    }
                                                    else
                                                    {

                                                        int docId = int.Parse(imageOutput.dwmsDocumentId);
                                                        //docDb.UpdateCmDocumentId(docId, string.Empty);

                                                        #region Email
                                                        // TODO: Email part
                                                        ExceptionLogDb exceptionLogDb = new ExceptionLogDb();

                                                        // TODO : to be removed
                                                        string channelType = "";
                                                        switch (documentInfo.docChannel)
                                                        {

                                                            //Only requested Customer
                                                            case "001":
                                                                channelType = "MyDoc";
                                                                break;
                                                            case "002":
                                                                channelType = "MyHDBPage";
                                                                break;
                                                            case "003":
                                                                channelType = "Scan";
                                                                break;
                                                            case "004":
                                                                channelType = "Fax";
                                                                break;
                                                            case "005":
                                                                channelType = "Email";
                                                                break;
                                                            case "006":
                                                                channelType = "Deposit Box";
                                                                break;
                                                            case "007":
                                                                channelType = "Hardcopy Mail";
                                                                break;
                                                            case "008":
                                                                channelType = "Counter";
                                                                break;
                                                            case "009":
                                                                channelType = "Mixed";
                                                                break;
                                                            default:
                                                                channelType = "Err No Doc Channel";
                                                                break;
                                                        }

                                                        //Util.CDBLog(string.Empty, "Call exception", EventLogEntryType.Warning);
                                                        exceptionLogDb.LogCDBException(null, channelType, businessInfo.businessRefNumber, channelType, imageOutput.errorCode, imageOutput.errorMessage, true, SendToCDBStageEnum.Verified);
                                                        #endregion


                                                        Util.CDBDetailLog(string.Empty, String.Format("Stage " + stage.ToString() + " Document Id: " + imageOutput.dwmsDocumentId + " imageOutput Result:" + imageOutput.resultFlg + " updated to " + SendToCDBStatusEnum.SentButFailed.ToString()), EventLogEntryType.Information);
                                                        docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.SentButFailed);

                                                    }
                                                }
                                                else if (stage.ToString().ToUpper() == "MODIFIEDVERIFIED" && isCDBUpdateResultToDBAccept())
                                                {
                                                    if (imageOutput.resultFlg)
                                                    {
                                                        int docId = int.Parse(imageOutput.dwmsDocumentId);
                                                        docDb.UpdateCmDocumentId(docId, imageOutput.cmDocumentId);
                                                        docDb.UpdateSentToCDBStatus(docId, SendToCDBStatusEnum.Sent);
                                                    }
                                                    else
                                                    {

                                                        int docId = int.Parse(imageOutput.dwmsDocumentId);
                                                        //docDb.UpdateCmDocumentId(docId, string.Empty);

                                                        // TODO: Email part
                                                        ExceptionLogDb exceptionLogDb = new ExceptionLogDb();

                                                        // TODO : to be removed
                                                        string channelType = "";
                                                        switch (documentInfo.docChannel)
                                                        {

                                                            //Only requested Customer
                                                            case "001":
                                                                channelType = "MyDoc";
                                                                break;
                                                            case "002":
                                                                channelType = "MyHDBPage";
                                                                break;
                                                            case "003":
                                                                channelType = "Scan";
                                                                break;
                                                            case "004":
                                                                channelType = "Fax";
                                                                break;
                                                            case "005":
                                                                channelType = "Email";
                                                                break;
                                                            case "006":
                                                                channelType = "Deposit Box";
                                                                break;
                                                            case "007":
                                                                channelType = "Hardcopy Mail";
                                                                break;
                                                            case "008":
                                                                channelType = "Counter";
                                                                break;
                                                            case "009":
                                                                channelType = "Mixed";
                                                                break;
                                                            default:
                                                                channelType = "Err No Doc Channel";
                                                                break;
                                                        }


                                                        exceptionLogDb.LogCDBException(null, channelType, businessInfo.businessRefNumber, channelType, imageOutput.errorCode, imageOutput.errorMessage, true, SendToCDBStageEnum.ModifiedVerified);

                                                        Util.CDBDetailLog(string.Empty, String.Format("Stage " + stage.ToString() + " Document Id: " + imageOutput.dwmsDocumentId + " imageOutput Result:" + imageOutput.resultFlg + " updated to " + SendToCDBStatusEnum.ModifiedSetSentButFailed.ToString()), EventLogEntryType.Information);
                                                        docDb.UpdateSentToCDBStatus(Convert.ToInt32(documentInfo.docId.Trim()), SendToCDBStatusEnum.ModifiedSetSentButFailed);


                                                        int? docSetId = docDb.GetDocSetIdByDocId(Convert.ToInt32(imageOutput.dwmsDocumentId.Trim()));
                                                        if (docSetId.HasValue)
                                                            docSetDb.UpdateSetSentToCDBStatus(docSetId.Value, SendToCDBStatusEnum.ModifiedSetSentButFailed);

                                                    }

                                                }
                                                else if (stage.ToString().ToUpper() == "ACCEPT" && isCDBUpdateResultToDBCompleteAccept())
                                                {
                                                    if (imageOutput.resultFlg)
                                                    {
                                                        // TODO: Email part
                                                        //int docId = int.Parse(documentInfo.docId);
                                                        //docDb.UpdateCmDocumentId(docId, string.Empty);
                                                        //    // TODO: 
                                                        //    //ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                                                        //    //exceptionLogDb.LogCDBException(null,  ,refNo, subDirInfo.Name, error, exception, true);
                                                        //docDb.UpdateSentToCDBStatus(Convert.ToInt32(documentInfo.docId.Trim()), SendToCDBStatusEnum.Sent);
                                                    }
                                                    else
                                                    {

                                                    }

                                                }
                                                #endregion


                                                #region image
                                                if (imageOutput.personOutputList != null)
                                                {
                                                    int personCount = 0;
                                                    //Business >> Customer >> documents >> Image >> Person
                                                    //Util.CDBLog("HAHA", "in personIdentity start", EventLogEntryType.Warning);

                                                    foreach (BE01PersonOutput personOutput in imageOutput.personOutputList)
                                                    {

                                                        //Util.CDBLog("HAHA", "in personIdentity loop", EventLogEntryType.Warning);


                                                        w.Write("~~~~~~BE01JPersonIdentityInfoDTO~~~~~~~~~~" + "\r\n");

                                                        BE01JPersonIdentityInfoDTO personIdentityInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount].personInfoList[personCount];

                                                        w.Write("customerIdFromSource: " + personIdentityInfo.customerIdFromSource + "\r\n");
                                                        w.Write("identityNo: " + personIdentityInfo.identityNo + "\r\n");
                                                        w.Write("identityType: " + personIdentityInfo.identityType + "\r\n");
                                                        w.Write("customerName: " + personIdentityInfo.customerName + "\r\n");

                                                        w.Write("~~~~~~BE01JPersonInfoDTO~~~~~~~~~~" + "\r\n");
                                                        if (personIdentityInfo.personInfo != null)
                                                        {
                                                            //Util.CDBLog("HAHA", "in person start", EventLogEntryType.Warning);

                                                            BE01JPersonInfoDTO personInfo = businessInfo.customerInfoList[customerCount].documentInfoList[documentCount].imageInfoList[imageCount].personInfoList[personCount].personInfo;

                                                            w.Write("identityNo: " + personInfo.identityNo + "\r\n");
                                                            w.Write("identityType: " + personInfo.identityType + "\r\n");
                                                            w.Write("customerName: " + personInfo.customerName + "\r\n");

                                                            //Util.CDBLog("HAHA", "in person end", EventLogEntryType.Warning);

                                                        }
                                                        else
                                                        {
                                                            w.Write("personInfo data is not avaialble \r\n");
                                                        }

                                                        w.Write("~~~~~~~~BE01PersonOutput~~~~~~~~~~" + "\r\n");
                                                        w.Write("resultFlg: " + personOutput.customerIdFromSource + "\r\n");
                                                        w.Write("resultFlg: " + personOutput.resultFlg + "\r\n");
                                                        w.Write("errorCode: " + personOutput.errorCode + "\r\n");
                                                        w.Write("errorMessage: " + personOutput.errorMessage + "\r\n");
                                                        w.Write("\r\n");

                                                        personCount++;

                                                        //Util.CDBLog("HAHA", "in personIdentity end", EventLogEntryType.Warning);
                                                    }
                                                }
                                                else
                                                {
                                                    w.Write("personOutputList is missing \r\n");
                                                }
                                                #endregion
                                                imageCount++;
                                            }
                                        }
                                        else
                                        {
                                            w.Write("imageOutputList is missing \r\n");
                                        }
                                        documentCount++;
                                    }
                                }
                                else
                                {
                                    w.Write("documentOutputList is missing \r\n");
                                }
                                w.Write("\r\n");

                                customerCount++;
                            }
                        }
                        else
                        {
                            w.Write("No customerOutputList missing \r\n");

                        }
                    }

                    w.Write("--------------END-------------------------------" + "\r\n");
                    w.Flush();
                    w.Close();

                    try
                    {
                        //delete the input file, because the Output file has both input and output info. This has been done in order to ease tracing
                        //As the either and OutErr or Out has been created, the input file is deleted
                        //File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Util.CDBLog(string.Empty, String.Format("Error while deleting the input file, both input and output file will be available, Message: " + ex.Message), EventLogEntryType.Warning);
                    }

                    return true; // successfuly processesd
                }
                else
                {
                    Util.CDBLog(string.Empty, String.Format("No output file can be produced, connection to CDB is configured OFF"), EventLogEntryType.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {

                string errorMessage = String.Format("GenerateOutputTextFile() Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.GenerateOutputTextFile()", errorMessage, EventLogEntryType.Error);
                return false;
            }
        }

        protected bool GenerateInputTextFile(BE01JSystemInfoDTO systemInfo, BE01JBusinessInfoDTO businessInfo, BE01JAuthenticationDTO authentication, string filePath)
        {

            string datePattern = "dd MMM yyyy hh:mm:ss tt";
            try
            {
                StreamWriter w;
                w = File.CreateText(filePath);


                w.Write("--------------------------------" + "\r\n");
                w.Write("BE01JAuthenticationDTO" + "\r\n");
                w.Write("--------------------------------" + "\r\n");
                w.Write("userName: " + authentication.userName + "\r\n");
                w.Write("passWord: " + authentication.passWord + "\r\n");
                w.Write("--------------------------------" + "\r\n");

                w.Write("--------------------------------" + "\r\n");
                w.Write("BE01JSystemInfoDTO" + "\r\n");
                w.Write("--------------------------------" + "\r\n");
                w.Write("fileSystemId: " + systemInfo.fileSystemId + "\r\n");
                w.Write("fileDate: " + systemInfo.fileDate.Value.ToString(datePattern) + "\r\n");
                w.Write("completenessUserId: " + systemInfo.completenessUserId + "\r\n");
                w.Write("updateDate: " + systemInfo.updateDate.Value.ToString(datePattern) + "\r\n");
                w.Write("updateSystemId: " + systemInfo.updateSystemId + "\r\n");
                w.Write("updateTime: " + systemInfo.updateTime.Value.ToString(datePattern) + "\r\n");
                w.Write("verificationUserId: " + systemInfo.verificationUserId + "\r\n");

                //Business >> Customer >> documents >> Image >> Person

                w.Write("\r\n--------------------------------\r\n");
                w.Write("BE01JBusinessInfoDTO" + "\r\n");
                w.Write("--------------------------------" + "\r\n");
                w.Write("businessRefNumber: " + businessInfo.businessRefNumber + "\r\n");
                w.Write("businessTransactionNumber: " + businessInfo.businessTransactionNumber + "\r\n");

                //Business >> Customer 
                if (businessInfo.customerInfoList != null)
                {
                    foreach (BE01JCustomerInfoDTO customer in businessInfo.customerInfoList)
                    {
                        w.Write("~~~~~~~~~CUSTOMER INFO~~~~~~~~~" + "\r\n");
                        w.Write("customerIdFromSource: " + customer.customerIdFromSource + "\r\n");
                        w.Write("customerName: " + customer.customerName + "\r\n");
                        w.Write("docCounter: " + customer.docCounter + "\r\n");
                        w.Write("identityNo: " + customer.identityNo + "\r\n");
                        w.Write("identityType: " + customer.identityType + "\r\n");
                        w.Write("customerType: " + customer.customerType + "\r\n");

                        if (customer.documentInfoList != null)
                        {
                            //Business >> Customer >> documents
                            foreach (BE01JDocumentInfoDTO document in customer.documentInfoList)
                            {
                                w.Write("~~~~~~~~DOCUMENT INFO~~~~~~~~~~~" + "\r\n");
                                w.Write("docId: " + document.docId + "\r\n");
                                w.Write("docIdSub: " + document.docIdSub + "\r\n");
                                w.Write("docDescription: " + document.docDescription + "\r\n");
                                w.Write("docStartDate: " + document.docStartDate.Value.ToString(datePattern) + "\r\n");
                                w.Write("docEndDate: " + document.docEndDate.Value.ToString(datePattern) + "\r\n");
                                w.Write("identityNoSub: " + document.identityNoSub + "\r\n");
                                w.Write("docChannel: " + document.docChannel + "\r\n");
                                w.Write("customerIdSubFromSource: " + document.customerIdSubFromSource + "\r\n");

                                if (document.imageInfoList != null)
                                {

                                    //Business >> Customer >> documents >> Image
                                    foreach (BE01JImageInfoDTO image in document.imageInfoList)
                                    {
                                        w.Write("~~~~~~~~~IMAGE INFO~~~~~~~~~~" + "\r\n");
                                        w.Write("imageUrl: " + image.imageUrl + "\r\n");
                                        w.Write("imageName: " + image.imageName + "\r\n");
                                        w.Write("imageSize: " + image.imageSize + "\r\n");
                                        w.Write("docRecievedSourceDate: " + image.docRecievedSourceDate.Value.ToString(datePattern) + "\r\n");
                                        w.Write("cmDocumentId: " + image.cmDocumentId + "\r\n");
                                        w.Write("dwmsDocumentId: " + image.dwmsDocumentId + "\r\n");
                                        w.Write("certificateNumber: " + image.certificateNumber + "\r\n");
                                        w.Write("certificateDate: " + image.certificateDate.Value.ToString(datePattern) + "\r\n");
                                        w.Write("localForeign: " + (image.localForeign.ToString().Length > 0 ? image.localForeign.ToString() : null) + "\r\n");
                                        w.Write("marriageType: " + (image.marriageType.ToString().Length > 0 ? image.marriageType.ToString() : null) + "\r\n");
                                        w.Write("vrfdWithOriginal: " + image.vrfdWithOriginal.ToString() + "\r\n");
                                        w.Write("imageCondition: " + image.imageCondition + "\r\n");


                                        if (image.personInfoList != null)
                                        {

                                            //Business >> Customer >> documents >> Image >> Person
                                            foreach (BE01JPersonIdentityInfoDTO personIdentity in image.personInfoList)
                                            {
                                                w.Write("~~~~~~PERSON-IDENTITY INFO~~~~~~~~~~" + "\r\n");
                                                w.Write("customerIdFromSource: " + personIdentity.customerIdFromSource + "\r\n");
                                                w.Write("identityNo: " + personIdentity.identityNo + "\r\n");
                                                w.Write("identityType: " + personIdentity.identityType + "\r\n");
                                                w.Write("customerName: " + personIdentity.customerName + "\r\n");

                                                w.Write("~~~~~~PERSON INFO~~~~~~~~~~" + "\r\n");
                                                if (personIdentity.personInfo != null)
                                                {
                                                    w.Write("identityNo: " + personIdentity.personInfo.identityNo + "\r\n");
                                                    w.Write("identityType: " + personIdentity.personInfo.identityType + "\r\n");
                                                    w.Write("customerName: " + personIdentity.personInfo.customerName + "\r\n");

                                                }
                                                else
                                                {
                                                    w.Write("PersonInfo is null" + "\r\n");
                                                }

                                                w.Write("\r\n");


                                            }
                                        }
                                        else
                                            w.Write("PersonInfoList (PersonIdentityInfoDTO) is null" + "\r\n");
                                    }
                                }
                                else
                                    w.Write("ERROR: ImageInfoList is null!" + "\r\n");
                            }
                        }
                        else
                            w.Write("ERROR: DocumentInfoList is null!" + "\r\n");
                        w.Write("\r\n");
                    }
                }
                else
                {
                    w.Write("Error: CustomerInfoList is null!" + "\r\n");
                }
                w.Write("-----------END-------------------------------" + "\r\n");
                w.Flush();
                w.Close();

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("GenerateInputTextFile() Message={0}, StackTrace={1}",
                   ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.GenerateInputTextFile()", errorMessage, EventLogEntryType.Error);
                return false;
            }

        }

        protected BE01JBusinessInfoDTO XmlInput(string fileFullPath, ref BE01JSystemInfoDTO systemInfo, ref BE01JAuthenticationDTO authentication)
        {
            Util.CDBLog(string.Empty, "Start: Importing data from XML file", EventLogEntryType.Information);

            BE01JBusinessInfoDTO businessInfo;
            try
            {

                authentication.userName = CDBUtil.GetUserNameDWMSToCDB();
                authentication.passWord = CDBUtil.GetPasswordDWMSToCDB();

                XmlDocument doc = new XmlDocument();
                doc.Load(fileFullPath); //open the test data file

                XmlNode BE01JSystemInfoDTO = doc.SelectSingleNode("/ToCDBData/BE01JSystemInfoDTO");
                systemInfo.fileSystemId = BE01JSystemInfoDTO.SelectSingleNode("fileSystemId").InnerText.Trim();
                systemInfo.updateSystemId = BE01JSystemInfoDTO.SelectSingleNode("updateSystemId").InnerText.Trim();
                systemInfo.fileDate = Convert.ToDateTime(BE01JSystemInfoDTO.SelectSingleNode("fileDate").InnerText.Trim());

                systemInfo.completenessUserId = BE01JSystemInfoDTO.SelectSingleNode("completenessUserId").InnerText.Trim();
                systemInfo.updateDate = Convert.ToDateTime(BE01JSystemInfoDTO.SelectSingleNode("updateDate").InnerText.Trim());
                systemInfo.updateTime = Convert.ToDateTime(BE01JSystemInfoDTO.SelectSingleNode("updateTime").InnerText.Trim());
                systemInfo.verificationUserId = BE01JSystemInfoDTO.SelectSingleNode("verificationUserId").InnerText.Trim();

                businessInfo = new BE01JBusinessInfoDTO();

                XmlNode BE01JBusinessInfoDTO = doc.SelectSingleNode("/ToCDBData/BE01JBusinessInfoDTO");

                businessInfo.businessRefNumber = BE01JBusinessInfoDTO.SelectSingleNode("businessRefNumber").InnerText.Trim();
                businessInfo.businessTransactionNumber = BE01JBusinessInfoDTO.SelectSingleNode("businessTransactionNumber").InnerText.Trim();

                XmlNode customerInfoListNode = doc.SelectSingleNode("/ToCDBData/BE01JBusinessInfoDTO/customerInfoList");


                XmlNodeList customerInfoList = customerInfoListNode.SelectNodes("BE01JCustomerInfoDTO");
                List<BE01JCustomerInfoDTO> customers = new List<BE01JCustomerInfoDTO>();

                foreach (XmlNode customerNode in customerInfoList)
                {
                    BE01JCustomerInfoDTO customer = new BE01JCustomerInfoDTO();
                    customer.customerIdFromSource = customerNode.SelectSingleNode("customerIdFromSource").InnerText.Trim();
                    customer.identityNo = customerNode.SelectSingleNode("identityNo").InnerText.Trim();
                    customer.identityType = customerNode.SelectSingleNode("identityType").InnerText.Trim();
                    customer.customerName = customerNode.SelectSingleNode("customerName").InnerText.Trim();
                    customer.docCounter = Convert.ToInt16(customerNode.SelectSingleNode("docCounter").InnerText.Trim());
                    customer.customerType = customerNode.SelectSingleNode("customerType").InnerText.Trim();

                    List<BE01JDocumentInfoDTO> documents = new List<BE01JDocumentInfoDTO>();

                    //XmlNode documentInfoListNode = doc.SelectSingleNode("/ToCDBData/BE01JBusinessInfoDTO/customerInfoList/BE01JCustomerInfoDTO/documentInfoList");
                    XmlNode documentInfoListNode = customerNode.SelectSingleNode(".//documentInfoList");


                    XmlNodeList documentInfoList = documentInfoListNode.SelectNodes("BE01JDocumentInfoDTO");


                    foreach (XmlNode documentNode in documentInfoList)
                    {
                        BE01JDocumentInfoDTO document = new BE01JDocumentInfoDTO();

                        document.docId = documentNode.SelectSingleNode("docId").InnerText.Trim();
                        document.docIdSub = documentNode.SelectSingleNode("docIdSub").InnerText.Trim();
                        document.docDescription = documentNode.SelectSingleNode("docDescription").InnerText.Trim();
                        document.docStartDate = Convert.ToDateTime(documentNode.SelectSingleNode("docStartDate").InnerText.Trim());
                        document.docEndDate = Convert.ToDateTime(documentNode.SelectSingleNode("docEndDate").InnerText.Trim());
                        document.identityNoSub = documentNode.SelectSingleNode("identityNoSub").InnerText.Trim();
                        document.docChannel = documentNode.SelectSingleNode("docChannel").InnerText.Trim();
                        document.customerIdSubFromSource = documentNode.SelectSingleNode("customerIdSubFromSource").InnerText.Trim();

                        List<BE01JImageInfoDTO> images = new List<BE01JImageInfoDTO>();
                        //XmlNode imageInfoListNode = doc.SelectSingleNode("/ToCDBData/BE01JBusinessInfoDTO/customerInfoList/BE01JCustomerInfoDTO/documentInfoList/BE01JDocumentInfoDTO/imageInfoList");
                        XmlNode imageInfoListNode = documentNode.SelectSingleNode(".//imageInfoList");


                        XmlNodeList imageInfoList = imageInfoListNode.SelectNodes("BE01JImageInfoDTO");


                        foreach (XmlNode imageNode in imageInfoList)
                        {
                            BE01JImageInfoDTO image = new BE01JImageInfoDTO();
                            image.imageUrl = imageNode.SelectSingleNode("imageUrl").InnerText.Trim();
                            image.imageName = imageNode.SelectSingleNode("imageName").InnerText.Trim();
                            image.imageSize = imageNode.SelectSingleNode("imageSize").InnerText.Trim();
                            image.docRecievedSourceDate = Convert.ToDateTime(imageNode.SelectSingleNode("docRecievedSourceDate").InnerText.Trim());
                            image.cmDocumentId = imageNode.SelectSingleNode("cmDocumentId").InnerText.Trim();
                            image.dwmsDocumentId = imageNode.SelectSingleNode("dwmsDocumentId").InnerText.Trim();
                            image.certificateNumber = imageNode.SelectSingleNode("certificateNumber").InnerText.Trim();
                            image.certificateDate = Convert.ToDateTime(imageNode.SelectSingleNode("certificateDate").InnerText.Trim());
                            image.localForeign = imageNode.SelectSingleNode("localForeign").InnerText.Trim();
                            image.marriageType = imageNode.SelectSingleNode("marriageType").InnerText.Trim();
                            image.vrfdWithOriginal = Convert.ToBoolean(imageNode.SelectSingleNode("vrfdWithOriginal").InnerText.Trim());
                            image.imageCondition = imageNode.SelectSingleNode("imageCondition").InnerText.Trim();



                            List<BE01JPersonIdentityInfoDTO> personsIdentities = new List<BE01JPersonIdentityInfoDTO>();
                            //XmlNode personInfoListNode = doc.SelectSingleNode("/ToCDBData/BE01JBusinessInfoDTO/customerInfoList/BE01JCustomerInfoDTO/documentInfoList/BE01JDocumentInfoDTO/imageInfoList/BE01JImageInfoDTO/personInfoList");
                            XmlNode personIdentityInfoListNode = imageNode.SelectSingleNode(".//personInfoList");
                            XmlNodeList personIdentityInfoList = personIdentityInfoListNode.SelectNodes("BE01JPersonIdentityInfoDTO");

                            foreach (XmlNode personIdentityNode in personIdentityInfoList)
                            {
                                BE01JPersonIdentityInfoDTO personIdentity = new BE01JPersonIdentityInfoDTO();
                                personIdentity.customerIdFromSource = personIdentityNode.SelectSingleNode("customerIdFromSource").InnerText.Trim();
                                personIdentity.identityNo = personIdentityNode.SelectSingleNode("identityNo").InnerText.Trim();
                                personIdentity.identityType = personIdentityNode.SelectSingleNode("identityType").InnerText.Trim();
                                personIdentity.customerName = personIdentityNode.SelectSingleNode("customerName").InnerText.Trim();

                                //List<BE01JPersonInfoDTO> persons = new List<BE01JPersonInfoDTO>();
                                //XmlNode personInfoListNode = imageNode.SelectSingleNode(".//personInfoList");
                                //XmlNodeList personInfoList = personIdentityInfoListNode.SelectNodes("BE01JPersonInfoDTO");

                                //foreach (XmlNode personNode in personInfoList)
                                //{
                                //    BE01JPersonInfoDTO person = new BE01JPersonInfoDTO();
                                //    person.identityNo = personNode.SelectSingleNode("identityNo").InnerText.Trim();
                                //    person.identityType = personNode.SelectSingleNode("identityType").InnerText.Trim();
                                //    person.customerName = personNode.SelectSingleNode("customerName").InnerText.Trim();
                                //    persons.Add(person);
                                //}

                                //XmlNode personInfoListNode = personIdentityInfoListNode.SelectSingleNode(".//personInfo");
                                XmlNode personInfoListNode = personIdentityNode.SelectSingleNode(".//BE01JPersonInfoDTO");
                                //XmlNodeList personInfoList = personInfoListNode.SelectNodes("BE01JPersonInfoDTO");

                                if (personInfoListNode != null)
                                {
                                    //XmlNodeList personInfoList = personIdentityInfoListNode.SelectNodes("BE01JPersonInfoDTO");

                                    BE01JPersonInfoDTO person = new BE01JPersonInfoDTO();
                                    person.identityNo = personInfoListNode.SelectSingleNode("identityNo").InnerText.Trim();
                                    person.identityType = personInfoListNode.SelectSingleNode("identityType").InnerText.Trim();
                                    person.customerName = personInfoListNode.SelectSingleNode("customerName").InnerText.Trim();

                                    personIdentity.personInfo = person;

                                }
                                //else
                                //{
                                //    //personIdentity.personInfo = null;
                                //}
                                personsIdentities.Add(personIdentity);
                            }

                            image.personInfoList = personsIdentities.ToArray();
                            images.Add(image);
                        }

                        document.imageInfoList = images.ToArray();
                        documents.Add(document);
                    }
                    customer.documentInfoList = documents.ToArray();
                    customers.Add(customer);
                }

                businessInfo.customerInfoList = customers.ToArray();

                Util.CDBLog(string.Empty, "End: Importing data from XML file", EventLogEntryType.Information);
                return businessInfo;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Error (DWMS_CDB_Service.XmlInput(): Message={0}, StackTrace={1}",
                   ex.Message, ex.StackTrace);

                Util.CDBLog("DWMS_CDB_Service.XmlInput()", errorMessage, EventLogEntryType.Error);
                return null;
            }
        }
        protected bool GenerateXmlOutput(string filePath, BE01JOutputDTO response, BE01JBusinessInfoDTO businessInfo, BE01JSystemInfoDTO systemInfo)
        {
            try
            {
                SendToCDB sendToCDB = new SendToCDB();
                sendToCDB.BE01JSystemInfoDTO = systemInfo;
                sendToCDB.BE01JBusinessInfoDTO = businessInfo;
                sendToCDB.BE01JOutputDTO = response;
                //sendToCDB.BE01JAuthenticationDTO = authentication;


                System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(SendToCDB));

                Util.CDBDetailLog(string.Empty, String.Format("Start writing XML file: " + filePath + ".xml"), EventLogEntryType.Information);

                System.IO.StreamWriter file = new System.IO.StreamWriter(filePath + ".xml");
                writer.Serialize(file, sendToCDB);

                Util.CDBDetailLog(string.Empty, String.Format("End writing XML file"), EventLogEntryType.Information);

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("GenerateXmlOutput() Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.GenerateXmlOutput()", errorMessage, EventLogEntryType.Error);
                return false;
            }
        }



        #region common data retrieval
        protected BE01JDocumentInfoDTO[] GetDocumentInfo(DataTable docsTable, RequestorCustomer requestedCustomer, int docAppId, SendToCDBStageEnum stage, out int count)
        {
            //Get Doc info for a particular NRIC
            //Get document list for the customer
            List<BE01JDocumentInfoDTO> documentList = new List<BE01JDocumentInfoDTO>();
            count = 0;

            try
            {
                if (docsTable.Rows.Count > 0)
                {
                    Util.CDBDetailLog(string.Empty, String.Format("Found {0} Document(s) ", docsTable.Rows.Count), EventLogEntryType.Information);

                    foreach (DataRow docRow in docsTable.Rows)
                    {
                        //Get the DocId 
                        int? docId = docRow.IsNull("DocId") ? null : (int?)docRow["DocId"];

                        if (docId.HasValue)
                        {
                            DocDb docDb = new DocDb();

                            //Retrieve the doc details
                            DataTable dtDoc = docDb.GetDocDetails(docId.Value); //modified 23-01-2013, to add "Distinct" in Query

                            Util.CDBDetailLog(string.Empty, String.Format("Found {0} Doc Detail(s) for DocId: {1}", dtDoc.Rows.Count, docId.Value), EventLogEntryType.Information);

                            if (dtDoc.Rows.Count > 0)
                            {
                                BE01JDocumentInfoDTO document;

                                DataRow row = dtDoc.Rows[0];
                                if (row != null)
                                {
                                    document = new BE01JDocumentInfoDTO();

                                    // DocId, from Doc
                                    if (!String.IsNullOrEmpty(row["DocumentID"] as string))
                                    {
                                        document.docId = row["DocumentID"].ToString();
                                    }
                                    else
                                    {
                                        document.docId = string.Empty;
                                    }

                                    //DocIdSub, from DocType
                                    if (!String.IsNullOrEmpty(row["DocIdSub"] as string))
                                    {
                                        document.docIdSub = row["DocIdSub"].ToString();
                                    }
                                    else
                                    {
                                        document.docIdSub = "00";  //defaulted = 00
                                    }


                                    //DocDescription, from Doc
                                    if (!String.IsNullOrEmpty(row["DocDescription"] as string))
                                    {
                                        document.docDescription = row["DocDescription"].ToString();
                                    }
                                    else
                                    {
                                        document.docDescription = string.Empty;
                                    }


                                    //docStartDate, docEndDate from MetaData table
                                    string docType = row["DocTypeCode"] as string;
                                    MetaDataDb metaDb = new MetaDataDb();

                                    document.docStartDate = string.IsNullOrEmpty(docType) ? Format.GetDefaultDateCDB() : metaDb.GetMetaDataDocStartDate(docId.Value, docType);
                                    document.docEndDate = string.IsNullOrEmpty(docType) ? Format.GetDefaultDateCDB() : metaDb.GetMetaDataDocEndDate(docId.Value, docType);

                                    // only if there is a subcode, then take from metadata, 
                                    // CustomerSourceId is avalable, then search for NRIC of child to in HouseHold structure
                                    document.identityNoSub = string.IsNullOrEmpty(docType) ? string.Empty : metaDb.GetMetaDataIdentityNoSub(docId.Value, docType);


                                    //DocChannel, from Doc Table
                                    if (!String.IsNullOrEmpty(row["DocChannel"] as string))
                                    {
                                        document.docChannel = row["DocChannel"].ToString();
                                    }
                                    else
                                    {
                                        document.docChannel = string.Empty;
                                    }


                                    //CustomerIdSubFromSource, Doc Table
                                    if (!String.IsNullOrEmpty(row["CustomerIdSubFromSource"] as string))
                                    {
                                        document.customerIdSubFromSource = row["CustomerIdSubFromSource"].ToString();
                                    }
                                    else
                                    {
                                        document.customerIdSubFromSource = string.Empty;
                                    }

                                    //Populate imageInfo for the document
                                    // TODO: To stop sending image info at acceptance
                                    //if (stage.ToString().ToUpper() != "ACCEPT")
                                    document.imageInfoList = GetImageInfo(dtDoc, requestedCustomer, docAppId, stage);
                                    //else
                                    //    document.imageInfoList = null;

                                    if (document.docEndDate >= document.docStartDate)
                                    {
                                        documentList.Add(document);
                                        count++;
                                    }
                                }
                            }
                        }
                    }
                    return documentList.ToArray();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                Util.CDBDetailLog("DWMS_CDB_Service.DocumentInfo()", errorMessage, EventLogEntryType.Error);

                throw;
            }

        }
        private BE01JImageInfoDTO[] GetImageInfo(DataTable dtDoc, RequestorCustomer requestedCustomer, int docAppId, SendToCDBStageEnum stage)
        {

            List<BE01JImageInfoDTO> imageList = new List<BE01JImageInfoDTO>();

            try
            {
                if (dtDoc.Rows.Count > 0)
                {
                    BE01JImageInfoDTO image;

                    Util.CDBDetailLog(string.Empty, String.Format("Found {0} Image(s) ", dtDoc.Rows.Count), EventLogEntryType.Information);


                    foreach (DataRow row in dtDoc.Rows)
                    {

                        Util.CDBDetailLog(string.Empty, String.Format("Processing Image: {0} ", row["DocId"]), EventLogEntryType.Information);

                        if (row != null)
                        {

                            string docType = row["DocTypeCode"] as string;
                            int docId = int.Parse(row["DocId"].ToString().Trim());

                            image = new BE01JImageInfoDTO();


                            string downloadLink = string.Empty;
                            string fileName = string.Empty;
                            long fileSize = 0;

                            Util.GetDownloadlinkAndFileSize(docId, out downloadLink, out fileSize, out fileName);

                            // imageSize
                            double fileSizeKB = (double)fileSize / (double)1024;
                            image.imageSize = fileSizeKB.ToString();

                            // imageUrl
                            image.imageUrl = downloadLink;


                            // imageName
                            image.imageName = fileName;



                            //ImageCondition
                            if (!string.IsNullOrEmpty(row["ImageCondition"] as string))
                            {
                                image.imageCondition = row["ImageCondition"].ToString().Trim();
                            }
                            else
                            {
                                image.imageCondition = string.Empty;
                            }

                            MetaDataDb metaDb = new MetaDataDb();

                            //Util.CDBLog(string.Empty, String.Format("Checking Image: {0} DocId and DocType: {1}", docId, docType), EventLogEntryType.Information);

                            //Local Foreign
                            image.localForeign = metaDb.LocalForeignMarriageType(docId, EnumManager.GetMetadataLocalForeign(docType));

                            //Marriage Type
                            image.marriageType = metaDb.MarriageType(docId, EnumManager.GetMetadataMarriageType(docType));

                            //vrfdWithOriginal, defaulted to false, as of 2013-01-22
                            image.vrfdWithOriginal = false;



                            //VerificationDateIn from DocSet
                            image.docRecievedSourceDate = (DateTime?)Convert.ToDateTime(row["VerificationDateIn"]);

                            ////dwmsDocumentId
                            image.dwmsDocumentId = row["DocId"].ToString();


                            ////CmDocumentId
                            //Should come from Doc table, 

                            //if (stage.ToString().ToUpper() != "VERIFIED" || stage.ToString().ToUpper() != "MODIFIEDVERIFIED")
                            //{
                            image.cmDocumentId = row["CmDocumentId"].ToString();
                            //}
                            //else
                            //{
                            //    image.cmDocumentId = string.Empty;
                            //}

                            //GetMetaDataCertDate
                            image.certificateDate = (DateTime?)metaDb.GetMetaDataCertDate(docId, docType);

                            //GetMetaDataCertNumber
                            image.certificateNumber = metaDb.GetMetaDataCertNumber(docId, docType);

                            //PersonList

                            image.personInfoList = GetPersonIdentityInfo(docId, docType, requestedCustomer, docAppId, stage);


                            imageList.Add(image);
                        }

                    }
                    return imageList.ToArray();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}",
                             ex.Message, ex.StackTrace);
                Util.CDBDetailLog("DWMS_CDB_Service.GetImageInfo()", errorMessage, EventLogEntryType.Error);

                throw;
            }

            return null;
        }
        private BE01JPersonIdentityInfoDTO[] GetPersonIdentityInfo(int docId, string docType, RequestorCustomer requestedCustomer, int docAppId, SendToCDBStageEnum stage)
        {
            try
            {

                Util.CDBDetailLog(string.Empty, String.Format("Query PersonIdentity(s) Details for, DocId: {0} DocTypeCode: {1} ", docId, docType), EventLogEntryType.Information);

                DocDb docDb = new DocDb();
                DataTable dtMetaDocDetails = docDb.GetMetaDataDetails(docId, docType);
                DataTable dtDocDetails = docDb.GetDocDetails(docId);

                List<BE01JPersonIdentityInfoDTO> personIdentityList = new List<BE01JPersonIdentityInfoDTO>();

                BE01JPersonIdentityInfoDTO personIdentity1;
                BE01JPersonIdentityInfoDTO personIdentity2;
                BE01JPersonIdentityInfoDTO personIdentity3;
                BE01JPersonIdentityInfoDTO personIdentity4;


                AppPersonalDb appPersonalDb = new AppPersonalDb();

                BE01JPersonInfoDTO person;
                BE01JPersonInfoDTO person2;


                if (dtDocDetails.Rows.Count > 0)
                {
                    switch (docType)
                    {

                        //Only requested Customer
                        case "IdentityCard":
                        case "NSIDcard":
                        case "CertificateCitizen":
                        case "Passport":
                        case "EntryPermit":
                        case "EmploymentPass":
                        case "StudentPass":
                        case "SocialVisit":
                        case "BirthCertificate":
                        case "DeathCertificate":
                        case "Adoption":
                        case "DeedPoll":
                        case "OfficialAssignee":
                        case "Baptism":
                        case "PAYSLIP":
                        case "CommissionStatement":
                        case "EmploymentLetter":
                        case "BankStatement":
                        case "CPFContribution":
                        case "IRASAssesement":
                        case "IRASIR8E":
                        case "CBR":
                        case "StatementofAccounts":
                        case "PensionerLetter":
                        case "OverseasIncome":
                        case "CPFStatement":
                        case "DeclaraIncomeDetails":
                        case "ReconciliatUndertakn":
                        case "StatutoryDeclaration":
                        case "DeclarationPrivProp":
                        case "StatutoryDeclGeneral":
                        case "LoanStatementSold":
                        case "PropertyQuestionaire":
                        case "StatementSale":
                        case "CPFStatementRefund":
                        case "GLA":
                        case "LettersolicitorPOA":
                        case "ProcessingFee":
                        case "NoticeofTransfer":
                        case "OrderofCourt":
                        case "PetitionforGLA":
                        case "PurchaseAgreement":
                        case "ReceiptsLoanArrear":
                        case "SpouseFormPurchase":
                        case "SpouseFormSale":
                        case "ValuationReport":
                        case "RentalArrears":
                        case "PrisonLetter":
                        case "WarrantToAct":
                        case "LastWillDeceased":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = requestedCustomer.identityNo;
                            personIdentity1.identityType = requestedCustomer.identityType;
                            personIdentity1.customerName = requestedCustomer.customerName;
                            personIdentity1.customerIdFromSource = requestedCustomer.customerIdFromSource;
                            personIdentityList.Add(personIdentity1);
                            break;

                        case "BirthCertificatChild":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataBirthCertificatChildEnum.IdentityNo.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataBirthCertificatChildEnum.IdentityNo.ToString().Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataBirthCertificatChildEnum.IDType.ToString()) : null);
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataBirthCertificatChildEnum.NameOfChild.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);
                            break;

                        case "DeathCertificateFa":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateFaEnum.IdentityNoOfFather.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateFaEnum.IdentityNoOfFather.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateFaEnum.IDType.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateFaEnum.NameOfFather.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);
                            break;

                        case "DeathCertificateMo":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateMoEnum.IdentityNoOfMother.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateMoEnum.IdentityNoOfMother.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateMoEnum.IDType.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateMoEnum.IdentityNoOfMother.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);
                            break;

                        case "DeathCertificateSP":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateSPEnum.IdentityNoOfSpouse.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateSPEnum.IdentityNoOfSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateSPEnum.IDType.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateSPEnum.NameOfSpouse.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);
                            break;

                        case "DeathCertificateEXSP":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateEXSPEnum.IdentityNoOfEXSpouse.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateEXSPEnum.IdentityNoOfEXSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateEXSPEnum.IDType.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateEXSPEnum.NameOfEXSpouse.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);
                            break;

                        case "DeathCertificateNRIC":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateNRICEnum.IdentityNoNRIC.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateNRICEnum.IdentityNoNRIC.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateNRICEnum.IDType.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDeathCertificateNRICEnum.NameNRIC.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);
                            break;

                        case "PowerAttorney":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor1.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor1.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IDTypeDonor1.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.NameDonor1.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            if (GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor2.ToString()).Length > 0)
                            {
                                personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor2.ToString());
                                personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor2.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IDTypeDonor2.ToString()) : null;
                                personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.NameDonor2.ToString());
                            }
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);
                            personIdentityList.Add(personIdentity2);

                            personIdentity3 = new BE01JPersonIdentityInfoDTO();
                            if (GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor3.ToString()).Length > 0)
                            {
                                personIdentity3.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor3.ToString());
                                personIdentity3.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor3.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IDTypeDonor3.ToString()) : null;
                                personIdentity3.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.NameDonor3.ToString());
                            }
                            personIdentity3.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity3.identityNo);
                            personIdentityList.Add(personIdentity3);

                            personIdentity4 = new BE01JPersonIdentityInfoDTO();
                            if (GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor4.ToString()).Length > 0)
                            {
                                personIdentity4.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor4.ToString());
                                personIdentity4.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IdentityNoDonor4.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.IDTypeDonor4.ToString()) : null;
                                personIdentity4.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataPowerAttorneyEnum.NameDonor4.ToString());
                            }
                            personIdentity4.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity4.identityNo);
                            personIdentityList.Add(personIdentity4);
                            break;


                        case "MarriageCertificate":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoRequestor.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IDTypeRequestor.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IDTypeRequestor.ToString()) : null;
                            personIdentity1.customerName = appPersonalDb.GetCustomerNameByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);

                            if (GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoImageRequestor.ToString()).Length > 0)
                            {
                                person = new BE01JPersonInfoDTO();
                                person.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoImageRequestor.ToString());
                                person.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IDTypeImageRequestor.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IDTypeImageRequestor.ToString()) : null;
                                person.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.NameOfRequestor.ToString());
                                personIdentity1.personInfo = person; //Add the PersonInfo class
                            }
                            personIdentityList.Add(personIdentity1);

                            //DataTable appPersonal = appPersonalDb.GetAppPersonalsByDocAppId(docAppId);
                            personIdentity2 = new BE01JPersonIdentityInfoDTO();// edited by calvin to take value when personidentity is empty
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()) : GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoImageSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IDTypeSpouse.ToString()) : GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IDTypeImageSpouse.ToString());
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()).Length > 0 ? appPersonalDb.GetCustomerNameByDocAppIdAndNric(docAppId, personIdentity2.identityNo) : GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.NameOfSpouse.ToString());
                            //2013-03-20, change requested by calvin.
                            //personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.NameOfSpouse.ToString()) : appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);


                            if (GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()).Length > 0)
                            {
                                person2 = new BE01JPersonInfoDTO();
                                person2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoImageSpouse.ToString()) : null;
                                person2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IDTypeImageSpouse.ToString()) : null;
                                person2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.NameOfSpouse.ToString()) : null;
                                personIdentity2.personInfo = person2; //Add the PersonInfo class
                            }
                                personIdentityList.Add(personIdentity2);
                            break;

                        case "MarriageCertParent":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertParentEnum.IdentityNoParent.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertParentEnum.IdentityNoParent.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertParentEnum.IDTypeParent.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertParentEnum.NameOfParent.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertParentEnum.IdentityNoSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertParentEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertParentEnum.IDTypeSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertParentEnum.NameOfSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);
                            personIdentityList.Add(personIdentity2);
                            break;

                        case "MarriageCertLtSpouse":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertLtSpouseEnum.IdentityNoRequestor.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertLtSpouseEnum.IdentityNoRequestor.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertLtSpouseEnum.IDTypeRequestor.ToString()) : null;
                            personIdentity1.customerName = appPersonalDb.GetCustomerNameByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);

                            person = new BE01JPersonInfoDTO();
                            person.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoImageRequestor.ToString());
                            person.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IdentityNoImageRequestor.ToString().Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.IDTypeImageRequestor.ToString()) : null);
                            person.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertificateEnum.NameOfRequestor.ToString());
                            personIdentity1.personInfo = person; //Add the PersonInfo class
                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertLtSpouseEnum.IdentityNoLateSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertLtSpouseEnum.IdentityNoLateSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertLtSpouseEnum.IDTypeLateSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertLtSpouseEnum.NameOfLateSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);
                            personIdentityList.Add(personIdentity2);
                            break;

                        case "MarriageCertChild":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertChildEnum.IdentityNoChild.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertChildEnum.IdentityNoChild.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertChildEnum.IDTypeChild.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertChildEnum.NameOfChild.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertChildEnum.IdentityNoSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertChildEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertChildEnum.IDTypeSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataMarriageCertChildEnum.NameOfSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);
                            personIdentityList.Add(personIdentity2);
                            break;

                        case "DivorceCertificate":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertificateEnum.IdentityNoRequestor.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertificateEnum.IdentityNoRequestor.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertificateEnum.IDTypeRequestor.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertificateEnum.NameOfRequestor.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertificateEnum.IdentityNoSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertificateEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertificateEnum.IDTypeSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertificateEnum.NameOfSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);
                            personIdentityList.Add(personIdentity2);
                            break;

                        case "DivorceCertFather":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertFatherEnum.IdentityNoFather.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertFatherEnum.IdentityNoFather.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertFatherEnum.IDTypeFather.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertFatherEnum.NameOfFather.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);
                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertFatherEnum.IdentityNoSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertFatherEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertFatherEnum.IDTypeSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertFatherEnum.NameOfSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);
                            personIdentityList.Add(personIdentity2);
                            break;

                        case "DivorceCertMother":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertMotherEnum.IdentityNoMother.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertMotherEnum.IdentityNoMother.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertMotherEnum.IDTypeMother.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertMotherEnum.NameOfMother.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);

                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertMotherEnum.IdentityNoSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertMotherEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertMotherEnum.IDTypeSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertMotherEnum.NameOfSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);

                            personIdentityList.Add(personIdentity2);
                            break;

                        case "DivorceCertExSpouse":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertExSpouseEnum.IdentityNoRequestor.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertExSpouseEnum.IdentityNoRequestor.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertExSpouseEnum.IDTypeRequestor.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertExSpouseEnum.NameOfRequestor.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);

                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertExSpouseEnum.IdentityNoExSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertExSpouseEnum.IdentityNoExSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertExSpouseEnum.IDTypeExSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertExSpouseEnum.NameOfExSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);

                            personIdentityList.Add(personIdentity2);
                            break;

                        case "DivorceCertChild":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertChildEnum.IdentityNoChild.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertChildEnum.IdentityNoChild.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertChildEnum.IDTypeChild.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertChildEnum.NameOfChild.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);

                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertChildEnum.IdentityNoSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertChildEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertChildEnum.IDTypeSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertChildEnum.NameOfSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);

                            personIdentityList.Add(personIdentity2);
                            break;

                        case "DivorceCertNRIC":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertNRICEnum.IdentityNoNRIC.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertNRICEnum.IdentityNoNRIC.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertNRICEnum.IDTypeNRIC.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertNRICEnum.NameOfNRIC.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);

                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertNRICEnum.IdentityNoSpouse.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertNRICEnum.IdentityNoSpouse.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertNRICEnum.IDTypeSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceCertNRICEnum.NameOfSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);

                            personIdentityList.Add(personIdentity2);
                            break;


                        case "DivorceDocInterim":
                        case "DivorceDocInitial":
                        case "DivorceDocFinal":
                        case "DeedSeverance":
                        case "DeedSeparation":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceDocInterimEnum.IdentityNoRequestor.ToString());
                            personIdentity1.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceDocInterimEnum.IdentityNoRequestor.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceDocInterimEnum.IDTypeRequestor.ToString()) : null;
                            personIdentity1.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceDocInterimEnum.NameOfRequestor.ToString());
                            personIdentity1.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity1.identityNo);

                            personIdentityList.Add(personIdentity1);

                            personIdentity2 = new BE01JPersonIdentityInfoDTO();
                            personIdentity2.identityNo = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceDocInterimEnum.IdentityNoRequestor.ToString());
                            personIdentity2.identityType = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceDocInterimEnum.IdentityNoRequestor.ToString()).Length > 0 ? GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceDocInterimEnum.IDTypeSpouse.ToString()) : null;
                            personIdentity2.customerName = GetFieldValueFromDataTable(dtMetaDocDetails, DocTypeMetaDataDivorceDocInterimEnum.NameOfSpouse.ToString());
                            personIdentity2.customerIdFromSource = appPersonalDb.GetCustomerSourceIdByDocAppIdAndNric(docAppId, personIdentity2.identityNo);
                            personIdentityList.Add(personIdentity2);
                            break;

                        case "BusinessProfile":
                        case "LicenseofTrade":
                            personIdentity1 = new BE01JPersonIdentityInfoDTO();
                            personIdentity1.identityNo = null;
                            personIdentity1.identityType = null;
                            personIdentity1.customerName = null;
                            personIdentity1.customerIdFromSource = null;
                            personIdentityList.Add(personIdentity1);
                            break;


                        default:
                            break;
                    }

                    Util.CDBDetailLog(string.Empty, String.Format("Found {0} in PersonIdentity(s)", personIdentityList.Count), EventLogEntryType.Information);

                    if (personIdentityList.Count > 0)
                        return personIdentityList.ToArray();
                    else
                        return null;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);
                Util.CDBDetailLog("DWMS_CDB_Service.GetPersonIdentityInfo()", errorMessage, EventLogEntryType.Error);



                throw;
            }

            return null;

        }

        private string GetFieldValueFromDataTable(DataTable dt, string fieldName)
        {
            try
            {
                return (from r in dt.AsEnumerable()
                        where r.Field<string>("FieldName").Equals(fieldName)
                        select r).First()["FieldValue"].ToString();
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}",
                ex.Message, ex.StackTrace);

                Util.CDBLog("DWMS_CDB_Service.GetFieldValueFromDataTable()", errorMessage, EventLogEntryType.Error);

                return null;
            }
        }



        #endregion


    }
}
