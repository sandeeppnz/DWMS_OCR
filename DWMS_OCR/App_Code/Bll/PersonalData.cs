using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class PersonalData
    {
        #region Object Properties
        private string nric = string.Empty;
        private string name = string.Empty;
        private string dateJoinedService = string.Empty;
        private string companyName = string.Empty;
        private string personalType = string.Empty;
        private string employmentType = string.Empty;
        private string relationship = string.Empty;

        // Salary Info
        private string month1Name = string.Empty;
        private string month2Name = string.Empty;
        private string month3Name = string.Empty;
        private string month4Name = string.Empty;
        private string month5Name = string.Empty;
        private string month6Name = string.Empty;
        private string month7Name = string.Empty;
        private string month8Name = string.Empty;
        private string month9Name = string.Empty;
        private string month10Name = string.Empty;
        private string month11Name = string.Empty;
        private string month12Name = string.Empty;
        private string month1Value = string.Empty;
        private string month2Value = string.Empty;
        private string month3Value = string.Empty;
        private string month4Value = string.Empty;
        private string month5Value = string.Empty;
        private string month6Value = string.Empty;
        private string month7Value = string.Empty;
        private string month8Value = string.Empty;
        private string month9Value = string.Empty;
        private string month10Value = string.Empty;
        private string month11Value = string.Empty;
        private string month12Value = string.Empty;

        private bool hasSalary = false;

        private int orderNo = 0;

        private int noOfIncomeMonths;   //Added By Edward 06.01.2014 

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

        public string DateJoinedService
        {
            get { return dateJoinedService; }
            set { dateJoinedService = value; }
        }

        public string CompanyName
        {
            get { return companyName; }
            set { companyName = value; }
        }

        public string PersonalType
        {
            get { return personalType; }
            set { personalType = value; }
        }

        public string EmploymentType
        {
            get { return employmentType; }
            set { employmentType = value; }
        }

        public string Relationship
        {
            get { return relationship; }
            set { relationship = value; }
        }

        public string Month1Name
        {
            get { return month1Name; }
            set { month1Name = value; }
        }

        public string Month2Name
        {
            get { return month2Name; }
            set { month2Name = value; }
        }

        public string Month3Name
        {
            get { return month3Name; }
            set { month3Name = value; }
        }

        public string Month4Name
        {
            get { return month4Name; }
            set { month4Name = value; }
        }

        public string Month5Name
        {
            get { return month5Name; }
            set { month5Name = value; }
        }

        public string Month6Name
        {
            get { return month6Name; }
            set { month6Name = value; }
        }

        public string Month7Name
        {
            get { return month7Name; }
            set { month7Name = value; }
        }

        public string Month8Name
        {
            get { return month8Name; }
            set { month8Name = value; }
        }

        public string Month9Name
        {
            get { return month9Name; }
            set { month9Name = value; }
        }

        public string Month10Name
        {
            get { return month10Name; }
            set { month10Name = value; }
        }

        public string Month11Name
        {
            get { return month11Name; }
            set { month11Name = value; }
        }

        public string Month12Name
        {
            get { return month12Name; }
            set { month12Name = value; }
        }

        public string Month1Value
        {
            get { return month1Value; }
            set { month1Value = value; }
        }

        public string Month2Value
        {
            get { return month2Value; }
            set { month2Value = value; }
        }

        public string Month3Value
        {
            get { return month3Value; }
            set { month3Value = value; }
        }

        public string Month4Value
        {
            get { return month4Value; }
            set { month4Value = value; }
        }

        public string Month5Value
        {
            get { return month5Value; }
            set { month5Value = value; }
        }

        public string Month6Value
        {
            get { return month6Value; }
            set { month6Value = value; }
        }

        public string Month7Value
        {
            get { return month7Value; }
            set { month7Value = value; }
        }

        public string Month8Value
        {
            get { return month8Value; }
            set { month8Value = value; }
        }

        public string Month9Value
        {
            get { return month9Value; }
            set { month9Value = value; }
        }

        public string Month10Value
        {
            get { return month10Value; }
            set { month10Value = value; }
        }

        public string Month11Value
        {
            get { return month11Value; }
            set { month11Value = value; }
        }

        public string Month12Value
        {
            get { return month12Value; }
            set { month12Value = value; }
        }

        public bool HasSalary
        {
            get { return hasSalary; }
            set { hasSalary = value; }
        }

        public int OrderNo
        {
            get { return orderNo; }
            set { orderNo = value; }
        }

        public int NoOfIncomeMonths
        {
            get { return noOfIncomeMonths; }
            set { noOfIncomeMonths = value; }
        }

        public PersonalData()
        {
        }

        public PersonalData(HleInterface.HleInterfaceRow hleInterface)
        {
            this.Nric = (hleInterface.IsNricNull() ? string.Empty : hleInterface.Nric);
            this.Name = (hleInterface.IsNameNull() ? string.Empty : hleInterface.Name);
            this.PersonalType = (hleInterface.IsApplicantTypeNull() ? string.Empty : hleInterface.ApplicantType);
            this.DateJoinedService = (hleInterface.IsDateJoinedNull() ? string.Empty : hleInterface.DateJoined);
            this.CompanyName = (hleInterface.IsEmployerNameNull() ? string.Empty : hleInterface.EmployerName);
            this.EmploymentType = (hleInterface.IsEmploymentTypeNull() ? string.Empty : hleInterface.EmploymentType);
            this.Relationship = (hleInterface.IsRelationshipNull() ? string.Empty : hleInterface.Relationship);
            this.Month1Name = (hleInterface.IsInc1DateNull() ? string.Empty : hleInterface.Inc1Date);
            this.Month1Value = (hleInterface.IsInc1Null() ? string.Empty : hleInterface.Inc1);
            this.Month2Name = (hleInterface.IsInc2DateNull() ? string.Empty : hleInterface.Inc2Date);
            this.Month2Value = (hleInterface.IsInc2Null() ? string.Empty : hleInterface.Inc2);
            this.Month3Name = (hleInterface.IsInc3DateNull() ? string.Empty : hleInterface.Inc3Date);
            this.Month3Value = (hleInterface.IsInc3Null() ? string.Empty : hleInterface.Inc3);
            this.Month4Name = (hleInterface.IsInc4DateNull() ? string.Empty : hleInterface.Inc4Date);
            this.Month4Value = (hleInterface.IsInc4Null() ? string.Empty : hleInterface.Inc4);
            this.Month5Name = (hleInterface.IsInc5DateNull() ? string.Empty : hleInterface.Inc5Date);
            this.Month5Value = (hleInterface.IsInc5Null() ? string.Empty : hleInterface.Inc5);
            this.Month6Name = (hleInterface.IsInc6DateNull() ? string.Empty : hleInterface.Inc6Date);
            this.Month6Value = (hleInterface.IsInc6Null() ? string.Empty : hleInterface.Inc6);
            this.Month7Name = (hleInterface.IsInc7DateNull() ? string.Empty : hleInterface.Inc7Date);
            this.Month7Value = (hleInterface.IsInc7Null() ? string.Empty : hleInterface.Inc7);
            this.Month8Name = (hleInterface.IsInc8DateNull() ? string.Empty : hleInterface.Inc8Date);
            this.Month8Value = (hleInterface.IsInc8Null() ? string.Empty : hleInterface.Inc8);
            this.Month9Name = (hleInterface.IsInc9DateNull() ? string.Empty : hleInterface.Inc9Date);
            this.Month9Value = (hleInterface.IsInc9Null() ? string.Empty : hleInterface.Inc9);
            this.Month10Name = (hleInterface.IsInc10DateNull() ? string.Empty : hleInterface.Inc10Date);
            this.Month10Value = (hleInterface.IsInc10Null() ? string.Empty : hleInterface.Inc10);
            this.Month11Name = (hleInterface.IsInc11DateNull() ? string.Empty : hleInterface.Inc11Date);
            this.Month11Value = (hleInterface.IsInc11Null() ? string.Empty : hleInterface.Inc11);
            this.Month12Name = (hleInterface.IsInc12DateNull() ? string.Empty : hleInterface.Inc12Date);
            this.Month12Value = (hleInterface.IsInc12Null() ? string.Empty : hleInterface.Inc12);
            this.HasSalary = true;
            this.OrderNo = (hleInterface.IsOrderNoNull() ? 0 : hleInterface.OrderNo);
            this.NoOfIncomeMonths = (hleInterface.IsNoOfIncomeMonthsNull() ? 0 : hleInterface.NoOfIncomeMonths);
        }

        public PersonalData(AppPersonal.AppPersonalRow appPersonal)
        {
            AppPersonalSalaryDb personalSalaryDb = new AppPersonalSalaryDb();
            AppPersonalSalary.AppPersonalSalaryDataTable personalSalaryDt = personalSalaryDb.GetAppPersonalSalaryByAppPersonalId(appPersonal.Id);

            this.Nric = (appPersonal.IsNricNull() ? string.Empty : appPersonal.Nric);
            this.Name = (appPersonal.IsNameNull() ? string.Empty : appPersonal.Name);
            this.PersonalType = appPersonal.PersonalType;
            this.DateJoinedService = (appPersonal.IsDateJoinedServiceNull() ? string.Empty : appPersonal.DateJoinedService);
            this.CompanyName = (appPersonal.IsCompanyNameNull() ? string.Empty : appPersonal.CompanyName);
            this.EmploymentType = (appPersonal.IsEmploymentTypeNull() ? string.Empty : appPersonal.EmploymentType);
            this.Relationship = (appPersonal.IsRelationshipNull() ? string.Empty : appPersonal.Relationship);

            bool hasSalaryTemp = false;
            if (personalSalaryDt.Rows.Count > 0)
            {
                AppPersonalSalary.AppPersonalSalaryRow personalSalaryDr = personalSalaryDt[0];

                this.Month1Name = (personalSalaryDr.IsMonth1NameNull() ? string.Empty : personalSalaryDr.Month1Name);
                this.Month1Value = (personalSalaryDr.IsMonth1ValueNull() ? string.Empty : personalSalaryDr.Month1Value);
                this.Month2Name = (personalSalaryDr.IsMonth2NameNull() ? string.Empty : personalSalaryDr.Month2Name);
                this.Month2Value = (personalSalaryDr.IsMonth2ValueNull() ? string.Empty : personalSalaryDr.Month2Value);
                this.Month3Name = (personalSalaryDr.IsMonth3NameNull() ? string.Empty : personalSalaryDr.Month3Name);
                this.Month3Value = (personalSalaryDr.IsMonth3ValueNull() ? string.Empty : personalSalaryDr.Month3Value);
                this.Month4Name = (personalSalaryDr.IsMonth4NameNull() ? string.Empty : personalSalaryDr.Month4Name);
                this.Month4Value = (personalSalaryDr.IsMonth4ValueNull() ? string.Empty : personalSalaryDr.Month4Value);
                this.Month5Name = (personalSalaryDr.IsMonth5NameNull() ? string.Empty : personalSalaryDr.Month5Name);
                this.Month5Value = (personalSalaryDr.IsMonth5ValueNull() ? string.Empty : personalSalaryDr.Month5Value);
                this.Month6Name = (personalSalaryDr.IsMonth6NameNull() ? string.Empty : personalSalaryDr.Month6Name);
                this.Month6Value = (personalSalaryDr.IsMonth6ValueNull() ? string.Empty : personalSalaryDr.Month6Value);
                this.Month7Name = (personalSalaryDr.IsMonth7NameNull() ? string.Empty : personalSalaryDr.Month7Name);
                this.Month7Value = (personalSalaryDr.IsMonth7ValueNull() ? string.Empty : personalSalaryDr.Month7Value);
                this.Month8Name = (personalSalaryDr.IsMonth8NameNull() ? string.Empty : personalSalaryDr.Month8Name);
                this.Month8Value = (personalSalaryDr.IsMonth8ValueNull() ? string.Empty : personalSalaryDr.Month8Value);
                this.Month9Name = (personalSalaryDr.IsMonth9NameNull() ? string.Empty : personalSalaryDr.Month9Name);
                this.Month9Value = (personalSalaryDr.IsMonth9ValueNull() ? string.Empty : personalSalaryDr.Month9Value);
                this.Month10Name = (personalSalaryDr.IsMonth10NameNull() ? string.Empty : personalSalaryDr.Month10Name);
                this.Month10Value = (personalSalaryDr.IsMonth10ValueNull() ? string.Empty : personalSalaryDr.Month10Value);
                this.Month11Name = (personalSalaryDr.IsMonth11NameNull() ? string.Empty : personalSalaryDr.Month11Name);
                this.Month11Value = (personalSalaryDr.IsMonth11ValueNull() ? string.Empty : personalSalaryDr.Month11Value);
                this.Month12Name = (personalSalaryDr.IsMonth12NameNull() ? string.Empty : personalSalaryDr.Month12Name);
                this.Month12Value = (personalSalaryDr.IsMonth12ValueNull() ? string.Empty : personalSalaryDr.Month12Value);
                hasSalaryTemp = true;
            }

            this.hasSalary = hasSalaryTemp;
            this.orderNo = (appPersonal.IsOrderNoNull() ? 0 : appPersonal.OrderNo);
        }

        public PersonalData(ResaleInterface.ResaleInterfaceRow resaleInterface)
        {
            this.Nric = resaleInterface.Nric;
            this.Name = resaleInterface.Name;
            this.PersonalType = (resaleInterface.IsApplicantTypeNull() ? string.Empty : resaleInterface.ApplicantType);
            this.DateJoinedService = string.Empty;
            this.CompanyName = string.Empty;
            this.EmploymentType = (resaleInterface.IsEmploymentTypeNull() ? string.Empty : resaleInterface.EmploymentType); ;
            this.Relationship = (resaleInterface.IsRelationshipNull() ? string.Empty : resaleInterface.Relationship);
            this.Month1Name = string.Empty;
            this.Month1Value = string.Empty;
            this.Month2Name = string.Empty;
            this.Month2Value = string.Empty;
            this.Month3Name = string.Empty;
            this.Month3Value = string.Empty;
            this.Month4Name = string.Empty;
            this.Month4Value = string.Empty;
            this.Month5Name = string.Empty;
            this.Month5Value = string.Empty;
            this.Month6Name = string.Empty;
            this.Month6Value = string.Empty;
            this.Month7Name = string.Empty;
            this.Month7Value = string.Empty;
            this.Month8Name = string.Empty;
            this.Month8Value = string.Empty;
            this.Month9Name = string.Empty;
            this.Month9Value = string.Empty;
            this.Month10Name = string.Empty;
            this.Month10Value = string.Empty;
            this.Month11Name = string.Empty;
            this.Month11Value = string.Empty;
            this.Month12Name = string.Empty;
            this.Month12Value = string.Empty;
            this.HasSalary = false;
            this.OrderNo = (resaleInterface.IsOrderNoNull() ? 0 : resaleInterface.OrderNo);
        }

        public PersonalData(SersInterface.SersInterfaceRow sersInterface)
        {
            this.Nric = sersInterface.Nric;
            this.Name = sersInterface.Name;
            this.PersonalType = sersInterface.ApplicantType;
            this.DateJoinedService = string.Empty;
            this.CompanyName = string.Empty;
            this.EmploymentType = string.Empty;
            this.Relationship = (sersInterface.IsRelationshipNull() ? string.Empty : sersInterface.Relationship);
            this.Month1Name = string.Empty;
            this.Month1Value = string.Empty;
            this.Month2Name = string.Empty;
            this.Month2Value = string.Empty;
            this.Month3Name = string.Empty;
            this.Month3Value = string.Empty;
            this.Month4Name = string.Empty;
            this.Month4Value = string.Empty;
            this.Month5Name = string.Empty;
            this.Month5Value = string.Empty;
            this.Month6Name = string.Empty;
            this.Month6Value = string.Empty;
            this.Month7Name = string.Empty;
            this.Month7Value = string.Empty;
            this.Month8Name = string.Empty;
            this.Month8Value = string.Empty;
            this.Month9Name = string.Empty;
            this.Month9Value = string.Empty;
            this.Month10Name = string.Empty;
            this.Month10Value = string.Empty;
            this.Month11Name = string.Empty;
            this.Month11Value = string.Empty;
            this.Month12Name = string.Empty;
            this.Month12Value = string.Empty;
            this.HasSalary = false;
            this.OrderNo = (sersInterface.IsOrderNoNull() ? 0 : sersInterface.OrderNo);
        }

        public PersonalData(SalesInterface.SalesInterfaceRow salesInterface)
        {
            this.Nric = salesInterface.Nric;
            this.Name = salesInterface.Name;
            this.PersonalType = salesInterface.ApplicantType;
            this.DateJoinedService = string.Empty;
            this.CompanyName = string.Empty;
            this.EmploymentType = (salesInterface.IsEmploymentTypeNull() ? string.Empty : salesInterface.EmploymentType);
            this.Relationship = (salesInterface.IsRelationshipNull() ? string.Empty : salesInterface.Relationship);
            this.Month1Name = string.Empty;
            this.Month1Value = string.Empty;
            this.Month2Name = string.Empty;
            this.Month2Value = string.Empty;
            this.Month3Name = string.Empty;
            this.Month3Value = string.Empty;
            this.Month4Name = string.Empty;
            this.Month4Value = string.Empty;
            this.Month5Name = string.Empty;
            this.Month5Value = string.Empty;
            this.Month6Name = string.Empty;
            this.Month6Value = string.Empty;
            this.Month7Name = string.Empty;
            this.Month7Value = string.Empty;
            this.Month8Name = string.Empty;
            this.Month8Value = string.Empty;
            this.Month9Name = string.Empty;
            this.Month9Value = string.Empty;
            this.Month10Name = string.Empty;
            this.Month10Value = string.Empty;
            this.Month11Name = string.Empty;
            this.Month11Value = string.Empty;
            this.Month12Name = string.Empty;
            this.Month12Value = string.Empty;
            this.HasSalary = false;
            this.OrderNo = (salesInterface.IsOrderNoNull() ? 0 : salesInterface.OrderNo);
            #region Added By Edward 12/3/2014 Sales and Resale Changes 
            this.NoOfIncomeMonths = (salesInterface.IsNoOfIncomeMonthsNull() ? 0 : salesInterface.NoOfIncomeMonths);
            #endregion
        }

        #endregion
    }
}
