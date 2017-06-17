using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using DWMS_OCR.App_Code.Helper;
using System.Collections;
using DWMS_OCR.App_Code.Dal;
using System.Diagnostics;

namespace DWMS_OCR.App_Code.Bll
{
    class MyDocSummaryXml
    {
        #region Members and Constructors
        private string xmlFilePath;
        private string dir;
        private string refNo;
        private string refType;
        private int docAppId;
        private string source;
        private int sourceId;
        private string applicationNric;
        private string acknowledgementNo;
        private int documentCount;
        private ArrayList documents;
        private bool isValid;

        private string refNoError;
        private string acknowledgementNoError;
        private string genericError;
        private string exceptionMessage;

        public string RefNo
        {
            get { return refNo; }
            set { refNo = value; }
        }

        public string RefType
        {
            get { return refType; }
            set { refType = value; }
        }

        public int DocAppId
        {
            get { return docAppId; }
            set { docAppId = value; }
        }

        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        public int SourceId
        {
            get { return sourceId; }
            set { sourceId = value; }
        }

        public string ApplicationNric
        {
            get { return applicationNric; }
            set { applicationNric = value; }
        }

        public string AcknowledgementNo
        {
            get { return acknowledgementNo; }
            set { acknowledgementNo = value; }
        }        

        public int DocumentCount
        {
            get { return documentCount; }
            set { documentCount = value; }
        }

        public ArrayList Documents
        {
            get { return documents; }
            set { documents = value; }
        }

        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; }
        }

        public string AcknowledgementNoError
        {
            get { return acknowledgementNoError; }
            set { acknowledgementNoError = value; }
        }

        public string GenericError
        {
            get { return genericError; }
            set { genericError = value; }
        }

        public string ExceptionMessage
        {
            get { return exceptionMessage; }
            set { exceptionMessage = value; }
        }

        public MyDocSummaryXml(string xmlFilePath, string dir)
        {
            this.xmlFilePath = xmlFilePath;
            this.dir = dir;
            documents = new ArrayList();
            this.docAppId = 0;
            isValid = false;

            // Parse the xml file
            ParseXmlFile();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Parse the XML file to retrieve data.
        /// </summary>
        private void ParseXmlFile()
        {
            if (!String.IsNullOrEmpty(xmlFilePath))
            {
                try
                {
                    // Load the XML file
                    XmlDocument summaryXmlDoc = new XmlDocument();
                    summaryXmlDoc.Load(xmlFilePath);

                    // Get the header info
                    XmlNodeList headerNode = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.MyDocSummaryXmlHeaderTagName);

                    if (headerNode.Count > 0)
                    {
                        XmlNode header = headerNode[0];

                        try
                        {
                            // Get the reference number and reference type
                            refNo = header["RefNo"].InnerText.Trim();

                            if (!String.IsNullOrEmpty(refNo))
                                refType = Util.GetReferenceType(refNo);
                            else
                                throw new Exception();
                        }
                        catch (Exception e)
                        {
                            refNo = string.Empty;

                            string warningMessageForLogging = String.Format("Could not retrieve reference number from XML file. {0}.", e.Message);
                            string warningMessageForUi = String.Format("Warning: Could not retrieve reference number from XML file. Message={0}", e.Message);

                            refNoError = warningMessageForUi;

                            throw new Exception(warningMessageForLogging);
                        }

                        try
                        {
                            // Get the source
                            source = header["Source"].InnerText.Trim();
                        }
                        catch (Exception)
                        {
                            source = string.Empty;
                        }

                        source = (source.ToLower().Contains("mydoc") ? "MyDoc" : source);

                        try
                            // Get the applicant Nric
                        {
                            applicationNric = header["ApplicantNric"].InnerText.Trim();
                        }
                        catch (Exception)
                        {
                            applicationNric = string.Empty;
                        }

                        try 
	                    {
                            // Get the acknowledgement number
		                    acknowledgementNo = header["AcknowledgementNo"].InnerText.Trim();
	                    }
	                    catch (Exception e)
	                    {
                            acknowledgementNo = string.Empty;

                            string warningMessageForLogging = String.Format("Warning (MyDocSummaryXml.ParseXmlFile): Could not retrieve acknowledgement number. Message={0}", e.Message);
                            string warningMessageForUi = String.Format("Warning: Could not retrieve acknowledgement number. Message={0}", e.Message);

                            Util.DWMSLog(string.Empty, warningMessageForLogging, EventLogEntryType.Warning);
                            acknowledgementNoError = warningMessageForUi;
	                    }

                        //documentCount = int.Parse(header["DocumentCount"].InnerText.Trim());

                        // Get the docAppId of the reference number
                        DocAppDb docAppDb = new DocAppDb();
                        DocApp.DocAppDataTable docAppDt = docAppDb.GetDocAppByRefNo(refNo);

                        if (docAppDt.Rows.Count > 0)
                        {
                            DocApp.DocAppRow docApp = docAppDt[0];
                            docAppId = docApp.Id;
                        }

                        // Get the equivalent source name from the database
                        MasterListDb masterListDb = new MasterListDb();
                        int masterListId = masterListDb.GetMasterListIdByName(MasterListEnum.Uploading_Channels.ToString().Replace("_", " "));

                        if (masterListId > 0)
                        {
                            MasterListItemDb masterListItemDb = new MasterListItemDb();
                            source = masterListItemDb.GetMasterListItemName(masterListId, source);
                        }

                        // Get the documents info
                        XmlNodeList documentNodes = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.MyDocSummaryXmlDocumentTagName);

                        foreach (XmlNode documentNode in documentNodes)
                        {
                            if (documentNode != null)
                            {
                                // Add the document info to the list
                                MyDocDocumentXml docXml = new MyDocDocumentXml(documentNode, dir);

                                if (docXml.IsValidFile)
                                    documents.Add(docXml);
                                else
                                    throw new Exception(String.Format("Set was not processed becuase {0} is invalid/not found.", 
                                        (dir + @"\" + docXml.FilePath.Substring(docXml.FilePath.LastIndexOf(@"\")))));
                            }
                        }

                        isValid = true;
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = String.Format("Error (MyDocSummaryXml.ParseXmlFile): XmlFilePath={0}, Message={1}, StackTrace={2}", 
                        xmlFilePath, ex.Message, ex.StackTrace);

                    Util.DWMSLog("MyDocSummaryXml.ParseXmlFile", errorMessage, EventLogEntryType.Error);

                    exceptionMessage = ex.Message;
                    genericError = "Error processing XML file.";
                    isValid = false;
                }
            }
        }
        #endregion
    }
}
