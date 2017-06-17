using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using DWMS_OCR.App_Code.Dal;
using DWMS_OCR.App_Code.Helper;
using System.Globalization;
using System.Diagnostics;

namespace DWMS_OCR.App_Code.Bll
{
    class MetaDataHardCoded
    {
        private int docId = -1;
        private string docType = string.Empty;
        private string ocrText = string.Empty;
        private ArrayList personalData;
        private ArrayList metaData = new ArrayList();

        public ArrayList MetaData
        {
            get { return metaData; }
            set { metaData = value; }
        }

        public MetaDataHardCoded(int docId, string docType, string ocrText, ArrayList personalData)
        {
            this.docId = docId;
            this.docType = docType;
            this.ocrText = ocrText;
            this.personalData = personalData;

            SetHardCodedMeta();
        }

        public MetaDataHardCoded(int docId, string docType)
        {
            this.docId = docId;
            this.docType = docType;
        }

        /// <summary>
        /// Set the hardcoded meta data
        /// </summary>
        private void SetHardCodedMeta()
        {
            if (!String.IsNullOrEmpty(docType))
            {
                if (docType.Equals("BirthCertificate"))
                    CreateMetaDataForBirthCertificates();
                else if (docType.Equals("CBR"))
                    CreateMetaDataForCbr();
                else if (docType.Equals("CPFContribution"))
                    CreateMetaDataForCpf();
                else if (docType.Equals("CPFStatement"))
                    CreateMetaDataForCpfStatement();
                else if (docType.Equals("CPFStatementRefund"))
                    CreateMetaDataForCpfStatementRefund();
                else if (docType.Equals("DeathCertificate"))
                    CreateMetaDataForDeathCertificate();
                else if (docType.Equals("IRASAssesement") || docType.Equals("IRASIR8E"))
                    CreateMetaDataForIraAssessment();
                else if (docType.Equals("MarriageCertificate"))
                    CreateMetaDataForMarriageCertificate();
                else if (docType.Equals("DivorceCertificate"))
                    CreateMetaDataForDivorceCertificate();
                else if (docType.Equals("PAYSLIP"))
                    CreateMetaDataForPayslip();
                else if (docType.Equals("CommissionStatement") || docType.Equals("OverseasIncome"))
                    CreateMetaDataForCommissionStatement();
                else if (docType.Equals("EmploymentLetter"))
                    CreateMetaDataForEmploymentLetter();
            }
        }

        #region Hard coded meta data
        /// <summary>
        /// Birth Certificate hardcoded meta data
        /// </summary>
        private void CreateMetaDataForBirthCertificates()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            // Create metadata for identity no
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = "Identity No";
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = false;
            metaDataOcr.CompletenessMandatory = false;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// CBR hardcoded meta data
        /// </summary>
        private void CreateMetaDataForCbr()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;
            string enquiryDate = " ";

            GetDatesForCbr(ocrText, out enquiryDate);

            // Create metadata for the date of report
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCBREnum.DateOfReport.ToString();
            metaDataOcr.FieldValue = enquiryDate;
            metaDataOcr.VerificationMandatory = false;
            metaDataOcr.CompletenessMandatory = false;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// CPF Contribution History hardcoded meta data
        /// </summary>
        private void CreateMetaDataForCpf()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            string startDate = " ";
            string endDate = " ";

            GetDatesForCpfContributionHistory(ocrText, out startDate, out endDate);

            // Create metadata for both start and end date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCPFContributionEnum.StartDate.ToString();
            metaDataOcr.FieldValue = startDate;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCPFContributionEnum.EndDate.ToString();
            metaDataOcr.FieldValue = endDate;
            metaDataOcr.VerificationMandatory = false;
            metaDataOcr.CompletenessMandatory = false;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create the consistent contribution meta data
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCPFContributionEnum.ConsistentContribution.ToString();
            metaDataOcr.FieldValue = DocTypeMetaDataValueCPFEnum.No.ToString();
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            string companyName = " ";
            companyName = GetCompanyName(personalData);

            // Create the Company 1 meta data
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCPFContributionEnum.CompanyName1.ToString();
            metaDataOcr.FieldValue = companyName;
            metaDataOcr.VerificationMandatory = false;
            metaDataOcr.CompletenessMandatory = false;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create the Company 2 meta data
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCPFContributionEnum.CompanyName2.ToString();
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = false;
            metaDataOcr.CompletenessMandatory = false;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// CPF Statements hardcoded meta data
        /// </summary>
        private void CreateMetaDataForCpfStatement()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            string startDate = " ";
            string endDate = " ";

            GetDatesForCpfStatement(ocrText, out startDate, out endDate);

            // Create metadata for both start and end date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCPFStatementEnum.StartDate.ToString();
            metaDataOcr.FieldValue = startDate;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for end adate
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCPFStatementEnum.EndDate.ToString();
            metaDataOcr.FieldValue = endDate;
            metaDataOcr.VerificationMandatory = false;
            metaDataOcr.CompletenessMandatory = false;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// CPF Statement Refund
        /// </summary>
        private void CreateMetaDataForCpfStatementRefund()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            string startDate = " ";

            GetDatesForCpfRefund(ocrText, out startDate);

            // Create metadata for both start and end date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataCPFStatementRefundEnum.DateOfStatement.ToString();
            metaDataOcr.FieldValue = startDate;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// Death Certificate (Foreign) hardcoded meta data
        /// </summary>
        private void CreateMetaDataForDeathCertificate()
        {
            ArrayList metaList = new ArrayList(); // Meta data container
            MetaDataOcr metaDataOcr = null;

            string nric = " ";
            string date = " ";
            string[] lines = ocrText.Split(Constants.NewLineSeperators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in words)
                {
                    if (Validation.IsNric(word) && !String.IsNullOrEmpty(nric.Trim()))
                        nric = word.ToUpper();

                    if (Validation.IsDate(word) && !String.IsNullOrEmpty(date.Trim()))
                        date = word;
                }

                if (!String.IsNullOrEmpty(nric.Trim()) && !String.IsNullOrEmpty(date.Trim()))
                    break;
            }

