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

namespace DWMS_OCR.LeasService
{

    public class VerifySendToLEAS
    {
        public PersonInfoUpdateWebRef.BP27JDwmsCaseDTO BP27JDwmsCaseDTO;
        public PersonInfoUpdateWebRef.BP27JDwmsPersonInfoDTO BP27JDwmsPersonInfoDTO;
        public PersonInfoUpdateWebRef.BP27JDwmsMonthlyIncomeDTO BP27JDwmsMonthlyIncomeDTO;
        public PersonInfoUpdateWebRef.BP27JDwmsDocumentImageDTO BP27JDwmsDocumentImageDTO;
        public PersonInfoUpdateWebRef.BP27JDwmsResultDto BP27JDwmsResultDto;
    }

    class LeasPersonUpdateInfoUtil
    {
        public static bool isWriteXMLOuput()
        {
            return (ConfigurationManager.AppSettings["isWriteXMLOuput"].Trim().ToUpper() == "TRUE");
        }

        public static string GetUserNameDWMSToLEAS()
        {
            return ConfigurationManager.AppSettings["UserNameDWMSToLEAS"].Trim();
        }
        public static string GetPasswordDWMSToLEAS()
        {
            return ConfigurationManager.AppSettings["PasswordDWMSToLEAS"].Trim();
        }

        public static string GetDownloadImageUrlLEAS()
        {
            return ConfigurationManager.AppSettings["DownloadImageUrlLEAS"].Trim();
        }

        public static int GetMaxAttemptAllowedToSendToLEAS()
        {
            return Convert.ToInt32(ConfigurationManager.AppSettings["maxAttempstToSendToLEASEachDocAppVerify"].ToString());
        }

        protected bool GenerateXmlOutput(string filePath,  PersonInfoUpdateWebRef.BP27JDwmsCaseDTO DwmsCaseDTO, 
            PersonInfoUpdateWebRef.BP27JDwmsResultDto result)
        {
            try
            {
                VerifySendToLEAS verifySendToLEAS = new VerifySendToLEAS();
                verifySendToLEAS.BP27JDwmsCaseDTO = DwmsCaseDTO;
                verifySendToLEAS.BP27JDwmsResultDto = result;

                System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(VerifySendToLEAS));

                Util.LEASDetailLog(string.Empty, String.Format("Start writing XML file: " + filePath + ".xml"), EventLogEntryType.Information);

                System.IO.StreamWriter file = new System.IO.StreamWriter(filePath + ".xml");
                writer.Serialize(file, verifySendToLEAS);

                Util.LEASDetailLog(string.Empty, String.Format("End writing XML file"), EventLogEntryType.Information);

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("GenerateXmlOutput() Message={0}, StackTrace={1}", ex.Message, ex.StackTrace);
                Util.LEASLog("DWMS_CDB_Service.GenerateXmlOutput()", errorMessage, EventLogEntryType.Error);
                return false;
            }
        }
    }
}
