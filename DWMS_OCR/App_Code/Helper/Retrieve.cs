using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using DWMS_OCR.App_Code.Bll;
using System.Data;

namespace DWMS_OCR.App_Code.Helper
{
    class Retrieve
    {
        /// <summary>
        /// Get the Maximum Ocr Notification Mail Sent
        /// </summary>
        /// <returns></returns>
        public static string GetMaximumNotifSent()
        {
            return ConfigurationManager.AppSettings["MaximumNotifSent"].ToString();
        }

        /// <summary>
        /// Get the Maximum Time Ocr Not Being Responsive to Trigger Notification Part
        /// </summary>
        /// <returns></returns>
        public static string GetMaxTimeOcrNotWorkingTrigger()
        {
            return ConfigurationManager.AppSettings["MaxTimeOcrNotWorkingTrigger"].ToString();
        }

        /// <summary>
        /// Get the ForOcr directory path.
        /// </summary>
        /// <returns>ForOcr dir path</returns>
        public static string GetDocsForOcrDirPath()
        {
            return ConfigurationManager.AppSettings["DocsForOcrFolder"].ToString();
        }

        /// <summary>
        /// Get the RawPage directory path.
        /// </summary>
        /// <returns>RawPage dir path</returns>
        public static string GetRawPageOcrDirPath()
        {
            return ConfigurationManager.AppSettings["RawPageOcrFolder"].ToString();
        }

        /// <summary>
        /// Get the SampleDocs directory path.
        /// </summary>
        /// <returns>SampleDoc dir path</returns>
        public static string GetSampleDocsForOcrDirPath()
        {
            return ConfigurationManager.AppSettings["SampleForOcrDocsFolder"].ToString();
        }

        /// <summary>
        /// Get the MyDoc directory path.
        /// </summary>
        /// <returns>MyDoc dir path</returns>
        public static string GetMyDocForOcrDirPath()
        {
            return ConfigurationManager.AppSettings["MyDocOcrFolder"].ToString();
        }

        /// <summary>
        /// Get the Fax directory path.
        /// </summary>
        /// <returns>Fax dir path</returns>
        public static string GetFaxForOcrDirPath()
        {
            return ConfigurationManager.AppSettings["FaxOcrFolder"].ToString();
        }

        /// <summary>
        /// Get the Scan directory path.
        /// </summary>
        /// <returns>Scan dir path</returns>
        public static string GetScanForOcrDirPath()
        {
            return ConfigurationManager.AppSettings["ScanOcrFolder"].ToString();
        }

        /// <summary>
        /// Get the Email directory path.
        /// </summary>
        /// <returns>Email dir path</returns>
        public static string GetEmailForOcrDirPath()
        {
            return ConfigurationManager.AppSettings["EmailOcrFolder"].ToString();
        }

        /// <summary>
        /// Get the WebService directory path.
        /// </summary>
        /// <returns>WebService dir path</returns>
        public static string GetWebServiceForOcrDirPath()
        {
            return ConfigurationManager.AppSettings["WebServiceOcrFolder"].ToString();
        }

        public static string GetEmailDomain()
        {
            return ConfigurationManager.AppSettings["EmailDomain"].ToString();
        }

        public static string GetDWMSDomain()
        {
            return ConfigurationManager.AppSettings["DWMSDomain"].ToString();
        }
        
        /// <summary>
        /// Get the MyDoc Imported directory path.
        /// </summary>
        /// <returns>MyDoc imported dir path</returns>
        public static string GetImportedMyDocsOcrDirPath()
        {
            return ConfigurationManager.AppSettings["ImportedMyDocOcrFolder"].ToString();
        }

        /// <summary>
        /// Return the path of the Images to be OCR'ed
        /// </summary>
        /// <returns></returns>
        public static string GetImportedFaxOcrDirPath()
        {
            return ConfigurationManager.AppSettings["ImportedFaxOcrFolder"].ToString();
        }