            // Create metadata for DateOfDeath
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataDeathCertificateEnum.DateOfDeath.ToString();
            metaDataOcr.FieldValue = date;
            metaDataOcr.VerificationMandatory = false;
            metaDataOcr.CompletenessMandatory = false;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);
        }

        /// <summary>
        /// IRAS hardcoded meta data
        /// </summary>
        private void CreateMetaDataForIraAssessment()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            // Get the year of assessment
            string yearOfAssessment = " ";
            string[] lines = ocrText.Split(Constants.NewLineSeperators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (line.Contains("year of assessment") || (line.Contains("year") && line.Contains("assessment")))
                {
                    if (line.IndexOf("assessment") + 10 < line.Length)
                    {
                        string temp = line.Substring(line.IndexOf("assessment") + 10);
                        temp = CategorizationHelpers.RemoveNonAlphanumericCharacters(temp);
                        temp = CategorizationHelpers.RemoveAlphaCharacters(temp);
                        yearOfAssessment = temp;
                    }
                }
            }

            // Create metadata for year of assessment
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataIRASAssesementEnum.YearOfAssessment.ToString();
            metaDataOcr.FieldValue = yearOfAssessment;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for date of filling
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataIRASAssesementEnum.DateOfFiling.ToString();
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for type of income
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataIRASAssesementEnum.TypeOfIncome.ToString();
            metaDataOcr.FieldValue = DocTypeMetaDataValueIRASAssesementEnum.Employment.ToString();
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// Marriage Certificate hardcoded meta data
        /// </summary>
        private void CreateMetaDataForMarriageCertificate()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            string[] lines = ocrText.Split(Constants.NewLineSeperators, StringSplitOptions.RemoveEmptyEntries);
            string[] nricList = new string[2];

            for (int cnt = 0; cnt < lines.Length; cnt++)
            {
                string line = lines[cnt];
                string[] currLineWordArray = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int cnt2 = 0, nricCnt = 0; cnt < currLineWordArray.Length; cnt++)
                {
                    string word = currLineWordArray[cnt2];

                    if (CategorizationHelpers.IsNric(word))
                    {
                        nricList[nricCnt] = word;
                        nricCnt++;

                        if (nricCnt == 2)
                            break;
                    }
                }
            }

            // Create metadata for entry no
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataMarriageCertificateEnum.MarriageCertNo.ToString();
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for husband identity no
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataMarriageCertificateEnum.IdentityNoRequestor.ToString();

            try
            {
                metaDataOcr.FieldValue = nricList[0];
            }
            catch (Exception)
            {
                metaDataOcr.FieldValue = " ";
            }

            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for wife identity no
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataMarriageCertificateEnum.IdentityNoSpouse.ToString();

            try
            {
                metaDataOcr.FieldValue = nricList[1];
            }
            catch (Exception)
            {
                metaDataOcr.FieldValue = " ";
            }

            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// Divorce Certificate hardcoded meta data
        /// </summary>
        private void CreateMetaDataForDivorceCertificate()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            string[] lines = ocrText.Split(Constants.NewLineSeperators, StringSplitOptions.RemoveEmptyEntries);
            string[] nricList = new string[2];

            for (int cnt = 0; cnt < lines.Length; cnt++)
            {
                string[] currLineWordArray = lines[cnt].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int cnt2 = 0, nricCnt = 0; cnt < currLineWordArray.Length; cnt++)
                {
                    string word = currLineWordArray[cnt2];

                    if (CategorizationHelpers.IsNric(word))
                    {
                        nricList[nricCnt] = word;
                        nricCnt++;

                        if (nricCnt == 2)
                            break;
                    }
                }
            }

            // Create metadata for identity no
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataDivorceCertificateEnum.Tag.ToString();
            metaDataOcr.FieldValue = "Local";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for entry no
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataDivorceCertificateEnum.DivorceCaseNo.ToString();
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for husband identity no
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataDivorceCertificateEnum.IdentityNoRequestor.ToString();

            try
            {
                metaDataOcr.FieldValue = nricList[0];
            }
            catch (Exception)
            {
                metaDataOcr.FieldValue = " ";
            }

            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for wife identity no
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataDivorceCertificateEnum.IdentityNoSpouse.ToString();

            try
            {
                metaDataOcr.FieldValue = nricList[1];
            }
            catch (Exception)
            {
                metaDataOcr.FieldValue = " ";
            }

            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// Payslip hard-coded meta data
        /// </summary>
        private void CreateMetaDataForPayslip()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            string startDate = " ";
            string endDate = " ";
            string amount = " ";

            // Get data for payslip
            GetDataForPayslip(personalData, out startDate, out endDate, out amount);

            // Create start date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.StartDate.ToString();
            metaDataOcr.FieldValue = startDate;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create end date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.EndDate.ToString();
            metaDataOcr.FieldValue = endDate;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            string company = " ";
            company = GetCompanyName(personalData);

            // Create metadata company
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.NameOfCompany.ToString();
            metaDataOcr.FieldValue = company;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for allce
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.Allowance.ToString();
            metaDataOcr.FieldValue = "No";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        /// <summary>
        /// Commission Statement hard-coded meta data
        /// </summary>
        private void CreateMetaDataForCommissionStatement()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            // Create start date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.StartDate.ToString();
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create end date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.EndDate.ToString();
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            string companyName = " ";
            companyName = GetCompanyName(personalData);

            // Create metadata company
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.NameOfCompany.ToString();
            metaDataOcr.FieldValue = companyName;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }

        private void CreateMetaDataForEmploymentLetter()
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            // Create start date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.StartDate.ToString();
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create end date
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.EndDate.ToString();
            metaDataOcr.FieldValue = " ";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            string companyName = " ";
            companyName = GetCompanyName(personalData);

            // Create metadata company
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.NameOfCompany.ToString();
            metaDataOcr.FieldValue = companyName;
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            // Create metadata for allce
            metaDataOcr = new MetaDataOcr();
            metaDataOcr.FieldName = DocTypeMetaDataPAYSLIPEnum.Allowance.ToString();
            metaDataOcr.FieldValue = "No";
            metaDataOcr.VerificationMandatory = true;
            metaDataOcr.CompletenessMandatory = true;
            metaDataOcr.VerificationVisible = true;
            metaDataOcr.CompletenessVisible = true;
            metaDataOcr.IsFixed = true;

            metaList.Add(metaDataOcr);

            MetaData = metaList;
        }
        #endregion

        #region Get Dates for CPF Docs
        private void GetDatesForCpfContributionHistory(string ocrText, out string startDate, out string endDate)
        {
            startDate = " ";
            endDate = " ";

            if (string.IsNullOrEmpty(ocrText))
            {
                return;
            }

            string keywordPageNumber = "Page 2";

            if (ocrText.ToLower().Contains(keywordPageNumber.ToLower()))
            {
                return;
            }

            string keywordFor = "For";
            string keywordTo = "to";
            string token = null;
            string[] lines = ocrText.Split(new[] { '\r', '\n' });
            bool allKeywordsFound = false;

            foreach (string line in lines)
            {
                string[] words = Util.SplitString(line, false, true);
                string keywordForTemp = null;
                string keywordToTemp = null;
                bool keywordForFound = false;
                bool keywordToFound = false;

                foreach (string word in words)
                {
                    if (!keywordForFound && LevenshteinDistance.IsMatch(word, keywordFor, 1))
                    {
                        keywordForTemp = word;
                        keywordForFound = true;
                    }

                    if (keywordForFound && !keywordToFound && word.Length >= keywordTo.Length &&
                        LevenshteinDistance.IsMatch(word.ToLower(), keywordTo.ToLower(), 1))
                    {
                        keywordToTemp = word;
                        keywordToFound = true;
                    }
                }

                if (keywordForFound && keywordToFound)
                {
                    keywordFor = keywordForTemp;
                    keywordTo = keywordToTemp;
                    token = line;
                    allKeywordsFound = true;
                    break;
                }
            }

            if (!allKeywordsFound)
            {
                return;
            }

            // Get start and end date
            string startMonth = null;
            string startYear = null;
            string endMonth = null;
            string endYear = null;

            string[] arr = Util.SplitString(token, false, true);

            for (int i = 0; i < arr.Length; i++)
            {
                string word = arr[i];

                if (word == keywordFor)
                {
                    if (i + 1 < arr.Length && arr[i + 1] != keywordTo)
                    {
                        startMonth = string.IsNullOrEmpty(startMonth) ? GetNearestMonth(arr[i + 1]) : startMonth;
                    }

                    if (i + 2 < arr.Length && arr[i + 2] != keywordTo)
                    {
                        startYear = string.IsNullOrEmpty(startYear) ? GetNearestYear(arr[i + 2]) : startYear;
                    }
                }
                else if (word == keywordTo)
                {
                    if (i + 1 < arr.Length)
                    {
                        endMonth = string.IsNullOrEmpty(endMonth) ? GetNearestMonth(arr[i + 1]) : endMonth;
                    }

                    if (i + 2 < arr.Length)
                    {
                        endYear = string.IsNullOrEmpty(endYear) ? GetNearestYear(arr[i + 2]) : endYear;
                    }
                }
            }

            startDate = startMonth + " " + startYear;
            startDate = startDate.Trim();
            endDate = endMonth + " " + endYear;
            endDate = endDate.Trim();

            startDate = GetCompleteDate(startDate);
            endDate = GetCompleteDate(endDate);
        }

        private void GetDatesForCpfStatement(string ocrText, out string startDate, out string endDate)
        {
            startDate = " ";
            endDate = " ";

            string[] keywords;

            // My Statement - Yearly Statement of Account 
            // For Jan 2011 to Dec 2011
            keywords = new string[] { "Yearly", "Statement" };

            if (MatchCpfStatementKeywords(keywords, ocrText, true, 3))
            {
                GetDatesForCpfStatement_YearlyStatementOfAccount(ocrText, out startDate, out endDate);
                return;
            }

            // My Statement - Transaction History 
            // For 01 Feb 2011 to 22 Apr 2012
            keywords = new string[] { "Transaction", "History" };

            if (MatchCpfStatementKeywords(keywords, ocrText, true, 3))
            {
                GetDatesForCpfStatement_TransactionHistory(ocrText, out startDate, out endDate);
                return;
            }

            // My Statement 
            // Account Balances (as at 21 Apr 2012) 
            keywords = new string[] { "Account", "Balances" };

            if (MatchCpfStatementKeywords(keywords, ocrText, true, 3))
            {
                GetDatesForCpfStatement_AccountBalances(ocrText, out startDate, out endDate);
                return;
            }
        }

        // My Statement - Yearly Statement of Account 
        // For Jan 2011 to Dec 2011
        private void GetDatesForCpfStatement_YearlyStatementOfAccount(string ocrText, out string startDate, out string endDate)
        {
            startDate = " ";
            endDate = " ";
            // The text format is the same as CPF Contribution History, 
            // so we simply call the GetDatesForCpfContributionHistory() function.
            GetDatesForCpfContributionHistory(ocrText, out startDate, out endDate);
        }

        // My Statement - Transaction History 
        // For 01 Feb 2011 to 22 Apr 2012
        private void GetDatesForCpfStatement_TransactionHistory(string ocrText, out string startDate, out string endDate)
        {
            startDate = " ";
            endDate = " ";

            string keywordFor = "For";
            string keywordTo = "to";
            string token = null;
            string[] lines = ocrText.Split(new[] { '\r', '\n' });
            bool allKeywordsFound = false;

            foreach (string line in lines)
            {
                string[] words = Util.SplitString(line, false, true);
                string keywordForTemp = null;
                string keywordToTemp = null;
                bool keywordForFound = false;
                bool keywordToFound = false;

                foreach (string word in words)
                {
                    if (!keywordForFound && LevenshteinDistance.IsMatch(word, keywordFor, 1))
                    {
                        keywordForTemp = word;
                        keywordForFound = true;
                    }

                    if (keywordForFound && !keywordToFound && word.Length >= keywordTo.Length &&
                        LevenshteinDistance.IsMatch(word.ToLower(), keywordTo.ToLower(), 1))
                    {
                        keywordToTemp = word;
                        keywordToFound = true;
                    }
                }

                if (keywordForFound && keywordToFound)
                {
                    keywordFor = keywordForTemp;
                    keywordTo = keywordToTemp;
                    token = line;
                    allKeywordsFound = true;
                    break;
                }
            }

            if (!allKeywordsFound)
            {
                return;
            }

            // Get start and end date
            string startDay = null;
            string startMonth = null;
            string startYear = null;
            string endDay = null;
            string endMonth = null;
            string endYear = null;

            string[] arr = Util.SplitString(token, false, true);

            for (int i = 0; i < arr.Length; i++)
            {
                string word = arr[i];

                if (word == keywordFor)
                {
                    // Must contains number
                    if (i + 1 < arr.Length && arr[i + 1] != keywordTo && Regex.IsMatch(arr[i + 1], @"\d"))
                    {
                        startDay = string.IsNullOrEmpty(startDay) ? arr[i + 1] : startDay;
                    }

                    if (i + 2 < arr.Length && arr[i + 2] != keywordTo)
                    {
                        startMonth = string.IsNullOrEmpty(startMonth) ? GetNearestMonth(arr[i + 2]) : startMonth;
                    }

                    if (i + 3 < arr.Length && arr[i + 3] != keywordTo)
                    {
                        startYear = string.IsNullOrEmpty(startYear) ? GetNearestYear(arr[i + 3]) : startYear;
                    }
                }
                else if (word == keywordTo)
                {
                    if (i + 1 < arr.Length && Regex.IsMatch(arr[i + 1], @"\d"))
                    {
                        endDay = string.IsNullOrEmpty(endDay) ? arr[i + 1] : endDay;
                    }

                    if (i + 2 < arr.Length)
                    {
                        endMonth = string.IsNullOrEmpty(endMonth) ? GetNearestMonth(arr[i + 2]) : endMonth;
                    }

                    if (i + 3 < arr.Length)
                    {
                        endYear = string.IsNullOrEmpty(endYear) ? GetNearestYear(arr[i + 3]) : endYear;
                    }
                }
            }

            startDate = startDay + " " + startMonth + " " + startYear;
            startDate = startDate.Trim();
            endDate = endDay + " " + endMonth + " " + endYear;
            endDate = endDate.Trim();

            startDate = GetCompleteDate(startDate);
            endDate = GetCompleteDate(endDate);
        }

        // My Statement 
        // Account Balances (as at 21 Apr 2012) 
        private void GetDatesForCpfStatement_AccountBalances(string ocrText, out string startDate, out string endDate)
        {
            startDate = " ";
            endDate = " ";

            if (string.IsNullOrEmpty(ocrText))
            {
                return;
            }

            string keywordPageNumber = "Page 2";

            if (ocrText.ToLower().Contains(keywordPageNumber.ToLower()))
            {
                return;
            }

            string keywordBalances = "Balances";
            string keywordAt = "at";
            string token = null;
            string[] lines = ocrText.Split(new[] { '\r', '\n' });
            bool allKeywordsFound = false;

            foreach (string line in lines)
            {
                string[] words = Util.SplitString(line, false, true);
                string keywordBalancesTemp = null;
                string keywordAtTemp = null;
                bool keywordAtFound = false;
                bool keywordBalancesFound = false;

                foreach (string word in words)
                {
                    if (!keywordBalancesFound &&
                        LevenshteinDistance.IsMatch(word, keywordBalances, 3))
                    {
                        keywordBalancesTemp = word;
                        keywordBalancesFound = true;
                    }

                    if (keywordBalancesFound && !keywordAtFound && word.Length == 2 &&
                        LevenshteinDistance.IsMatch(word, keywordAt, 1))
                    {
                        keywordAtTemp = word;
                        keywordAtFound = true;
                    }
                }

                if (keywordBalancesFound && keywordAtFound)
                {
                    keywordBalances = keywordBalancesTemp;
                    keywordAt = keywordAtTemp;
                    token = line;
                    allKeywordsFound = true;
                    break;
                }
            }

            if (!allKeywordsFound)
            {
                return;
            }

            // Get start and end date
            string day = null;
            string month = null;
            string year = null;

            string[] arr = Util.SplitString(token, false, true);

            for (int i = 0; i < arr.Length; i++)
            {
                string word = arr[i];

                if (word == keywordAt)
                {
                    if (i + 1 < arr.Length && !Regex.IsMatch(arr[i + 1], @"\d"))
                    {
                        i++;
                    }

                    // Must contains number
                    if (i + 1 < arr.Length && arr[i + 1] != keywordAt && Regex.IsMatch(arr[i + 1], @"\d"))
                    {
                        day = string.IsNullOrEmpty(day) ? arr[i + 1] : day;
                    }

                    if (i + 2 < arr.Length && arr[i + 2] != keywordAt)
                    {
                        month = string.IsNullOrEmpty(month) ? GetNearestMonth(arr[i + 2]) : month;
                    }

                    if (i + 3 < arr.Length && arr[i + 3] != keywordAt)
                    {
                        year = string.IsNullOrEmpty(year) ? GetNearestYear(arr[i + 3]) : year;
                    }
                }
            }

            startDate = day + " " + month + " " + year;
            startDate = startDate.Trim();

            startDate = GetCompleteDate(startDate);
        }

        private void GetDatesForCpfRefund(string ocrText, out string startDate)
        {
            startDate = " ";

            if (string.IsNullOrEmpty(ocrText))
            {
                return;
            }

            string keywordPageNumber = "Page 2";

            if (ocrText.ToLower().Contains(keywordPageNumber.ToLower()))
            {
                return;
            }

            string keywordBalances = "Amount";
            string keywordAt = "at";
            string token = null;
            string[] lines = ocrText.Split(new[] { '\r', '\n' });
            bool allKeywordsFound = false;

            foreach (string line in lines)
            {
                string[] words = Util.SplitString(line, false, true);
                string keywordBalancesTemp = null;
                string keywordAtTemp = null;
                bool keywordAtFound = false;
                bool keywordBalancesFound = false;

                foreach (string word in words)
                {
                    if (!keywordBalancesFound &&
                        LevenshteinDistance.IsMatch(word, keywordBalances, 2))
                    {
                        keywordBalancesTemp = word;
                        keywordBalancesFound = true;
                    }

                    if (keywordBalancesFound && !keywordAtFound && word.Length == 2 &&
                        LevenshteinDistance.IsMatch(word, keywordAt, 1))
                    {
                        keywordAtTemp = word;
                        keywordAtFound = true;
                    }
                }

                if (keywordBalancesFound && keywordAtFound)
                {
                    keywordBalances = keywordBalancesTemp;
                    keywordAt = keywordAtTemp;
                    token = line;
                    allKeywordsFound = true;
                    break;
                }
            }

            if (!allKeywordsFound)
            {
                return;
            }

            // Get start and end date
            string day = null;
            string month = null;
            string year = null;

            string[] arr = Util.SplitString(token, false, true);

            for (int i = 0; i < arr.Length; i++)
            {
                string word = arr[i];

                if (word == keywordAt)
                {
                    if (i + 1 < arr.Length && !Regex.IsMatch(arr[i + 1], @"\d"))
                    {
                        i++;
                    }

                    // Must contains number
                    if (i + 1 < arr.Length && arr[i + 1] != keywordAt && Regex.IsMatch(arr[i + 1], @"\d"))
                    {
                        day = string.IsNullOrEmpty(day) ? arr[i + 1] : day;
                    }

                    if (i + 2 < arr.Length && arr[i + 2] != keywordAt)
                    {
                        month = string.IsNullOrEmpty(month) ? GetNearestMonth(arr[i + 2]) : month;
                    }

                    if (i + 3 < arr.Length && arr[i + 3] != keywordAt)
                    {
                        year = string.IsNullOrEmpty(year) ? GetNearestYear(arr[i + 3]) : year;
                    }
                }
            }

            startDate = day + " " + month + " " + year;
            startDate = startDate.Trim();

            startDate = GetCompleteDate(startDate);
        }

        private string GetNearestMonth(string month)
        {
            if (string.IsNullOrEmpty(month))
            {
                return month;
            }

            month = month.ToLower();
            //Response.Write(month + "<br><br>");

            string[] months = new string[] { 
                "Jan", "Feb", "Mar", "Apr", "May", "Jun", 
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" 
            };
            Dictionary<string, int> dir = new Dictionary<string, int>();

            foreach (string m in months)
            {
                dir.Add(m, LevenshteinDistance.Compute(month, m.ToLower()));
                //Response.Write(m + ": " + LevenshteinDistance.Compute(month, m) + "<br />");
            }

            var sortedDir = (from entry in dir orderby entry.Value ascending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);
            return ((Dictionary<string, int>)sortedDir).First().Key;
        }

        private string GetNearestYear(string year)
        {
            if (string.IsNullOrEmpty(year))
            {
                return year;
            }

            year = year.ToLower();
            int scope = 10;
            int currentYear = DateTime.Now.Year;
            string[] years = new string[scope];
            int count = 0;

            for (int i = currentYear; i > currentYear - scope; i--)
            {
                years[count] = i.ToString();
                count++;
            }

            Dictionary<string, int> dir = new Dictionary<string, int>();

            foreach (string y in years)
            {
                dir.Add(y, LevenshteinDistance.Compute(year, y));
            }

            var sortedDir = (from entry in dir orderby entry.Value ascending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);
            return ((Dictionary<string, int>)sortedDir).First().Key;
        }

        private bool MatchCpfStatementKeywords(string[] keywords, string ocrText, bool ignoreCase, int maxEdits)
        {
            string[] lines = ocrText.Split(new[] { '\r', '\n' });

            foreach (string line in lines)
            {
                int matchCount = 0;
                string[] words = Util.SplitString(line, false, true);

                foreach (string word in words)
                {
                    foreach (string keyword in keywords)
                    {
                        if (ignoreCase)
                        {
                            if (LevenshteinDistance.IsMatch(word.ToLower(), keyword.ToLower(), maxEdits))
                            {
                                matchCount++;
                            }
                        }
                        else
                        {
                            if (LevenshteinDistance.IsMatch(word, keyword, maxEdits))
                            {
                                matchCount++;
                            }
                        }
                    }
                }

                if (matchCount >= keywords.Length)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Get Date for CBR Docs
        private void GetDatesForCbr(string ocrText, out string enquiryDate)
        {
            enquiryDate = " ";

            if (string.IsNullOrEmpty(ocrText))
            {
                return;
            }

            string keywordCbsConsumer = "CBS Consumer";
            string keywordEnquiryReport = "Enquiry Report";
            string keywordEnquiryNumber = "Enquiry Number";
            string keywordEnquiryDate = "Enquiry Date";

            if (!ocrText.ToLower().Contains(keywordCbsConsumer.ToLower()) &&
                !ocrText.ToLower().Contains(keywordEnquiryReport.ToLower()) &&
                !ocrText.ToLower().Contains(keywordEnquiryNumber.ToLower()) &&
                !ocrText.ToLower().Contains(keywordEnquiryDate.ToLower()))
            {
                return;
            }

            string keywordEnquiry = "Enquiry";
            string keywordDate = "Date";
            string token = null;
            string[] lines = ocrText.Split(new[] { '\r', '\n' });
            bool allKeywordsFound = false;

            foreach (string line in lines)
            {
                string[] words = Util.SplitString(line, false, true);
                string keywordEnquiryTemp = null;
                string keywordDateTemp = null;
                bool keywordDateFound = false;
                bool keywordEnquiryFound = false;

                foreach (string word in words)
                {
                    if (!keywordEnquiryFound &&
                        LevenshteinDistance.IsMatch(word, keywordEnquiry, 3))
                    {
                        keywordEnquiryTemp = word;
                        keywordEnquiryFound = true;
                    }

                    if (keywordEnquiryFound && !keywordDateFound &&
                        LevenshteinDistance.IsMatch(word, keywordDate, 1))
                    {
                        keywordDateTemp = word;
                        keywordDateFound = true;
                    }
                }

                if (keywordEnquiryFound && keywordDateFound)
                {
                    keywordEnquiry = keywordEnquiryTemp;
                    keywordDate = keywordDateTemp;
                    token = line;
                    allKeywordsFound = true;
                    break;
                }
            }

            if (!allKeywordsFound)
            {
                return;
            }

            string[] arr = token.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < arr.Length; i++)
            {
                string word = arr[i];

                if (word == keywordDate)
                {
                    if (i + 1 < arr.Length)
                    {
                        enquiryDate = arr[i + 1];
                        break;
                    }
                }
            }

            enquiryDate = string.IsNullOrEmpty(enquiryDate) ? enquiryDate : enquiryDate.Trim();

            enquiryDate = GetCompleteDate(enquiryDate);
        }
        #endregion

        #region Get Date for Payslip
        private void GetDataForPayslip(ArrayList personalData,
            out string startDate, out string endDate, out string amount)
        {
            startDate = " ";
            endDate = " ";
            amount = " ";

            if (string.IsNullOrEmpty(ocrText) || personalData.Count == 0)
            {
                return;
            }

            startDate = null;
            endDate = null;
            amount = null;

            PersonalData r = (PersonalData)personalData[0];

            DateTime?[] monthName = new DateTime?[12];

            monthName[0] = (String.IsNullOrEmpty(r.Month1Name) ? null : ConvertToDate(r.Month1Name));
            monthName[1] = (String.IsNullOrEmpty(r.Month2Name) ? null : ConvertToDate(r.Month2Name));
            monthName[2] = (String.IsNullOrEmpty(r.Month3Name) ? null : ConvertToDate(r.Month3Name));
            monthName[3] = (String.IsNullOrEmpty(r.Month4Name) ? null : ConvertToDate(r.Month4Name));
            monthName[4] = (String.IsNullOrEmpty(r.Month5Name) ? null : ConvertToDate(r.Month5Name));
            monthName[5] = (String.IsNullOrEmpty(r.Month6Name) ? null : ConvertToDate(r.Month6Name));
            monthName[6] = (String.IsNullOrEmpty(r.Month7Name) ? null : ConvertToDate(r.Month7Name));
            monthName[7] = (String.IsNullOrEmpty(r.Month8Name) ? null : ConvertToDate(r.Month8Name));
            monthName[8] = (String.IsNullOrEmpty(r.Month9Name) ? null : ConvertToDate(r.Month9Name));
            monthName[9] = (String.IsNullOrEmpty(r.Month10Name) ? null : ConvertToDate(r.Month10Name));
            monthName[10] = (String.IsNullOrEmpty(r.Month11Name) ? null : ConvertToDate(r.Month11Name));
            monthName[11] = (String.IsNullOrEmpty(r.Month12Name) ? null : ConvertToDate(r.Month12Name));

            // Replace OCR recognization error
            ocrText = CorrectOcrError(ocrText, monthName);

            string[] monthValue = new string[12];

            monthValue[0] = (String.IsNullOrEmpty(r.Month1Value) ? null : r.Month1Value);
            monthValue[1] = (String.IsNullOrEmpty(r.Month2Value) ? null : r.Month2Value);
            monthValue[2] = (String.IsNullOrEmpty(r.Month3Value) ? null : r.Month3Value);
            monthValue[3] = (String.IsNullOrEmpty(r.Month4Value) ? null : r.Month4Value);
            monthValue[4] = (String.IsNullOrEmpty(r.Month5Value) ? null : r.Month5Value);
            monthValue[5] = (String.IsNullOrEmpty(r.Month6Value) ? null : r.Month6Value);
            monthValue[6] = (String.IsNullOrEmpty(r.Month7Value) ? null : r.Month7Value);
            monthValue[7] = (String.IsNullOrEmpty(r.Month8Value) ? null : r.Month8Value);
            monthValue[8] = (String.IsNullOrEmpty(r.Month9Value) ? null : r.Month9Value);
            monthValue[9] = (String.IsNullOrEmpty(r.Month10Value) ? null : r.Month10Value);
            monthValue[10] = (String.IsNullOrEmpty(r.Month11Value) ? null : r.Month11Value);
            monthValue[11] = (String.IsNullOrEmpty(r.Month12Value) ? null : r.Month12Value);

            string[] dateFormats = new string[] { 
                "MMMM yyyy", 
                "MMM yyyy",
                "MMM yy",
                "MM.yyyy",
                "MM/yyyy",
                "MM/yy",
                "MMM-yy",
                "MMM-yyyy",
                "dd.MM.yyyy",
                "dd/MM/yyyy",
                "dd/MM/yy",
                "dd-MMM-yy",
                "dd-MMM-yyyy"
            };

            for (int i = 0; i < monthName.Length; i++)
            {
                if (monthName[i] != null)
                {
                    string month = null;

                    for (int j = 0; j < dateFormats.Length; j++)
                    {
                        month = monthName[i].Value.ToString(dateFormats[j]);

                        if (ocrText.ToLower().Contains(month.ToLower()))
                        {
                            month = monthName[i].Value.ToString("MMM yyyy");
                            startDate = month;
                            endDate = month;
                            amount = monthValue[i];

                            i = monthName.Length; // Break outer loop
                            break; // Break inner loop
                        }
                    }
                }
            }

            startDate = GetCompleteDate(startDate);
            endDate = GetCompleteDate(endDate);
        }

        private DateTime? ConvertToDate(string yearMonth)
        {
            DateTime? date = null;

            if (string.IsNullOrEmpty(yearMonth) && yearMonth.Trim().Length != 6)
            {
                return null;
            }

            yearMonth = yearMonth.Trim();

            try
            {
                int year = int.Parse(yearMonth.Substring(0, 4));
                int month = int.Parse(yearMonth.Substring(4, 2));
                int day = 1;
                date = new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }

            return date;
        }

        private string CorrectOcrError(string ocrText, DateTime?[] monthName)
        {
            if (string.IsNullOrEmpty(ocrText))
            {
                return null;
            }

            ocrText = ocrText.ToLower();

            string[] words = Util.SplitString(ocrText, true, true);
            //string[] monthShort = new string[] { 
            //    "Jan", "Feb", "Mar", "Apr", "May", "Jun", 
            //    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" 
            //};
            //string[] monthLong = new string[] { 
            //    "January", "February", "March", "April", "May", "June", 
            //    "July", "August", "September", "October", "November", "December" 
            //};

            foreach (string word in words)
            {
                string wordLower = word.ToLower();

                foreach (DateTime? month in monthName)
                {
                    if (month != null)
                    {
                        string monthStr;

                        monthStr = month.Value.ToString("MMM").ToLower();

                        if (wordLower.Length >= 3 && LevenshteinDistance.IsMatch(wordLower, monthStr, 1))
                        {
                            if (wordLower != "mar" && wordLower != "may" && wordLower != "jun" && wordLower != "jul")
                            {
                                ocrText = ocrText.Replace(wordLower, monthStr);
                            }
                        }

                        monthStr = month.Value.ToString("MMMM").ToLower();

                        if (monthStr.Length > 4)
                        {
                            if (wordLower.Length > 4 && LevenshteinDistance.IsMatch(wordLower, monthStr, 2))
                            {
                                ocrText = ocrText.Replace(wordLower, monthStr);
                            }
                        }
                        else
                        {
                            if (wordLower.Length >= 3 && LevenshteinDistance.IsMatch(wordLower, monthStr, 1))
                            {
                                ocrText = ocrText.Replace(wordLower, monthStr);
                            }
                        }
                    }
                }
            }

            return ocrText;
        }
        #endregion

        #region Get Company Name for Docs
        private string GetCompanyName(ArrayList personalData)
        {
            string result = " ";

            if (docId > 0)
            {
                AppDocRefDb appDocRefDb = new AppDocRefDb();
                AppDocRef.AppDocRefDataTable appDocRefDt = appDocRefDb.GetAppDocRefByDocId(docId);

                if (appDocRefDt.Rows.Count > 0)
                {
                    AppDocRef.AppDocRefRow appDocRefDr = appDocRefDt[0];

                    AppPersonalDb appPersonalDb = new AppPersonalDb();
                    AppPersonal.AppPersonalDataTable appPerDt = appPersonalDb.GetAppPersonalById(appDocRefDr.AppPersonalId);

                    if (appPerDt.Rows.Count > 0)
                    {
                        AppPersonal.AppPersonalRow appPerDr = appPerDt[0];
                        result = (String.IsNullOrEmpty(appPerDr.CompanyName) ? " " : appPerDr.CompanyName);
                    }
                }
            }

            return result;
        }
        #endregion

        private string GetCompleteDate(string date)
        {
            string completeDateString = date;

            if (!String.IsNullOrEmpty(completeDateString) || date != null)
            {
                // Format the string to save the complete date
                DateTime completeDate = new DateTime();

                if (DateTime.TryParse(completeDateString, out completeDate))
                {
                    completeDateString = Format.FormatDateTime(completeDate, DateTimeFormat.dd__MMM__yyyy);
                }
            }

            return completeDateString;
        }

        public void CreateMetaDataForWebService(string certNo, string certDate, string localForeign, string marriageType, string docStartDate, string docEndDate, string identityNoSub)
        {
            ArrayList metaList = new ArrayList(); // Meta data container

            MetaDataOcr metaDataOcr = null;

            string certNoFieldName = EnumManager.GetMetadataCertNo(this.docType);
            string certDateFieldName = EnumManager.GetMetadataCertDate(this.docType);
            string localForeignFieldName = EnumManager.GetMetadataLocalForeign(this.docType);
            string marriageTypeFieldName = EnumManager.GetMetadataMarriageType(this.docType);
            string docStartDateFieldName = EnumManager.GetMetadataStartDate(this.docType);
            string docEndDateFieldName = EnumManager.GetMetadataEndDate(this.docType);
            string identityNoSubFieldName = EnumManager.GetMetadataIdentityNoSub(this.docType);

            if (!String.IsNullOrEmpty(certNoFieldName))
            {
                // Create metadata for certificate number
                metaDataOcr = new MetaDataOcr();
                metaDataOcr.FieldName = certNoFieldName;
                metaDataOcr.FieldValue = certNo;
                metaDataOcr.VerificationMandatory = false;
                metaDataOcr.CompletenessMandatory = false;
                metaDataOcr.VerificationVisible = true;
                metaDataOcr.CompletenessVisible = true;
                metaDataOcr.IsFixed = true;
                metaList.Add(metaDataOcr);
            }

            if (!String.IsNullOrEmpty(certDateFieldName))
            {
                // Create metadata for certificate date
                metaDataOcr = new MetaDataOcr();
                metaDataOcr.FieldName = certDateFieldName;

                //for hle, MortgageLoanForm, NoLoanNotification update Yes, No 
                if (this.docType.Equals(DocTypeEnum.HLE.ToString()) ||
                    this.docType.Equals(DocTypeEnum.MortgageLoanForm.ToString()) ||
                    this.docType.Equals(DocTypeEnum.NoLoanNotification.ToString()))
                {
                    try
                    {
                        DateTime certDateInDateFormat = DateTime.Now;
                        bool isInDateFormat = DateTime.TryParse(certDate, out certDateInDateFormat);

                        if (isInDateFormat) // if valid date
                        {
                            if (certDateInDateFormat.Year == 0001 && certDateInDateFormat.Month == 01 && certDateInDateFormat.Day == 01) // if date is in format 0001-01-01 then consider it as null
                                metaDataOcr.FieldValue = DocTypeMetaDataValueYesNoEnum.No.ToString();
                            else
                                metaDataOcr.FieldValue = DocTypeMetaDataValueYesNoEnum.Yes.ToString();
                        }
                        else
                            metaDataOcr.FieldValue = DocTypeMetaDataValueYesNoEnum.No.ToString();
                    }
                    catch (Exception)
                    {
                        metaDataOcr.FieldValue = DocTypeMetaDataValueYesNoEnum.No.ToString();
                    }
                }
                else
                    metaDataOcr.FieldValue = certDate;
                metaDataOcr.VerificationMandatory = false;
                metaDataOcr.CompletenessMandatory = false;
                metaDataOcr.VerificationVisible = true;
                metaDataOcr.CompletenessVisible = true;
                metaDataOcr.IsFixed = true;

                metaList.Add(metaDataOcr);
            }

            //if (!String.IsNullOrEmpty(localForeignFieldName))
            //{
            //    // Create metadata for LocalForeign
            //    metaDataOcr = new MetaDataOcr();
            //    metaDataOcr.FieldName = localForeignFieldName;
            //    metaDataOcr.FieldValue = (localForeign.ToLower().IndexOf("f") >= 0) ? TagGeneralEnum.Foreign.ToString() : TagGeneralEnum.Local.ToString();
            //    metaDataOcr.VerificationMandatory = false;
            //    metaDataOcr.CompletenessMandatory = false;
            //    metaDataOcr.VerificationVisible = true;
            //    metaDataOcr.CompletenessVisible = true;
            //    metaDataOcr.IsFixed = true;

            //    metaList.Add(metaDataOcr);
            //}

            if (!String.IsNullOrEmpty(marriageTypeFieldName))
            {
                // Create metadata for MarriageType
                metaDataOcr = new MetaDataOcr();
                metaDataOcr.FieldName = marriageTypeFieldName;
                if (!String.IsNullOrEmpty(localForeignFieldName))
                {
                    if (localForeignFieldName.ToLower().IndexOf("f") >= 0)
                        metaDataOcr.FieldValue = TagEnum.Foreign.ToString();
                    else
                        metaDataOcr.FieldValue = (marriageType.ToLower().IndexOf("m") >= 0) ? TagEnum.Local_Muslim.ToString() : TagEnum.Local_Civil.ToString();
                }
                else
                    metaDataOcr.FieldValue = TagEnum.Foreign.ToString();
                metaDataOcr.VerificationMandatory = false;
                metaDataOcr.CompletenessMandatory = false;
                metaDataOcr.VerificationVisible = true;
                metaDataOcr.CompletenessVisible = true;
                metaDataOcr.IsFixed = true;

                metaList.Add(metaDataOcr);
            }

            if (!String.IsNullOrEmpty(docStartDateFieldName))
            {
                //docStartDateFieldname
                metaDataOcr = new MetaDataOcr();
                metaDataOcr.FieldName = docStartDateFieldName;

                try
                {
                    DateTime docStartDateFormat = DateTime.Now;
                    if (DateTime.TryParseExact(docStartDate, "dd/MM/yyyy HH:mm:ss", CultureInfo.CreateSpecificCulture("en-GB"), DateTimeStyles.None, out docStartDateFormat))
                    {
                        if (docStartDateFormat.Year == 0001 && docStartDateFormat.Month == 01 && docStartDateFormat.Day == 01) // if date is in format 0001-01-01 then consider it as null
                        {
                            // do nothing
                        }
                        else
                        {
                            //Util.DWMSLog(string.Empty, "TryParseExact Date update format" + docStartDateFormat.ToString(), EventLogEntryType.Information);
                            metaDataOcr.FieldValue = Format.FormatDateTime(docStartDateFormat, DateTimeFormat.dd_Hyp_MM_Hyp_yyyy);
                            //Util.DWMSLog(string.Empty, "TryParseExact Date update format" + metaDataOcr.FieldValue.ToString(), EventLogEntryType.Information);
                            metaDataOcr.VerificationMandatory = false;
                            metaDataOcr.CompletenessMandatory = false;
                            metaDataOcr.VerificationVisible = true;
                            metaDataOcr.CompletenessVisible = true;
                            metaDataOcr.IsFixed = true;

                            metaList.Add(metaDataOcr);
                        }
                    }
                    else
                    {
                        // eg. Year of Assessment
                        metaDataOcr.FieldValue = docStartDate.Trim();
                        metaDataOcr.VerificationMandatory = false;
                        metaDataOcr.CompletenessMandatory = false;
                        metaDataOcr.VerificationVisible = true;
                        metaDataOcr.CompletenessVisible = true;
                        metaDataOcr.IsFixed = true;

                        metaList.Add(metaDataOcr);
                    }
                }
                catch (Exception)
                {
                   
                }
            }

            if (!String.IsNullOrEmpty(docEndDateFieldName))
            {
                //docEndDateFieldname
                metaDataOcr = new MetaDataOcr();
                metaDataOcr.FieldName = docEndDateFieldName;

                try
                {
                    DateTime docEndDateFormat = DateTime.Now;

                    if (DateTime.TryParseExact(docEndDate, "dd/MM/yyyy HH:mm:ss", CultureInfo.CreateSpecificCulture("en-GB"), DateTimeStyles.None, out docEndDateFormat))
                    {
                        if (docEndDateFormat.Year == 0001 && docEndDateFormat.Month == 01 && docEndDateFormat.Day == 01) // if date is in format 0001-01-01 then consider it as null
                        {
                            // do nothing
                        }
                        else
                        {
                            if (docEndDateFormat.Day == 1)
                                docEndDateFormat = new DateTime(docEndDateFormat.Year, docEndDateFormat.Month, DateTime.DaysInMonth(docEndDateFormat.Year, docEndDateFormat.Month));
                            metaDataOcr.FieldValue = Format.FormatDateTime(docEndDateFormat, DateTimeFormat.dd_Hyp_MM_Hyp_yyyy);
                            metaDataOcr.VerificationMandatory = false;
                            metaDataOcr.CompletenessMandatory = false;
                            metaDataOcr.VerificationVisible = true;
                            metaDataOcr.CompletenessVisible = true;
                            metaDataOcr.IsFixed = true;

                            metaList.Add(metaDataOcr);
                        }
                    }
                    else
                    {
                        // eg. Year of Assessment
                        metaDataOcr.FieldValue = docEndDate.Trim();
                        metaDataOcr.VerificationMandatory = false;
                        metaDataOcr.CompletenessMandatory = false;
                        metaDataOcr.VerificationVisible = true;
                        metaDataOcr.CompletenessVisible = true;
                        metaDataOcr.IsFixed = true;

                        metaList.Add(metaDataOcr);
                    }
                }
                catch (Exception)
                {

                }


                
            }

            if (!String.IsNullOrEmpty(identityNoSubFieldName) && !(String.IsNullOrEmpty(EnumManager.GetMetadataIdentityNoSubIDType(docType))))
            {
                //identityNoSubFieldname
                if (!String.IsNullOrEmpty(identityNoSub))
                {
                    metaDataOcr = new MetaDataOcr();
                    metaDataOcr.FieldName = identityNoSubFieldName;
                    metaDataOcr.FieldValue = identityNoSub;
                    metaDataOcr.VerificationMandatory = false;
                    metaDataOcr.CompletenessMandatory = false;
                    metaDataOcr.VerificationVisible = true;
                    metaDataOcr.CompletenessVisible = true;
                    metaDataOcr.IsFixed = true;
                    metaList.Add(metaDataOcr);

                    
                    metaDataOcr = new MetaDataOcr();
                    metaDataOcr.FieldName = EnumManager.GetMetadataIdentityNoSubIDType(this.docType);
                    metaDataOcr.FieldValue = Retrieve.GetIdTypeByNRIC(identityNoSub);
                    metaDataOcr.VerificationMandatory = false;
                    metaDataOcr.CompletenessMandatory = false;
                    metaDataOcr.VerificationVisible = true;
                    metaDataOcr.CompletenessVisible = true;
                    metaDataOcr.IsFixed = true;
                    metaList.Add(metaDataOcr);
                }
            }

            MetaData = metaList;
        }
    }
}
