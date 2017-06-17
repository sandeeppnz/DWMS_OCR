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
    class FaxSummaryXml
    {
        #region Members and Constructors
        private string xmlFilePath;        
        private string source;
        private int sourceId;
        private ArrayList documents;
        private string dir;
        private bool isValid;
        private string acknowledgementNo;

        private string genericError;
        private string exceptionMessage;

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

        public string AcknowledgementNo
        {
            get { return acknowledgementNo; }
            set { acknowledgementNo = value; }
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

        public FaxSummaryXml(string xmlFilePath, string dir)
        {
            this.xmlFilePath = xmlFilePath;
            this.dir = dir;
            this.documents = new ArrayList();
            this.isValid = false;
            this.acknowledgementNo = string.Empty;

            // Parse the xml file
            ParseXmlFile();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Parse the XML file
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

                    // Get the properties info
                    XmlNodeList propertyNodes = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.FaxSummaryXmlPropertyTagName);

                    if (propertyNodes.Count > 0)
                    {
                        // Get the source
                        source = "FAX";

                        MasterListDb masterListDb = new MasterListDb();
                        int masterListId = masterListDb.GetMasterListIdByName(MasterListEnum.Uploading_Channels.ToString().Replace("_", " "));

                        if (masterListId > 0)
                        {
                            MasterListItemDb masterListItemDb = new MasterListItemDb();
                            source = masterListItemDb.GetMasterListItemName(masterListId, source);
                        }

                        // Get the Acknowledgement Number
                        foreach (XmlNode xmlNode in propertyNodes)
                        {
                            if (xmlNode.Attributes["name"].Value.ToUpper().Equals("REMOTECSID"))
                            {
                                if (xmlNode.HasChildNodes)
                                {
                                    XmlNode valueNode = xmlNode.FirstChild;

                                    try
                                    {
                                        FileInfo xmlFile = new FileInfo(xmlFilePath);

                                        this.acknowledgementNo = String.Format(Constants.FaxAcknowledgementNumberFormat, 
                                            Format.FormatDateTime(xmlFile.CreationTime, DateTimeFormat.yyyyMMdd_dash_HHmmss), valueNode.InnerText);

                                        break;
                                    }
                                    catch (Exception)
                                    {
                                        this.acknowledgementNo = string.Empty;
                                    }
                                }
                            }
                        }
                    }

                    // Get the files for the set
                    XmlNodeList parentFilesNodesList = summaryXmlDoc.DocumentElement.GetElementsByTagName(Constants.FaxSummaryXmlFilesTagName);

                    if (parentFilesNodesList.Count > 0)
                    {
                        XmlNode parentFilesNode = parentFilesNodesList[0];

                        if (parentFilesNode.HasChildNodes)
                        {
                            XmlNode fileNode = parentFilesNode.FirstChild;

                            if (fileNode.HasChildNodes)
                            {
                                string filePathTemp = string.Empty;

                                // Add the document info to the list
                                try
                                {
                                    filePathTemp = fileNode.FirstChild.InnerText.Trim();

                                    // format the path to get only the file name
                                    filePathTemp = filePathTemp.Substring(filePathTemp.LastIndexOf(@"\") + 1);

                                    filePathTemp = Path.Combine(dir, filePathTemp);

                                    isValid = File.Exists(filePathTemp);

                                    if (!isValid)
                                        throw new Exception();
                                }
                                catch (Exception)
                                {
                                    throw new Exception(String.Format("Invalid file {0}.  Set will not be processed.", filePathTemp));
                                }

                                documents.Add(filePathTemp);                                
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = String.Format("Error (FaxSummaryXml.ParseXmlFile): XmlFilePath={0}, Message={1}, StackTrace={2}", 
                        xmlFilePath, ex.Message, ex.StackTrace);

                    Util.DWMSLog("FaxSummaryXml.ParseXmlFile", errorMessage, EventLogEntryType.Error);

                    exceptionMessage = errorMessage;
                    genericError = "Error processing XML file.";

                    isValid = false;
                }
            }
        }
        #endregion
    }
}
