using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DWMS_OCR.App_Code.Bll
{
    class SetInfo
    {
        private int docSetId;
        private string acknowledgeNo;
        private string refNo;

        List<string> errorMessages;

        public SetInfo(int docSetId, string acknowledgeNo, string refNo)
        {
            this.docSetId = docSetId;
            this.acknowledgeNo = acknowledgeNo;
            this.refNo = refNo;

            errorMessages = new List<string>();
        }

        public void AddErrorMessage(string errorMessage)
        {
            this.errorMessages.Add(errorMessage);
        }
    }
}
