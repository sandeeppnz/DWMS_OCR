using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace DWMS_OCR.App_Code.Bll
{
    class DocOcr
    {
        private string docType = null;
        private string nric = null;
        private string hleNumber = null;
        private PageOcr firstPage = null;
        private ArrayList personalList = new ArrayList();
        private ArrayList metaDataMaintenance = new ArrayList();
        private ArrayList metaDataHardCode = new ArrayList();

        private bool isBlank = false;

        public string DocumentType
        {
            get { return docType; }
            set { docType = value; }
        }

        public string Nric
        {
            get { return nric; }
            set { nric = value; }
        }

        public string HleNumber
        {
            get { return hleNumber; }
            set { hleNumber = value; }
        }

        public PageOcr FirstPage
        {
            get { return firstPage; }
            set { firstPage = value; }
        }

        public ArrayList PersonalList
        {
            get { return personalList; }
            set { personalList = value; }
        }

        public ArrayList MetaDataMaintenance
        {
            get { return metaDataMaintenance; }
            set { metaDataMaintenance = value; }
        }

        public ArrayList MetaDataHardCode
        {
            get { return metaDataHardCode; }
            set { metaDataHardCode = value; }
        }

        public bool IsBlank
        {
            get { return isBlank; }
            set { isBlank = value; }
        }

        public DocOcr(PageOcr firstPage)
        {
            FirstPage = firstPage;
            DocumentType = firstPage.DocumentType;
            IsBlank = firstPage.IsBlank;
            //IsBlank = CheckIfDocumentIsBlank();
        }

        private bool CheckIfDocumentIsBlank()
        {
            bool result = false;

            PageOcr currPage = firstPage;
            StringBuilder ocrText = new StringBuilder();

            while(currPage.NextPage != null)
            {
                ocrText.Append(currPage.OcrText.Trim());
                currPage = currPage.NextPage;
            }

            if (ocrText.ToString().Length <= 10)
                result = true;

            return result;
        }
    }
}
