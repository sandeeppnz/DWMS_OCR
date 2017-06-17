using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.AppPersonalTableAdapters;
using DWMS_OCR.App_Code.Dal;
using DWMS_OCR.App_Code.Helper;
using System.Collections;

namespace DWMS_OCR.App_Code.Bll
{
    class AppPersonalDb
    {
        private AppPersonalTableAdapter _AppPersonalTableAdapter = null;

        protected AppPersonalTableAdapter Adapter
        {
            get
            {
                if (_AppPersonalTableAdapter == null)
                    _AppPersonalTableAdapter = new AppPersonalTableAdapter();

                return _AppPersonalTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public AppPersonal.AppPersonalDataTable GetAppPersonals()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get personal data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AppPersonal.AppPersonalDataTable GetAppPersonalById(int id)
        {
            return Adapter.GetDataById(id);
        }

        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public AppPersonal.AppPersonalDataTable GetAppPersonalsByDocAppId(int docAppId)
        {
            return Adapter.GetDataByDocAppId(docAppId);
        }

        /// <summary>
        /// Get the app personal
        /// </summary>
        /// <param name="docSetId"></param>
        /// <returns></returns>
        public AppPersonal.AppPersonalDataTable GetAppPersonalsByDocSetId(int docSetId)
        {
            return Adapter.GetDataByDocSetId(docSetId);
        }

        /// <summary>
        /// Get app personal
        /// </summary>
        /// <param name="docSetId"></param>
        /// <param name="refType"></param>
        /// <returns></returns>
        public AppPersonal.AppPersonalDataTable GetAppPersonalsByDocSetIdAndRefType(int docSetId, string refType)
        {
            return Adapter.GetDataByDocSetIdAndRefType(docSetId, refType);
        }

        public AppPersonal.AppPersonalDataTable GetAppPersonalByNricFolderRelationshipDocAppId(string nric, string folder, string relationship, int docAppId)
        {
            return Adapter.GetDataByNricFolderRelationshipDocAppId(nric, docAppId, folder, relationship);
        }

        public AppPersonal.AppPersonalDataTable GetAppPersonalByNricAndRefNo(string nric, string refNo)
        {
            return Adapter.GetDataByNricAndRefNo(nric, refNo);
        }

        public AppPersonal.AppPersonalDataTable GetAppPersonalByCustomerSourceIdAndRefNo(string customerSourceId, string refNo)
        {
            return Adapter.GetDataByCustomerSourceIdandRefNo(customerSourceId, refNo);
        }

        public string GetCustomerSourceIdByDocAppIdAndNric(int docAppId, string nric)
        {
            return Adapter.GetCustomerSourceIdByDocAppIdAndNric(docAppId, nric);
        }


        public string GetCustomerNameByDocAppIdAndNric(int docAppId, string nric)
        {
            return Adapter.GetCustomerNameByDocAppIdAndNRic(docAppId, nric);
        }
        //Added By Edward 22.11.2013 for Leas Service
        public AppPersonal.AppPersonalDataTable GetAppPersonalByNricAndDocAppId(string nric, int docAppId)
        {
            return Adapter.GetAppPersonalByNricAndDocAppId(nric, docAppId);
        }    

        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert method
        /// </summary>
        /// <param name="AppAppPersonalRefId"></param>
        /// <param name="docType"></param>
        /// <param name="originalSetId"></param>
        /// <param name="status"></param>
        /// <param name="referenceNumber"></param>
        /// <param name="nric"></param>
        /// <returns></returns>
        public int Insert(int docAppId, string nric, string name, string personalType,
            string dateJoinedService, string companyName, string employmentType, string folder, 
            string relationship, int orderNo)
        {
            AppPersonal.AppPersonalDataTable dt = new AppPersonal.AppPersonalDataTable();
            AppPersonal.AppPersonalRow r = dt.NewAppPersonalRow();

            r.DocAppId = docAppId;
            r.Nric = (nric == null ? " " : nric);
            r.Name = (name == null ? " " : name);
            r.PersonalType = (personalType == null ? " " : personalType);
            r.DateJoinedService = (dateJoinedService == null ? " " : dateJoinedService);
            r.CompanyName = (companyName == null ? " " : companyName);
            r.EmploymentType = (employmentType == null ? " " : employmentType);
            r.Folder = (folder == null ? " " : folder);

            r.CustomerType = CustomerTypeEnum.P.ToString(); // as of now the CustomerType is defaulted to P.
            r.IdType = Retrieve.GetIdTypeByNRIC(nric);

            if (relationship.Equals(RelationshipEnum.Husband.ToString()) ||
                relationship.Equals(RelationshipEnum.Wife.ToString()))
                r.Relationship = relationship;

            r.OrderNo = orderNo;

            dt.AddAppPersonalRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }

        //Added By Edward 06.01.2014 for Inserting MonthsToLeas
        public int Insert(int docAppId, string nric, string name, string personalType,
            string dateJoinedService, string companyName, string employmentType, string folder,
            string relationship, int orderNo, int noOfIncomeMonths)
        {
            AppPersonal.AppPersonalDataTable dt = new AppPersonal.AppPersonalDataTable();
            AppPersonal.AppPersonalRow r = dt.NewAppPersonalRow();

            r.DocAppId = docAppId;
            r.Nric = (nric == null ? " " : nric);
            r.Name = (name == null ? " " : name);
            r.PersonalType = (personalType == null ? " " : personalType);
            r.DateJoinedService = (dateJoinedService == null ? " " : dateJoinedService);
            r.CompanyName = (companyName == null ? " " : companyName);
            r.EmploymentType = (employmentType == null ? " " : employmentType);
            r.Folder = (folder == null ? " " : folder);

            r.CustomerType = CustomerTypeEnum.P.ToString(); // as of now the CustomerType is defaulted to P.
            r.IdType = Retrieve.GetIdTypeByNRIC(nric);

            if (relationship.Equals(RelationshipEnum.Husband.ToString()) ||
                relationship.Equals(RelationshipEnum.Wife.ToString()))
                r.Relationship = relationship;

            r.OrderNo = orderNo;
            r.MonthsToLEAS = noOfIncomeMonths;

            dt.AddAppPersonalRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }
        #endregion

        //#region Insert Methods
        ///// <summary>
        ///// Insert method
        ///// </summary>
        ///// <param name="AppAppPersonalRefId"></param>
        ///// <param name="docType"></param>
        ///// <param name="originalSetId"></param>
        ///// <param name="status"></param>
        ///// <param name="referenceNumber"></param>
        ///// <param name="nric"></param>
        ///// <returns></returns>
        //public int Insert(int docAppId, string nric, string name, string personalType,
        //    string dateJoinedService, string companyName, string employmentType,
        //    string folder, RelationshipEnum? relationship, int orderNo, string customerId)
        //{
        //    AppPersonal.AppPersonalDataTable dt = new AppPersonal.AppPersonalDataTable();
        //    AppPersonal.AppPersonalRow r = dt.NewAppPersonalRow();

        //    r.DocAppId = docAppId;
        //    r.Nric = nric;
        //    r.Name = name;
        //    r.PersonalType = personalType;
        //    r.DateJoinedService = dateJoinedService;
        //    r.CompanyName = companyName;
        //    r.EmploymentType = employmentType;
        //    r.Folder = folder;
        //    r.OrderNo = orderNo;
        //    if (!string.IsNullOrEmpty(customerId))
        //        r.CustomerSourceId = customerId;

        //    if (relationship != null && (relationship.Value == RelationshipEnum.Requestor || relationship.Value == RelationshipEnum.Spouse))
        //        r.Relationship = relationship.ToString();

        //    r.CustomerType = CustomerTypeEnum.P.ToString(); // as of now the CustomerType is defaulted to P.

        //    r.IdType = Retrieve.GetIdTypeByNRIC(nric);

        //    dt.AddAppPersonalRow(r);
        //    int rowsAffected = Adapter.Update(dt);

        //    //if (rowsAffected > 0)
        //    //{
        //    //    AuditTrailDb auditTrailDb = new AuditTrailDb();
        //    //    auditTrailDb.Record(TableNameEnum.AppPersonal, r.Id.ToString(), OperationTypeEnum.Insert);
        //    //}

        //    return r.Id;
        //}

        //#endregion

        #region Update Methods
        /// <summary>
        /// Update personal
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nric"></param>
        /// <param name="name"></param>
        /// <param name="personalType"></param>
        /// <param name="dateJoinedService"></param>
        /// <param name="companyName"></param>
        /// <param name="employmentType"></param>
        /// <param name="folder"></param>
        /// <param name="relationship"></param>
        /// <returns></returns>
        public bool Update(int id, string nric, string name, string personalType,
            string dateJoinedService, string companyName, string employmentType, string folder, 
            string relationship, int orderNo)
        {
            AppPersonal.AppPersonalDataTable appPersonal = GetAppPersonalById(id);
            if (appPersonal.Count == 0) return false;

            AppPersonal.AppPersonalRow r = appPersonal[0];

            r.Nric = nric;
            r.Name = name;
            r.PersonalType = personalType;
            r.DateJoinedService = dateJoinedService;
            r.CompanyName = companyName;
            r.EmploymentType = employmentType;
            r.Folder = folder;

            r.IdType = Retrieve.GetIdTypeByNRIC(nric);

            if (relationship.Equals(RelationshipEnum.Husband.ToString()) ||
                relationship.Equals(RelationshipEnum.Wife.ToString()))
                r.Relationship = relationship;

            r.OrderNo = orderNo;

            int affected = Adapter.Update(appPersonal);
            return affected > 0;
        }

        //Added by Edward 06.01.2014 Added MonthsToLEAS
        public bool Update(int id, string nric, string name, string personalType,
            string dateJoinedService, string companyName, string employmentType, string folder,
            string relationship, int orderNo, int noOfIncomeMonths)
        {
            AppPersonal.AppPersonalDataTable appPersonal = GetAppPersonalById(id);
            if (appPersonal.Count == 0) return false;

            AppPersonal.AppPersonalRow r = appPersonal[0];

            r.Nric = nric;
            r.Name = name;
            r.PersonalType = personalType;
            r.DateJoinedService = dateJoinedService;
            r.CompanyName = companyName;
            r.EmploymentType = employmentType;
            r.Folder = folder;

            r.IdType = Retrieve.GetIdTypeByNRIC(nric);

            if (relationship.Equals(RelationshipEnum.Husband.ToString()) ||
                relationship.Equals(RelationshipEnum.Wife.ToString()))
                r.Relationship = relationship;

            r.OrderNo = orderNo;
            r.MonthsToLEAS = noOfIncomeMonths;

            int affected = Adapter.Update(appPersonal);
            return affected > 0;
        }

        public bool UpdateRelationshipToHusbandOrWife(int id, string relationship)
        {
            AppPersonal.AppPersonalDataTable appPersonal = GetAppPersonalById(id);
            if (appPersonal.Count == 0) return false;

            AppPersonal.AppPersonalRow r = appPersonal[0];

            if (relationship.Equals(RelationshipEnum.Husband.ToString()) ||
                relationship.Equals(RelationshipEnum.Wife.ToString()))
                r.Relationship = relationship;

            int affected = Adapter.Update(appPersonal);
            return affected > 0;
        }
        #endregion

        #region Delete Methods
        #endregion

        #region Miscellaneous
        /// <summary>
        /// SAve the personal records
        /// </summary>
        /// <param name="docSetId"></param>
        /// <param name="docAppId"></param>
        public void SavePersonalRecords(int docSetId, int docAppId)
        {
            AppPersonalSalaryDb appPersonalSalaryDb = new AppPersonalSalaryDb();
            DocAppDb docAppDb = new DocAppDb();
            DocApp.DocAppDataTable docAppTable = docAppDb.GetDocAppById(docAppId);

            if (docAppTable.Rows.Count > 0)
            {
                DocApp.DocAppRow docApp = docAppTable[0];
                string refType = docApp.RefType.Trim();
                string refNo = docApp.RefNo.Trim();

                // Get the personal info, basing on the reference number, from the interface files
                ArrayList personals = GetPersonalDataFromInterface(refNo, refType);                

                if (personals.Count > 0)
                {
                    AppPersonal.AppPersonalDataTable currPersonalTable = GetAppPersonalsByDocAppId(docAppId);

                    foreach (PersonalData personal in personals)
                    {
                        bool isNew = true;

                        foreach (AppPersonal.AppPersonalRow currPersonalRow in currPersonalTable)
                        {
                            // Check if the AppPersonal record matches the NRIC from interface file AND the personal type is not empty
                            if (currPersonalRow.Nric.ToLower().Equals(personal.Nric.ToLower()) && !String.IsNullOrEmpty(currPersonalRow.PersonalType))
                            {
                                // Update personal records
                                #region Modified by Edward 06.01.2014 Added NoOfIncomeMonths
                                //Update(currPersonalRow.Id, personal.Nric, personal.Name, personal.PersonalType,
                                //    personal.DateJoinedService, personal.CompanyName, personal.EmploymentType, DocFolderEnum.Unidentified.ToString(),
                                //    personal.Relationship, personal.OrderNo);

                                Update(currPersonalRow.Id, personal.Nric, personal.Name, personal.PersonalType,
                                    personal.DateJoinedService, personal.CompanyName, personal.EmploymentType, DocFolderEnum.Unidentified.ToString(),
                                    personal.Relationship, personal.OrderNo, personal.NoOfIncomeMonths);
                                #endregion

                                AppPersonalSalary.AppPersonalSalaryDataTable personalSalaryTable = appPersonalSalaryDb.GetAppPersonalSalaryByAppPersonalId(currPersonalRow.Id);

                                if (personalSalaryTable.Rows.Count > 0)
                                {
                                    AppPersonalSalary.AppPersonalSalaryRow personalSalaryRow = personalSalaryTable[0];

                                    // Update AppPersonalSalary records
                                    appPersonalSalaryDb.Update(personalSalaryRow.Id, personal.Month1Name, personal.Month1Value, personal.Month2Name, personal.Month2Value,
                                        personal.Month3Name, personal.Month3Value, personal.Month4Name, personal.Month4Value, personal.Month5Name, personal.Month5Value,
                                        personal.Month6Name, personal.Month6Value, personal.Month7Name, personal.Month7Value, personal.Month8Name, personal.Month8Value,
                                        personal.Month9Name, personal.Month9Value, personal.Month10Name, personal.Month10Value, personal.Month11Name, personal.Month11Value,
                                        personal.Month12Name, personal.Month12Value);
                                }
                                else
                                {
                                    if (personal.HasSalary)
                                    {
                                        // Create AppPersonalSalary records
                                        appPersonalSalaryDb.Insert(currPersonalRow.Id, personal.Month1Name, personal.Month1Value, personal.Month2Name, personal.Month2Value,
                                            personal.Month3Name, personal.Month3Value, personal.Month4Name, personal.Month4Value, personal.Month5Name, personal.Month5Value,
                                            personal.Month6Name, personal.Month6Value, personal.Month7Name, personal.Month7Value, personal.Month8Name, personal.Month8Value,
                                            personal.Month9Name, personal.Month9Value, personal.Month10Name, personal.Month10Value, personal.Month11Name, personal.Month11Value,
                                            personal.Month12Name, personal.Month12Value);
                                    }
                                }

                                isNew = false;
                            }
                        }

                        if (isNew)
                        {
                            // Create AppPersonal records
                            #region Modified by Edward 06.01.2014 Added NoOfIncomeMonths
                            //int appPersonalId = Insert(docAppId, personal.Nric, personal.Name, personal.PersonalType,
                            //    personal.DateJoinedService, personal.CompanyName, personal.EmploymentType, DocFolderEnum.Unidentified.ToString(),
                            //    personal.Relationship, personal.OrderNo);

                            int appPersonalId = Insert(docAppId, personal.Nric, personal.Name, personal.PersonalType,
                                personal.DateJoinedService, personal.CompanyName, personal.EmploymentType, DocFolderEnum.Unidentified.ToString(),
                                personal.Relationship, personal.OrderNo, personal.NoOfIncomeMonths);
                            #endregion
                            if (personal.HasSalary)
                            {
                                // Create AppPersonalSalary records
                                appPersonalSalaryDb.Insert(appPersonalId, personal.Month1Name, personal.Month1Value, personal.Month2Name, personal.Month2Value,
                                    personal.Month3Name, personal.Month3Value, personal.Month4Name, personal.Month4Value, personal.Month5Name, personal.Month5Value,
                                    personal.Month6Name, personal.Month6Value, personal.Month7Name, personal.Month7Value, personal.Month8Name, personal.Month8Value,
                                    personal.Month9Name, personal.Month9Value, personal.Month10Name, personal.Month10Value, personal.Month11Name, personal.Month11Value,
                                    personal.Month12Name, personal.Month12Value);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the personal data from the interface files
        /// </summary>
        /// <param name="refNo"></param>
        /// <param name="refType"></param>
        /// <returns></returns>
        private ArrayList GetPersonalDataFromInterface(string refNo, string refType)
        {
            ArrayList result = new ArrayList();

            if (refType.Equals(ReferenceTypeEnum.HLE.ToString()))
            {
                #region Get personal data from HLE interface
                HleInterfaceDb hleInterfaceDb = new HleInterfaceDb();
                HleInterface.HleInterfaceDataTable hleTable = hleInterfaceDb.GetHleInterfaceByHleNumber(refNo);

                foreach (HleInterface.HleInterfaceRow hleRow in hleTable.Rows)
                {
                    PersonalData personalData = new PersonalData(hleRow);
                    result.Add(personalData);
                }
                #endregion
            }
            else if (refType.Equals(ReferenceTypeEnum.SALES.ToString()))
            {
                #region Get personal data from SALES interface
                SalesInterfaceDb salesInterfaceDb = new SalesInterfaceDb();
                SalesInterface.SalesInterfaceDataTable salesTable = salesInterfaceDb.GetSalesInterfaceByRefNo(refNo);

                foreach (SalesInterface.SalesInterfaceRow salesRow in salesTable.Rows)
                {
                    PersonalData personalData = new PersonalData(salesRow);
                    result.Add(personalData);
                }
                #endregion
            }
            else if (refType.Equals(ReferenceTypeEnum.RESALE.ToString()))
            {
                #region Get personal data from RESALE interface
                ResaleInterfaceDb resaleInterfaceDb = new ResaleInterfaceDb();
                ResaleInterface.ResaleInterfaceDataTable resaleTable = resaleInterfaceDb.GetResaleInterfaceByCaseNo(refNo);

                foreach (ResaleInterface.ResaleInterfaceRow resaleRow in resaleTable.Rows)
                {
                    PersonalData personalData = new PersonalData(resaleRow);
                    result.Add(personalData);
                }
                #endregion
            }
            else if (refType.Equals(ReferenceTypeEnum.SERS.ToString()))
            {
                #region Get personal data from SERS interface
                SersInterfaceDb sersInterfaceDb = new SersInterfaceDb();
                SersInterface.SersInterfaceDataTable sersTable = sersInterfaceDb.GetSersInterfaceByRefNo(refNo);

                foreach (SersInterface.SersInterfaceRow sersRow in sersTable.Rows)
                {
                    PersonalData personalData = new PersonalData(sersRow);
                    result.Add(personalData);
                }
                #endregion
            }
            else
            {
            }

            return result;
        }
        #endregion
    }
}
