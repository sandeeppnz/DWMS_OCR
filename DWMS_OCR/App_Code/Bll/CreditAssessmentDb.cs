using System;
using System.Collections.Generic;
using System.Web;
using DWMS_OCR.App_Code.Dal.CreditAssessmentTableAdapters;
using System.Data;
using DWMS_OCR.App_Code.Dal;


namespace DWMS_OCR.App_Code.Bll
{
    /// <summary>
    /// Summary description for CreditAssessmentDb
    /// </summary>
    public class CreditAssessmentDb
    {
        private CreditAssessmentTableAdapter _Adapter = null;

        protected CreditAssessmentTableAdapter Adapter
        {
            get
            {
                if (_Adapter == null)
                    _Adapter = new CreditAssessmentTableAdapter();

                return _Adapter;
            }
        }


        public CreditAssessment.CreditAssessmentDataTable GetCAByAppPersonalIdByIncomeItemType(int appPersonalId, string component, string type)
        {
            return Adapter.GetCAByAppPersonalIdByIncomeItemType(appPersonalId,component,type);
        }

        public CreditAssessment.CreditAssessmentDataTable GetCreditAssessmentById(int Id)
        {
            return Adapter.GetCreditAssessmentById(Id);
        }

        public int Insert(int AppPersonalId, string IncomeItem, string IncomeType, decimal Amount, Guid? EnteredBy)
        {
            CreditAssessment.CreditAssessmentDataTable dt = new CreditAssessment.CreditAssessmentDataTable();
            CreditAssessment.CreditAssessmentRow row = dt.NewCreditAssessmentRow();

            row.AppPersonalId = AppPersonalId;
            row.IncomeItem = IncomeItem;
            row.IncomeType = IncomeType;
            row.CreditAssessmentAmount = Amount;
            row.EnteredBy = EnteredBy.Value;
            row.DateEntered = DateTime.Now;

            dt.AddCreditAssessmentRow(row);

            Adapter.Update(dt);

            int id = row.Id;

            if (id > 0)
            {
                AuditTrailDb auditTrailDb = new AuditTrailDb();
                auditTrailDb.Record(TableNameEnum.CreditAssessment, id.ToString(), OperationTypeEnum.Insert);
            }
            return id;
        }


        public bool Update(int id, decimal Amount, Guid? EnteredBy)
        {
            CreditAssessment.CreditAssessmentDataTable dt = Adapter.GetCreditAssessmentById(id);

            if (dt.Count == 0)
                return false;

            CreditAssessment.CreditAssessmentRow row = dt[0];

            row.CreditAssessmentAmount = Amount;
            row.EnteredBy = EnteredBy.Value;
            row.DateEntered = DateTime.Now;

            int rowsAffected = Adapter.Update(dt);

            if (rowsAffected > 0)
            {
                AuditTrailDb auditTrailDb = new AuditTrailDb();
                auditTrailDb.Record(TableNameEnum.CreditAssessment, id.ToString(), OperationTypeEnum.Update);
            }
            return rowsAffected == 1;
        }
    }
}