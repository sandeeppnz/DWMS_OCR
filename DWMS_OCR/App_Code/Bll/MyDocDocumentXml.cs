using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Bll
{
    class MyDocDocumentXml
    {
        #region Members and Constructors
        private int docNo;
        private string dir;
        private string filePath;
        private string docOwnerNric;
        private string docTypeCode;
        private string docTypeDescription;
        private string attachmentFileName;
        private string fileSize;

        private bool isValidFile = true;

        private XmlNode documentNode;

        public int DocNo
        {
            get { return docNo; }
            set { docNo = value; }
        }

        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }

        public string DocOwnerNric
        {
            get { return docOwnerNric; }
            set { docOwnerNric = value; }
        }

        public string DocTypeCode
        {
            get { return docTypeCode; }
            set { docTypeCode = value; }
        }

        public string DocTypeDescription
        {
            get { return docTypeDescription; }
            set { docTypeDescription = value; }
        }

        public string AttachmentFileName
        {
            get { return attachmentFileName; }
            set { attachmentFileName = value; }
        }

        public string FileSize
        {
            get { return fileSize; }
            set { fileSize = value; }
        }

        public bool IsValidFile
        {
            get { return isValidFile; }
            set { isValidFile = value; }
        }

        public MyDocDocumentXml(XmlNode documentNode, string dir)
        {
            this.documentNode = documentNode;
            this.dir = dir;

            // REtrieve the data from the node
            ParseXmlFile();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Retrieve the data from the node
        /// </summary>
        private void ParseXmlFile()
        {
            if (documentNode != null)
            {
                docNo = -1;
                docOwnerNric = string.Empty;
                docTypeCode = string.Empty;
                docTypeDescription = string.Empty;
                attachmentFileName = string.Empty;
                fileSize = string.Empty;
                filePath = string.Empty;

                try
                {                    
                    docNo = int.Parse(documentNode.Attributes["No"].InnerText.Trim());
                }
                catch (Exception)
                {                    
                }

                try
                {
                    docOwnerNric = documentNode["DocumentOwnerNric"].InnerText.Trim();
                }
                catch (Exception)
                {
                }

                try
                {
                    docTypeCode = documentNode["DocumentTypeCode"].InnerText.Trim();
                }
                catch (Exception)
                {
                }

                try
                {
                    docTypeDescription = documentNode["DocTypeDescription"].InnerText.Trim();
                }
                catch (Exception)
                {
                }

                try
                {
                    attachmentFileName = documentNode["AttachmentFilename"].InnerText.Trim().Replace(Environment.NewLine, " ");

                    FileInfo temp = new FileInfo(attachmentFileName);
                }
                catch (Exception)
                {
                }

                try
                {
                    fileSize = documentNode["Filesize"].InnerText.Trim();
                }
                catch (Exception)
                {
                }

                try
                {
                    if (!String.IsNullOrEmpty(attachmentFileName))
                        filePath = dir + "\\" + attachmentFileName;
                }
                catch (Exception)
                {
                }

                // Check if the file is a valid file or the file exists
                isValidFile = File.Exists(filePath);
            }
            
        }
        #endregion
    }
}
