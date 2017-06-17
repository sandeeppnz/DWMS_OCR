using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DWMS_OCR.App_Code.Bll
{
    class MetaDataOcr
    {
        private string fieldName = string.Empty;
        private string fieldValue = string.Empty;
        private bool verificationMandatory = true;
        private bool completenessMandatory = true;
        private bool verificationVisible = true;
        private bool completenessVisible = true;
        private bool isFixed = true;
        private int maximumLength = 50;

        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value; }
        }

        public string FieldValue
        {
            get { return fieldValue; }
            set { fieldValue = value; }
        }

        public bool VerificationMandatory
        {
            get { return verificationMandatory; }
            set { verificationMandatory = value; }
        }

        public bool CompletenessMandatory
        {
            get { return completenessMandatory; }
            set { completenessMandatory = value; }
        }

        public bool VerificationVisible
        {
            get { return verificationVisible; }
            set { verificationVisible = value; }
        }

        public bool CompletenessVisible
        {
            get { return completenessVisible; }
            set { completenessVisible = value; }
        }

        public bool IsFixed
        {
            get { return isFixed; }
            set { isFixed = value; }
        }

        public int MaximumLength
        {
            get { return maximumLength; }
            set { maximumLength = value; }
        }

        public MetaDataOcr()
        {

        }
    }
}