        /// <summary>
        /// Imported Scan folder
        /// </summary>
        /// <returns></returns>
        public static string GetImportedScanOcrDirPath()
        {
            return ConfigurationManager.AppSettings["ImportedScanOcrFolder"].ToString();
        }

        /// <summary>
        /// Imported Email folder
        /// </summary>
        /// <returns></returns>
        public static string GetImportedEmailOcrDirPath()
        {
            return ConfigurationManager.AppSettings["ImportedEmailOcrFolder"].ToString();
        }


       
        /// <summary>
        /// Retrieve the Failed MyDocs folder
        /// </summary>
        /// <returns></returns>
        public static string GetFailedMyDocsOcrDirPath()
        {
            return ConfigurationManager.AppSettings["FailedMyDocOcrFolder"].ToString();
        }

        /// <summary>
        /// Retrieve the Failed Fax folder
        /// </summary>
        /// <returns></returns>
        public static string GetFailedFaxOcrDirPath()
        {
            return ConfigurationManager.AppSettings["FailedFaxOcrFolder"].ToString();
        }

        /// <summary>
        /// Failed Scan flder
        /// </summary>
        /// <returns></returns>
        public static string GetFailedScanOcrDirPath()
        {
            return ConfigurationManager.AppSettings["FailedScanOcrFolder"].ToString();
        }

        /// <summary>
        /// Failed Email flder
        /// </summary>
        /// <returns></returns>
        public static string GetFailedEmailOcrDirPath()
        {
            return ConfigurationManager.AppSettings["FailedEmailOcrFolder"].ToString();
        }

        /// <summary>
        /// Imported Web Service folder
        /// </summary>
        /// <returns></returns>
        public static string GetImportedWebServiceOcrDirPath()
        {
            return ConfigurationManager.AppSettings["ImportedWebServiceOcrFolder"].ToString();
        }

        /// <summary>
        /// Failed Web Service folder
        /// </summary>
        /// <returns></returns>
        public static string GetFailedWebServiceOcrDirPath()
        {
            return ConfigurationManager.AppSettings["FailedWebServiceOcrFolder"].ToString();
        }

        /// <summary>
        /// Return the path of the Images to be OCR'ed
        /// </summary>
        /// <returns></returns>
        public static string GetSpellCheckerDirPath()
        {
            return ConfigurationManager.AppSettings["SpellCheckLibraryFolder"].ToString();
        }

        /// <summary>
        /// Get the connection string from the web.config file.
        /// </summary>
        /// <returns>Connection string</returns>
        public static ConnectionStringSettings GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DWMS_OCR.Properties.Settings.ASPNETDBConnectionString"];
        }

        /// <summary>
        /// Get the Temp Dir folder
        /// </summary>
        /// <returns></returns>
        public static string GetTempDirPath()
        {
            return ConfigurationManager.AppSettings["TempFolder"].ToString();
        }

        public static void GetHunspellResourcesPath(out string libAffPath, out string libDicPath)
        {
            libAffPath = Path.Combine(GetSpellCheckerDirPath(), "en_US.aff");
            libDicPath = Path.Combine(GetSpellCheckerDirPath(), "en_US.dic");
        }

        public static Guid GetSystemGuid()
        {
            // Get the imported by id
            Guid importedBy = Guid.NewGuid();
            DWMS_OCR.App_Code.Bll.ProfileDb profileDb = new DWMS_OCR.App_Code.Bll.ProfileDb();
            Guid? systemGuid = profileDb.GetSystemGuid();
            if (systemGuid.HasValue)
                importedBy = systemGuid.Value;

            return importedBy;
        }






        /// <summary>
        /// Get IdType by nric
        /// </summary>
        /// <param name="nric"></param>
        /// <returns></returns>
        public static string GetIdTypeByNRIC(string nric)
        {
            if (string.IsNullOrEmpty(nric))
                return string.Empty;
            else if (Validation.IsNric(nric))
                return IDTypeEnum.UIN.ToString();
            else if (Validation.IsFin(nric))
                return IDTypeEnum.FIN.ToString();
            else
                return IDTypeEnum.XIN.ToString();
        }
    }
}
