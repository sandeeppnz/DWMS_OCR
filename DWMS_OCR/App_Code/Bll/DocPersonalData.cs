using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Helper;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class DocPersonalData
    {
        #region Object Properties
        private string ocrText = string.Empty;
        private string ocrTextLower = string.Empty;
        private string nric = string.Empty;
        private string name = string.Empty;
        private string relationship = string.Empty;

        public string OcrText
        {
            get { return ocrText; }
            set { ocrText = value; }
        }

        public string OcrTextLower
        {
            get { return ocrTextLower; }
            set { ocrTextLower = value; }
        }

        public string Nric
        {
            get { return nric; }
            set { nric = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Relationship
        {
            get { return relationship; }
            set { relationship = value; }
        }

        public DocPersonalData()
        {
        }
        #endregion

        #region Object Methods
        #endregion
    }
}
