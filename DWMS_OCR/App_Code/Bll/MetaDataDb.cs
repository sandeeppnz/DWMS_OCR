using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.MetaDataTableAdapters;
using DWMS_OCR.App_Code.Dal;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Bll
{
    class MetaDataDb
    {
        private MetaDataTableAdapter _MetaDataTableAdapter = null;

        protected MetaDataTableAdapter Adapter
        {
            get
            {
                if (_MetaDataTableAdapter == null)
                    _MetaDataTableAdapter = new MetaDataTableAdapter();

                return _MetaDataTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public MetaData.MetaDataDataTable GetMetaDatas()
        {
            return Adapter.GetData();
        }


        public DateTime? GetMetaDataDocStartDate(int docId, string docType)//edit by Calvin
        {
            try
            {
                string fieldName = EnumManager.GetMetadataStartDate(docType);
                string fieldValue = Adapter.GetFieldValueByDocIdAndFieldName(fieldName, docId);
                if (string.IsNullOrEmpty(fieldValue))
                    return Format.GetDefaultDateCDB();
                else
                    return Format.GetMetaDataValueInMetaDataDateFormatCDB(fieldValue);
            }
            catch
            {
                return Format.GetDefaultDateCDB();
            }
        }



        public DateTime? GetMetaDataDocEndDate(int docId, string docType)
        {
            try
            {
                string fieldName = EnumManager.GetMetadataEndDate(docType);
                string fieldValue = Adapter.GetFieldValueByDocIdAndFieldName(fieldName, docId);
                if (string.IsNullOrEmpty(fieldValue))
                    return Format.GetDefaultDateCDB();
                else
                    return Format.GetMetaDataValueInMetaDataDateFormatCDB(fieldValue);
            }
            catch
            {
                return Format.GetDefaultDateCDB();
            }
        }



        public DateTime? GetMetaDataCertDate(int docId, string docType)
        {
            try
            {
                string fieldName = EnumManager.GetMetadataCertDate(docType);
                string fieldValue = Adapter.GetFieldValueByDocIdAndFieldName(fieldName, docId);
                if (string.IsNullOrEmpty(fieldValue))
                    return Format.GetDefaultDateCDB();
                else
                    return Format.GetMetaDataValueInMetaDataDateFormatCDB(fieldValue);
            }
            catch
            {
                return Format.GetDefaultDateCDB();
            }

        }

        public string GetMetaDataIdentityNoSub(int docId, string docType)
        {
            try
            {
                string fieldName = EnumManager.GetMetadataIdentityNoSub(docType);
                string fieldValue = Adapter.GetFieldValueByDocIdAndFieldName(fieldName, docId);

                if (string.IsNullOrEmpty(fieldValue))
                    return string.Empty;
                else
                    return fieldValue;
            }
            catch
            {
                return string.Empty;
            }
        }

        //GetMetaDataCertNumber
        public string GetMetaDataCertNumber(int docId, string docType)
        {
            try
            {
                string fieldName = EnumManager.GetMetadataCertNo(docType);
                string fieldValue = Adapter.GetFieldValueByDocIdAndFieldName(fieldName, docId);
                if (string.IsNullOrEmpty(fieldValue))
                    return string.Empty;
                else
                    return (fieldValue);
            }
            catch
            {
                return string.Empty;
            }
        }


        public bool IsForeign(int docId, string fieldName) //Not Used (Andrew)
        {
            //Search by docId and FieldName and FieldValue (foreign)

            try
            {
                int? count = Adapter.GetCountByDocIdAndFieldNameAndLikeFieldValue("foreign", fieldName.Trim(), docId);
                if (count != null && count.Value > 0)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsMuslim(int docId, string fieldName) //Not Used (Andrew)
        {
            //Search by docId and FieldName and FieldValue (foreign)
            try
            {
                int? count = Adapter.GetCountByDocIdAndFieldNameAndLikeFieldValue("muslim", fieldName.Trim(), docId);
                if (count != null && count.Value > 0)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public string LocalForeignMarriageType(int docId, string fieldName)
        {
            try
            {
                string value = Adapter.GetFieldValueByDocIdAndFieldName(fieldName.Trim(), docId);
                if (!String.IsNullOrEmpty(value))
                    return value.Substring(0, 1).ToUpper(); // send first letter
                else
                    return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }


        public string MarriageType(int docId, string fieldName)
        {
            try
            {
                string value = Adapter.GetFieldValueByDocIdAndFieldName(fieldName.Trim(), docId);
                if (!String.IsNullOrEmpty(value))
                    return TranslateMarriageType(value).ToUpper(); // send first letter
                else
                    return " ";
            }
            catch
            {
                return " ";
            }
        }


        private string TranslateMarriageType(string value)
        {
            //string s = string.Empty;

            switch (value.ToUpper().Trim())
            {
                case "LOCAL_CIVIL":
                    return "C";
                //case "LOCALCIVIL":
                //    return "C";
                case "LOCAL_MUSLIM":
                    return "M";
                //case "LOCALMUSLIM":
                //    return "M";
                default:
                    return " ";
            }
        }

        #endregion

        #region Insert Methods
        public int Insert(int docId, string fieldName, string fieldValue, bool verMandatory, bool comMandatory,
            bool verVisible, bool comVisible, bool isFixed, int maximumLength, bool isStamp)
        {
            MetaData.MetaDataDataTable dt = new MetaData.MetaDataDataTable();
            MetaData.MetaDataRow r = dt.NewMetaDataRow();

            r.Doc = docId;
            r.FieldName = fieldName.Trim();

            if (String.IsNullOrEmpty(fieldValue))
                fieldValue = " ";

            r.FieldValue = fieldValue.Trim();
            r.VerificationMandatory = verMandatory;
            r.CompletenessMandatory = comMandatory;
            r.VerificationVisible = verVisible;
            r.CompletenessVisible = comVisible;
            r.Fixed = isFixed;
            r.MaximumLength = maximumLength;
            r.isOldData = false;
            r.isStamp = isStamp;
            r.CreatedDate = DateTime.Now;
            r.ModifiedDate = DateTime.Now;

            dt.AddMetaDataRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }
        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        #endregion
    }
}
