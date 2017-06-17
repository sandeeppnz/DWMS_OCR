using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DWMS_OCR.App_Code.Helper
{
    class Constants
    {
        public static readonly char[] OcrTextLineSeperators = new char[] { '\r', '\n', ':', ' ', ',' };
        public static readonly char[] NewLineSeperators = new char[] { '\r', '\n' };

        public static readonly string HleNumberRefPrefix = "Ref";

        public static readonly string KeywordSeperator = "_&&_";

        public static readonly string MyDocSummaryXmlFileName = "Summary?.xml";
        public static readonly string MyDocSummaryXmlHeaderTagName = "Header";
        public static readonly string MyDocSummaryXmlDocumentTagName = "Document";

        public static readonly string FaxSummaryXmlPropertiesTagName = "properties";
        public static readonly string FaxSummaryXmlPropertyTagName = "property";
        public static readonly string FaxSummaryXmlFilesTagName = "files";
        public static readonly string FaxSummaryXmlFileTagName = "file";

        public static readonly string WebServiceSetXmlSetTagName = "SET";
        public static readonly string WebServiceSetXmlRefNoTagName = "REFNO";
        public static readonly string WebServiceSetXmlChannelTagName = "CHANNEL";
        public static readonly string WebServiceSetXmlHasDocIdTagName = "HASDOCID";
        public static readonly string WebServiceSetXmlCustomerTagName = "CUSTOMER";
        public static readonly string WebServiceSetXmlCustIdTagName = "CUSTOMERIDFROMSOURCE";
        public static readonly string WebServiceSetXmlCustNameTagName = "CUSTNAME"; 
        public static readonly string WebServiceSetXmlNricTagName = "NRIC";
        public static readonly string WebServiceSetXmlIdTypeTagName = "IDENTITYTYPE";
        public static readonly string WebServiceSetXmlCustomerTypeTagName = "CUSTOMERTYPE";

        public static readonly string WebServiceSetXmlDocTagName = "DOC";

        //DocumentClass as of 2013-01-24
        public static readonly string WebServiceSetXmlDocIdTagName = "DOCID";
        public static readonly string WebServiceSetXmlDocSubIdTagName = "DOCSUBID";
        public static readonly string WebServiceSetXmlDocDescriptionTagName = "DOCDESCRIPTION";
        public static readonly string WebServiceSetXmlDocStartDateTagName = "DOCSTARTDATE";
        public static readonly string WebServiceSetXmlDocEndDateTagName = "DOCENDDATE";
        public static readonly string WebServiceSetXmlIdentityNoSubTagName = "IDENTITYNOSUB";
        public static readonly string WebServiceSetXmlCustomerIdSubFromSourceTagName = "CUSTOMERIDSUBFROMSOURCE";
        public static readonly string WebServiceSetXmlRequesterNicknameTagName = "REQUESTERNICKNAME";


        //ImageInfoClass as of 2013-01-24
        public static readonly string WebServiceSetXmlImageUrlTagName = "IMAGEURL";
        public static readonly string WebServiceSetXmlImageNameTagName = "IMAGENAME";
        public static readonly string WebServiceSetXmlImageSizeTagName = "IMAGESIZE";
        public static readonly string WebServiceSetXmlDateReceivedFromSourceTagName = "DATERECEIVEDFROMSOURCE";
        public static readonly string WebServiceSetXmlDocChannelTagName = "DOCCHANNEL";
        public static readonly string WebServiceSetXmlCmDocumentIdTagName = "CMDOCUMENTID";
        public static readonly string WebServiceSetXmlIsAcceptedTagName = "ISACCEPTED";
        public static readonly string WebServiceSetXmlIsMatchedWithExternalOrgTagName = "ISMATCHEDWITHEXTERNALORG";
        public static readonly string WebServiceSetXmlDateFiledTagName = "DATEFILED";
        public static readonly string WebServiceSetXmlCertNoTagName = "CERTIFICATENO";
        public static readonly string WebServiceSetXmlCertDateTagName = "CERTIFICATEDATE";
        public static readonly string WebServiceSetXmlLocalForeignTagName = "LOCALFOREIGN";
        public static readonly string WebServiceSetXmlMarriageTypeTagName = "MARRIAGETYPE";
        public static readonly string WebServiceSetXmlIsVerifiedTagName = "ISVERIFIED";


        //Required for the structure of the XML generated
        public static readonly string WebServiceSetXmlFileTagName = "FILE";
        public static readonly string WebServiceSetXmlNameTagName = "NAME"; //referes to the ImageName, in the constructed xml by the DWMS webservice of CDB
        public static readonly string WebServiceSetXmlMetaDataTagName = "METADATA";


        

        public static readonly string SetNumberFormat = "{0}{1}{2}-{3}"; // Where {0}=Group Code; {1}=Operation Code; {2}=Date In and {3}=Sequential Number
        public static readonly string FaxAcknowledgementNumberFormat = "{0}-{1}"; // Where {0}=Date and {1}=Remote CSID

        public const int MIN_STR_LENGTH = 20;

        public const string DWMSLogSource = "DWMSOCR";
        public const string DWMSLog = "DWMSNewOCRLog";
        public const string DWMSSampleLogSource = "DWMSSampleDocOCR";
        public const string DWMSSampleLog = "DWMSSampleDocOCRLog";
        public const string DWMSMaintenanceLogSource = "DWMSMaintenance";
        public const string DWMSMaintenanceLog = "DWMSMaintenanceLog";

        public const string DWMSCDBLogSource = "DWMS_CDB";
        public const string DWMSCDBLog = "DWMS_CDB_Log";

        #region Added By Edward for Leas Service
        public const string DWMSLEASLogSource = "DWMS_LEAS";
        public const string DWMSLEASLog = "DWMS_LEAS_Log";
        public const string VerifyDWMSToLEASLogFileName = "Sent-LEAS-{0}-{1}_{2}"; //first parameter, is set for the ref - datetime stamp,  
        #endregion

        public const int MinimumFileAgeToProcessForMaintenanceInDays = 10;

        public const string VerifyDWMSToCDBLogFileName = "Verified-CDB-{0}-{1}_{2}"; //first parameter, is set for the ref - datetime stamp, 
        public const string AcceptDWMSToCDBLogFileName = "Modified-CDB-{0}-{1}_{2}"; //first parameter, is set for the ref - datetime stamp, 
        public const string CompleteAcceptDWMSToCDBLogFileName = "Accept-CDB-{0}-{1}_{2}"; //first parameter, is set for the ref - datetime stamp,         

        public const string WebServiceNullDate = "0001-01-01";


    }
}
