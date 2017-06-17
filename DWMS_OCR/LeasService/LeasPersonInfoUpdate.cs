using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using DWMS_OCR.App_Code.Helper;
using DWMS_OCR.PersonInfoUpdateWebRef;
using DWMS_OCR.App_Code.Bll;
using DWMS_OCR.App_Code.Dal;
using System.Xml;
using System.IO;
using System.Web;


namespace DWMS_OCR.LeasService
{
    class LeasPersonInfoUpdate : LeasPersonUpdateInfoUtil
    {
        public bool XmlOutput { get; set; }

        public void SendDocsToLeas()
        {

            try
            {
                BP27JDwmsPersonInfoUpdateService webRef = new BP27JDwmsPersonInfoUpdateService();
                DocSetDb docSetDb = new DocSetDb();
                DocAppDb docAppDb = new DocAppDb();
                bool logging = Util.Logging();
                bool detailLogging = Util.DetailLogging();

                using (DataTable dtDocApp = IncomeAssessmentDb.GetDocAppByStatus("Extracted", SendToLEASStatusEnum.Ready.ToString()))
                {
                    if (dtDocApp.Rows.Count > 0)
                    {
                        if (logging) Util.LEASLog(string.Empty, String.Format("Applications: {0} to be sent.", dtDocApp.Rows.Count), EventLogEntryType.Information);
                        foreach (DataRow row in dtDocApp.Rows)
                        {
                            if (((int)row["SentToLeasAttemptCount"] < LeasPersonUpdateInfoUtil.GetMaxAttemptAllowedToSendToLEAS()) || row.IsNull("SentToLeasAttemptCount"))
                            {
                               // bool hasSentToLeas = false;
                                SendToLEASStatusEnum hasSentToLeas = SendToLEASStatusEnum.Ready;
                                int attemptCount = 0;
                                for (attemptCount = 0; attemptCount < LeasPersonUpdateInfoUtil.GetMaxAttemptAllowedToSendToLEAS(); attemptCount++)
                                {
                                    Util.LEASLog("DWMS_LEAS_Service.SendDocsToLeas()", "Starting to attempt send for " + row["RefNo"].ToString() + " Attempt Count = " + attemptCount.ToString(), EventLogEntryType.Warning);
                                    hasSentToLeas = UpdatePersonalInfo(webRef, row);
                                    if (hasSentToLeas == SendToLEASStatusEnum.Sent)
                                        break;
                                }
                                #region Modified by Edward 23/2/2014 Add New LeasStatus 23/2/2014
                                //if (hasSentToLeas == SendToLEASStatusEnum.Sent)
                                    //docAppDb.UpdateSentToLeasStatus(int.Parse(row["DocAppId"].ToString()), SendToLEASStatusEnum.Sent.ToString(), attemptCount);
                               // else
                                    //docAppDb.UpdateSentToLeasStatus(int.Parse(row["DocAppId"].ToString()), SendToLEASStatusEnum.SentButFailed.ToString(), attemptCount);
                                docAppDb.UpdateSentToLeasStatus(int.Parse(row["DocAppId"].ToString()), hasSentToLeas.ToString(), attemptCount);
                                Util.LEASLog("DWMS_LEAS_Service.SendDocsToLeas()", row["Refno"].ToString() + " LEAS Status = " + hasSentToLeas.ToString(), EventLogEntryType.Warning);
                                #endregion
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                Util.LEASLog("DWMS_LEAS_Service.SendDocsToLeas()", errorMessage, EventLogEntryType.Error);
            }
        }

        private SendToLEASStatusEnum UpdatePersonalInfo(BP27JDwmsPersonInfoUpdateService webRef, DataRow row)
        {
            bool hasInputInfo = true;
            string filePath = string.Empty;
            string errorPath = string.Empty;
            string checkedFilePath = string.Empty;
            //bool hasDocSetSuccessfullySentToLeas = true;
            SendToLEASStatusEnum hasDocSetSuccessfullySentToLeas = SendToLEASStatusEnum.Ready;
            BP27JDwmsCaseDTO CaseInfo = new BP27JDwmsCaseDTO();
            BP27JDwmsAuthenticationDTO AuthenticationInfo = new BP27JDwmsAuthenticationDTO();
            BP27JDwmsResultDto result = new BP27JDwmsResultDto();
            LogActionDb logActionDb = new LogActionDb();
            bool connError = false;

            CaseInfo.numHla = row["RefNo"].ToString();

            filePath = GetFilePathAndName(CaseInfo.numHla, 1);
            string userName = IncomeAssessmentDb.GetUserNameByAssessmentStaffId((Guid)row["AssessmentStaffUserId"]);

            Util.LEASLog(string.Empty, String.Format("Starting to get docs for DocAppId {0} : ", row["DocAppId"].ToString()), EventLogEntryType.Information);
            hasInputInfo = GetVerifiedDocs(CaseInfo, AuthenticationInfo, row, userName);                                                        
            
            checkedFilePath = filePath;

            if (!hasInputInfo)
            {
                Util.LEASDetailLog(string.Empty, String.Format("VERIFY: Retrieval of data for, DocAppId({0}) failed, this DocApp will not be sent to LEAS.", row["DocAppId"].ToString()), EventLogEntryType.Error);
                checkedFilePath = checkedFilePath + ".InpErr";
                //hasDocSetSuccessfullySentToLeas = false;
                hasDocSetSuccessfullySentToLeas = SendToLEASStatusEnum.FailedInpErr;    
            }
            else
            {
                try
                {
                    for (int cnt = 0; cnt < 3; cnt++)
                    {
                        webRef.Timeout = 300000;//add timeout of 5min
                        Util.LEASDetailLog(string.Empty, String.Format("VERIFY: Start sending to Leas attempt " + cnt + 1), EventLogEntryType.Warning);
                        result = webRef.updatePersonInfo(AuthenticationInfo, CaseInfo);
                        if (result.ToString().Length > 0)
                        {
                            connError = false;
                            Util.CDBDetailLog(string.Empty, String.Format("VERIFY: Successfully sent to Leas"), EventLogEntryType.Information);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    connError = true;
                    //hasDocSetSuccessfullySentToLeas = false;
                    hasDocSetSuccessfullySentToLeas = SendToLEASStatusEnum.FailedConnErr;   
                    Util.LEASLog(string.Empty, String.Format("VERIFY: Connection to Leas attempt failed, Message: " + ex.Message.Substring(0, 30) + ", StackTrace: " + ex.StackTrace), EventLogEntryType.Error);
                    logActionDb.Insert((Guid)row["AssessmentStaffUserId"], "Set sent to Leas with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)row["DocAppId"]);
                }
                #region write connection error file
                if (connError)
                {
                    checkedFilePath = filePath + ".ConnErr";
                    logActionDb.Insert((Guid)row["AssessmentStaffUserId"], "Set sent to Leas with error", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)row["DocAppId"]);
                }
                #endregion
            }

            if (!connError && hasInputInfo)
            {
                if (ValidateLeasVerifyForDocSet(result) == SendToLEASStatusEnum.Sent) 
                    checkedFilePath = filePath;
                else
                    checkedFilePath = filePath + ".OutErr";

                Util.LEASDetailLog(string.Empty, String.Format("VERIFY: Start writing output result to file: " + checkedFilePath), EventLogEntryType.Information);
                bool resultWrite = ProcessOutput(result, CaseInfo);

                if (hasDocSetSuccessfullySentToLeas == SendToLEASStatusEnum.Ready)  
                    hasDocSetSuccessfullySentToLeas = ValidateLeasVerifyForDocSet(result);
                if (hasDocSetSuccessfullySentToLeas == SendToLEASStatusEnum.Sent)
                    logActionDb.Insert((Guid)row["AssessmentStaffUserId"], "Set sent to Leas successfully", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)row["DocAppId"]);
                else
                    logActionDb.Insert((Guid)row["AssessmentStaffUserId"], "Set sent to Leas failed", userName, string.Empty, string.Empty, string.Empty, LogTypeEnum.S, (int)row["DocAppId"]);
                //Util.CDBDetailLog(string.Empty, String.Format("VERIFY: End writing output result to file sent from CDB"), EventLogEntryType.Information);
            }

            if (XmlOutput)
                GenerateXmlOutput(checkedFilePath, CaseInfo, result);
            return hasDocSetSuccessfullySentToLeas;

        }


        private bool GetVerifiedDocs(BP27JDwmsCaseDTO CaseInfo, BP27JDwmsAuthenticationDTO AuthenticationInfo, DataRow row, string userName)
        {
            try
            {
                List<BP27JDwmsPersonInfoDTO> PersonInfoList = new List<BP27JDwmsPersonInfoDTO>();
                using (DataTable dtAppPersonal = IncomeAssessmentDb.GetAppPersonalByDocAppId(int.Parse(row["DocAppId"].ToString())))
                {
                    if (dtAppPersonal.Rows.Count > 0)
                    {
                        foreach (DataRow rowAppPersonal in dtAppPersonal.Rows)
                        {
                            BP27JDwmsPersonInfoDTO PersonInfo = GetPersonInfo(rowAppPersonal);
                            if (PersonInfo != null)
                                PersonInfoList.Add(PersonInfo);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                Util.LEASLog(string.Empty, String.Format("Finished getting personInfo for DocAppId {0} : ", row["DocAppId"].ToString()), EventLogEntryType.Information);

                CaseInfo.personDetail = PersonInfoList.ToArray();

                BP27JDwmsDocumentImageDTO docImage = new BP27JDwmsDocumentImageDTO();
                docImage.url = GetDownloadImageUrlLEAS() + row["DocAppId"].ToString();

                Util.LEASLog(string.Empty, String.Format("Finished getting DocImage for DocAppId {0} : ", row["DocAppId"].ToString()), EventLogEntryType.Information);

                CaseInfo.numUserId = userName;
                CaseInfo.docImage = docImage;

                AuthenticationInfo.userName = LeasPersonUpdateInfoUtil.GetUserNameDWMSToLEAS();
                AuthenticationInfo.password = LeasPersonUpdateInfoUtil.GetPasswordDWMSToLEAS();

                if (PersonInfoList.Count < 0)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Util.LEASLog(string.Empty, String.Format("Error for DocAppId in GetVerifiedDocs {0}, {1} ", row["DocAppId"].ToString(), ex.Message), EventLogEntryType.Error);
                return false;
            }
            
        }


        private BP27JDwmsPersonInfoDTO GetPersonInfo(DataRow rowDocApp)
        {
            BP27JDwmsPersonInfoDTO PersonInfo = new BP27JDwmsPersonInfoDTO();

            PersonInfo.numNric = rowDocApp["Nric"].ToString();

            DataTable dtAmount = IncomeAssessmentDb.GetIncomeAmount(int.Parse(rowDocApp["DocAppId"].ToString()), int.Parse(rowDocApp["Id"].ToString()), "ALLOWANCE");
            if (dtAmount.Rows.Count > 0)
                PersonInfo.amtAvgAllowance = Math.Floor(decimal.Parse(dtAmount.Rows[0]["TotalAmount"].ToString())).ToString();

            dtAmount = IncomeAssessmentDb.GetIncomeAmount(int.Parse(rowDocApp["DocAppId"].ToString()), int.Parse(rowDocApp["Id"].ToString()), "OVERTIME");
            if (dtAmount.Rows.Count > 0)
                PersonInfo.amtAvgOvertime = Math.Floor(decimal.Parse(dtAmount.Rows[0]["TotalAmount"].ToString())).ToString();

            dtAmount = IncomeAssessmentDb.GetIncomeAmount(int.Parse(rowDocApp["DocAppId"].ToString()), int.Parse(rowDocApp["Id"].ToString()), "CA");
            if (dtAmount.Rows.Count > 0)
                PersonInfo.amtCaIncome = Math.Floor(decimal.Parse(dtAmount.Rows[0]["TotalAmount"].ToString())).ToString();

            List<BP27JDwmsMonthlyIncomeDTO> MonthlyIncomeList = new List<BP27JDwmsMonthlyIncomeDTO>();
            //using (DataTable dtMonthLyIncome = IncomeAssessmentDb.GetDataForIncomeAssessment(int.Parse(row["DocAppId"].ToString()), int.Parse(row["Id"].ToString())))
            //using (DataTable dtMonthLyIncome = IncomeAssessmentDb.GetDataForIncomeAssessment(int.Parse(rowDocApp["DocAppId"].ToString()), rowDocApp["Nric"].ToString()))
            using (DataTable dtMonthLyIncome = IncomeAssessmentDb.GetDataForIncomeAssessment(int.Parse(rowDocApp["Id"].ToString())))    //Modified by Edward 10/3/2014 
            {
                //DataTable dt = dtMonthLyIncome.AsEnumerable().OrderByDescending(c => c.Field<int>("IncomeYear")).ThenByDescending(c => c.Field<int>("IncomeMonth")).AsDataView().ToTable();

                if (dtMonthLyIncome.Rows.Count > 0)
                {
                    List<string> listAmount = new List<string>();
                    List<DateTime> listMonth = new List<DateTime>();

                    int MonthToLeas = int.Parse(dtMonthLyIncome.Rows[0]["MonthsToLeas"].ToString());
                    int i = 0;
                    foreach (DataRow rowMonthlyIncome in dtMonthLyIncome.Rows)
                    {
                        if (i != MonthToLeas)
                        {
                            listAmount.Add(!rowMonthlyIncome.IsNull("GrossIncome") || !string.IsNullOrEmpty(rowMonthlyIncome["GrossIncome"].ToString())
                                ? Math.Floor(decimal.Parse(rowMonthlyIncome["GrossIncome"].ToString())).ToString() : string.Empty);
                            listMonth.Add(!rowMonthlyIncome.IsNull("IncomeYear") || !string.IsNullOrEmpty(rowMonthlyIncome["IncomeYear"].ToString())
                                ? new DateTime(int.Parse(rowMonthlyIncome["IncomeYear"].ToString()), int.Parse(rowMonthlyIncome["IncomeMonth"].ToString()), 1)
                                : new DateTime(1, 1, 1));
                            i++;
                        }
                        else
                            break;
                    }
                    listAmount.Reverse();
                    listMonth.Reverse();

                    for (int j = 0; j < i; j++)
                    {
                        BP27JDwmsMonthlyIncomeDTO MonthlyInfo = new BP27JDwmsMonthlyIncomeDTO();
                        MonthlyInfo.amtIncome = listAmount[j];
                        MonthlyInfo.dteIncome = listMonth[j];
                        MonthlyIncomeList.Add(MonthlyInfo);
                    }

                }

            }
            PersonInfo.monthlyIncome = MonthlyIncomeList.ToArray();
            return PersonInfo;


        }

        private string GetFilePathAndName(string numHla, int attemptCount)
        {
            return Path.Combine(Retrieve.GetWebServiceForOcrDirPath(), string.Format(Constants.VerifyDWMSToLEASLogFileName, numHla, Format.FormatDateTimeCDB(DateTime.Now, Format.DateTimeFormatCDB.yyyyMMdd_dash_HHmmss), attemptCount));
        }


        private bool ProcessOutput(BP27JDwmsResultDto response,BP27JDwmsCaseDTO CaseInfo )
        {
            if (response.errorCode != "0000")
            {

                ExceptionLogDb exceptionLogDb = new ExceptionLogDb();
                string errorReason = "Doc send to Leas failed.";
                string errorMessage = response.errorCode + " - " + response.errorMessage;

                // TODO : to be removed
                exceptionLogDb.LeasExceptionLogInsert(string.Empty, CaseInfo.numHla, DateTime.Now, errorReason, errorMessage);
            }

            return true;
        }

        private SendToLEASStatusEnum ValidateLeasVerifyForDocSet(BP27JDwmsResultDto output)
        {
            if (output != null)
            {
                //Check if the OutputDTO result flag,
                string strOutput = !string.IsNullOrEmpty(output.errorCode) ? output.errorCode.Trim() : "0000";
                if (strOutput.Trim() == "0000")
                {
                    return SendToLEASStatusEnum.Sent; 
                }
                else
                {
                    return SendToLEASStatusEnum.FailedOutErr;
                }
            }
            else
            {
                return SendToLEASStatusEnum.FailedOutErr; 
            }
        }
    }
}
