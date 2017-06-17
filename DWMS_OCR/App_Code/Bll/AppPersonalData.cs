using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class AppPersonalData
    {
        #region Object Properties
        private string nric = string.Empty;
        private string name = string.Empty;
        private string dateJoinedService = string.Empty;
        private string companyName = string.Empty;
        private string personalType = string.Empty;
        private string employmentType = string.Empty;
        private string relationship = string.Empty;
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

        public AppPersonalData(HleInterface.HleInterfaceRow hleInterface)
        {
            this.Nric = hleInterface.Nric;
            this.Name = hleInterface.Name;
            this.PersonalType = hleInterface.ApplicantType;
            this.DateJoinedService = hleInterface.DateJoined;
            this.CompanyName = hleInterface.EmployerName;
            this.EmploymentType = hleInterface.EmploymentType;
            this.Month1Name = hleInterface.Inc1Date;
            this.Month1Value = hleInterface.Inc1;
            this.Month2Name = hleInterface.Inc2Date;
            this.Month2Value = hleInterface.Inc2;
            this.Month3Name = hleInterface.Inc3Date;
            this.Month3Value = hleInterface.Inc3;
            this.Month4Name = hleInterface.Inc4Date;
            this.Month4Value = hleInterface.Inc4;
            this.Month5Name = hleInterface.Inc5Date;
            this.Month5Value = hleInterface.Inc5;
            this.Month6Name = hleInterface.Inc6Date;
            this.Month6Value = hleInterface.Inc6;
            this.Month7Name = hleInterface.Inc7Date;
            this.Month7Value = hleInterface.Inc7;
            this.Month8Name = hleInterface.Inc8Date;
            this.Month8Value = hleInterface.Inc8;
            this.Month9Name = hleInterface.Inc9Date;
            this.Month9Value = hleInterface.Inc9;
            this.Month10Name = hleInterface.Inc10Date;
            this.Month10Value = hleInterface.Inc10;
            this.Month11Name = hleInterface.Inc11Date;
            this.Month11Value = hleInterface.Inc11;
            this.Month12Name = hleInterface.Inc12Date;
            this.Month12Value = hleInterface.Inc12;
        }

        #endregion
    }
}
