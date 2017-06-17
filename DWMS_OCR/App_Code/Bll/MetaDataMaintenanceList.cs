using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class MetaDataMaintenanceList
    {
        private string docType = string.Empty;
        private ArrayList metaData = new ArrayList();

        public ArrayList MetaData
        {
            get { return metaData; }
            set { metaData = value; }
        }

        public MetaDataMaintenanceList(string docType)
        {
            this.docType = docType;

            SetMetaData();
        }

        /// <summary>
        /// Set the meta data
        /// </summary>
        private void SetMetaData()
        {
            ArrayList metaList = new ArrayList();

            MetaFieldDb metaFieldDb = new MetaFieldDb();
            MetaField.MetaFieldDataTable metaFieldDt = metaFieldDb.GetMetaFieldByDocTypeCode(this.docType);

            foreach (MetaField.MetaFieldRow metaField in metaFieldDt.Rows)
            {
                MetaDataOcr metaDataOcr = new MetaDataOcr();
                metaDataOcr = new MetaDataOcr();
                metaDataOcr.FieldName = metaField.FieldName;
                metaDataOcr.FieldValue = string.Empty;
                metaDataOcr.VerificationMandatory = metaField.VerificationMandatory;
                metaDataOcr.CompletenessMandatory = metaField.CompletenessMandatory;
                metaDataOcr.VerificationVisible = metaField.VerificationVisible;
                metaDataOcr.CompletenessVisible = metaField.CompletenessVisible;
                metaDataOcr.IsFixed = metaField.Fixed;

                metaList.Add(metaDataOcr);
            }

            if (metaList.Count > 0)
                MetaData = metaList;
        }
    }
}
