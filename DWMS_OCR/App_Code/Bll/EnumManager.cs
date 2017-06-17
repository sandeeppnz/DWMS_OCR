using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DWMS_OCR.App_Code.Bll
{

    public enum EmailTemplateVariablesEnum
    {
        Remark,
        SystemName,
        Source
    }

    public enum EmailTemplateCodeEnum
    {
        Main_Template,
        Exception_Log_Template,
        Exception_Log_CDB_Verify_Template,
        Exception_Log_CDB_Accept_Template
    }

    public enum leasInterfaceStatusEnum
    {
        PEN
    }

    public enum HleStatusEnum
    {
        Approved,
        Cancelled,
        Complete_Pre_E_Check,
        Expired,
        KIV_CA,
        KIV_Pre_E,
        Pending_Cancellation,
        Pending_Pre_E,
        Pending_Rejection,
        Rejected,
        Route_To_CA_Officer,
    }

    public enum ApplicantTypeEnum
    {
        Applicant,
        Occupier
    }

    public enum WebStringEnum
    {
        Empty
    }

    public enum HleLanesEnum
    {
        A,
        B,
        C,
        E,
        F,
        H,
        L,
        N,
        T,
        X
    }

    public enum CDBVerifyOutputStatus
    {
        A, //accepted (all documents passed by dwms are accepted
        R, //rejecte all the documents
        W //paritially
    }

    public enum CDBAcceptOutputStatus
    {
        A, //accepted (all documents passed by dwms are accepted
        R, //rejecte all the documents
        W //paritially
    }

    public enum PendingAssignmentReportCountEnum
    {
        _5
    }

    public enum ApplicationCancellationOptionEnum
    {
        CustomerRequest,
        Overdue,
        Others,
        None
    }

    public enum DepartmentCodeEnum
    {
        AAD,
        PRD,
        RSD,
        SSD
    }

    public enum OperationTypeEnum
    {
        Insert,
        Update,
        Delete,
        Append,
        Overwrite
    }

    public enum TableNameEnum
    {
        AppDocRef,
        AppPersonal,
        aspnet_Applications,
        aspnet_Membership,
        aspnet_Paths,
        aspnet_PersonalizationAllUsers,
        aspnet_PersonalizationPerUser,
        aspnet_Profile,
        aspnet_Roles,
        aspnet_SchemaVersions,
        aspnet_Users,
        aspnet_UsersInRoles,
        aspnet_WebEvent_Events,
        AccessControl,
        AuditTrail,
        CategorisationRule,
        CategorisationRuleKeyword,
        ScanChannel,
        Department,
        Doc,
        DocApp,
        DocDetail,
        DocMaster,
        DocPersonal,
        DocSet,
        DocType,
        DocumentDetail,
        DocumentMaster,
        EmailTemplate,
        Interface,
        InterfaceSalary,
        InterfaceIncomeComputation,
        LogAction,
        MasterList,
        MasterListItem,
        MetaData,
        MetaField,
        Operation,
        Parameter,
        Personal,
        Profile,
        RawFile,
        RawPage,
        RawText,
        RelevanceRanking,
        ResaleInterface,
        RoleToDepartment,
        SalesInterface,
        Section,
        SersInterface,
        SetApp,
        SetDocRef,
        Street,
        Unit,
        UploadChannel,
        UserGroup,
        SampleDoc,
        SamplePage,
        CreditAssessment        //Added By Edward 25.11.2013 for Leas Service
    }

    public enum AuditTableNameEnum
    {
        AccessControl,
        AuditTrail,
        CategorisationRule,
        CategorisationRuleKeyword,
        ScanChannel,
        DocDetail,
        DocMaster,
        DocType,
        EmailTemplate,
        Parameter,
        Personal,
        Profile,
        RawText,
        UploadChannel
    }

    public enum DownloadStatusEnum
    {
        Pending_Download, // docApp not downloaded by any user
        Downloaded, // docApp downloaded by a user
    }

    public enum CitizenshipEnum
    {
        Others, // 00
        Singapore_Citizen, // 10
        Singapore_Permanent_Resident, // 20
        Malaysian_Citizen // 30
    }

    public enum EmploymentStatusEnum
    {
        Unemployed, // 1
        Employed, // 3
        Self_Employed, // 5
        Employed_opa_Comission_sl_Incentive_Based_cpa_, // 7
        Odd_Job_sl_Part_Time_Worker // 9
    }

    public enum MaritalStatusEnum
    {
        Single, // S
        Single_Orphan, // O
        Married, // M
        Divorced, // D
        Widowed, // W
        Seperated // P
    }

    public enum LogTypeEnum
    {
        D, // used for document log under verification
        S, // used for updates related to set/verification
        A, // used for updates related to application/completeness
        C // used for document log under completeness
    }

    public enum LogActionEnum
    {
        Assigned_set_to_REPLACE1,
        Assigned_application_to_REPLACE1,
        Classified_REPLACE1_in_REPLACE2_as_REPLACE3,
        Confirmed_metadata,
        Confirmed_set,
        Confirmed_application,
        Confirmed_application_COLON_To_Cancel,
        Document_moved_from_REPLACE1_folder_to_REPLACE2_folder,
        Document_merged_from_REPLACE1_to_REPLACE2,
        Document_REPLACE1_metadata_name_changed_from_REPLACE2_to_REPLACE3,
        REPLACE1_from_REPLACE2_merged_with_REPLACE3_from_REPLACE4,
        Recatogorized_set_due_to_reference_no_change,
        REPLACE1_Recatogorized_the_set,
        Reference_No_updated_to_REPLACE1,
        Release_application_from_REPLACE1,
        Release_set_from_REPLACE1,
        REPLACE1_accepted_REPLACE2_at_REPLACE3,
        REPLACE1_rejected_REPLACE2_at_REPLACE3,
        REPLACE1_extracted_as_new_document_REPLACE2_from_REPLACE3,
        REPLACE1_change_section_from_REPLACE2_to_REPLACE3,
        REPLACE1, // used only for formatting
        REPLACE2, // used only for formatting
        REPLACE3, // used only for formatting
        REPLACE4, // used only for formatting
        Route_set,
        Route_document,
        Saved_metadata_as_draft,
        Set_closed,
        None, // used for null reference
        File_Error,
        Thumbnail_Creation_Error,
        REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2,
        REPLACE1_COLON_Message_EQUALSSIGN_REPLACE2_SEMICOLON_File_EQUALSSIGN_REPLACE3,
        REPLACE1_COLON_REPLACE2
    }

    public enum RelationshipEnum
    {
        Self, // 00
        GrandParents, // 01
        Father, // 02
        Uncle_sl_Aunt, // 03
        Cousin, // 05
        Brother_sl_Sister, // 06
        Brother_sl_Sister_in_Law, // 07
        Nephew_sl_Niece, // 08
        Fiance_sl_Fiancee, // 09
        Mother, // 12
        Husband_sl_Wife, // 20
        GrandParents_in_Law, // 21
        Father_in_Law, // 22
        Mother_in_Law, // 32
        Adoptive_Father_sl_Mother, // 38
        Adoptive_Son_sl_Daughter, // 39
        Son_sl_Daughter, // 41
        Son_sl_Daughter_in_Law, // 42
        GrandChild, // 43
        Unrelated, // 47
        Husband, // Used in save metadata function in viewset and viewapp
        Wife // Used in save metadata function in viewset and viewapp
    }

    //public enum RelationshipEnum
    //{
    //    Self, // 00
    //    GrandParents, // 01
    //    Father, // 02
    //    Uncle_sl_Aunt, // 03
    //    Cousin, // 05
    //    Brother_sl_Sister, // 06
    //    Brother_sl_Sister_in_Law, // 07
    //    Nephew_sl_Niece, // 08
    //    Fiance_sl_Fiancee, // 09
    //    Mother, // 12
    //    Husband_sl_Wife, // 20
    //    GrandParents_in_Law, // 21
    //    Father_in_Law, // 22
    //    Mother_in_Law, // 32
    //    Adoptive_Father_sl_Mother, // 38
    //    Adoptive_Son_sl_Daughter, // 39
    //    Son_sl_Daughter, // 41
    //    Son_sl_Daughter_in_Law, // 42
    //    GrandChild, // 43
    //    Unrelated, // 47
    //    Requestor, // Used in save metadata function in viewset and viewapp
    //    Spouse // Used in save metadata function in viewset and viewapp
    //}

    public enum ResaleRelationshipEnum
    {
        Brother_sl_Sister_in_Law, // BI
        Brother_sl_Sister, // BS
        Cousin, // CO
        Daughter, // DA
        Ex_Husband, // EH
        Ex_Wife, // EW
        Father, // FA
        Father_in_Law, // FI
        Fiance_sl_Fiancee, // FN
        Grandparent_in_Law, // GI
        Grandparent, // GR
        Grandson_sl_Grandaughter, //GS
        Husband, // HU
        Mother, //MO
        Nephew_sl_Niece, // NN
        Others, // OT
        Senior_Citizen, // SC
        Self, // SE
        Son_sl_Daughter_in_Law, // SI
        Spouse_of_Joint_Lessee, // SJ
        Spouse_of_Lessee, // SL        
        Step_Mother_sl_Father, // SM
        Son, // SO        
        Second_Wife, // SW
        Uncle_sl_Aunt, // UA
        Wife, // WI
        Spouse_of_Seller, // SS
        Parent, // PR
        Child // CH
    }

    public enum XmlSpecialCharactersEnum
    {
        _amp_
    }

    public enum DateTimeFormat
    {
        yyMMdd,
        yyyyMMdd_dash_HHmmss,
        dd_Hyp_MM_Hyp_yyyy,
        dd_MM_yy,
        dd__MMM__yyyy,
        dd_MM_yyyy,
        dd__MMM__yy,
        dMMMyyyyhmmtt,
        ddd_C_d__MMM__yyyy,
        d__MMM__yyyy_C_h_Col_mm__tt
    }

    public enum SourceFileEnum
    {
        MyDoc,
        MyHDBPage,
        Fax,
        Scan,
        Email,
        WebService,
        MyHDBPage_Common_Panel  //Added By Edward 01.11.2013 011 New Channel
    }

    public enum SystemAccountEnum
    {
        SYSTEM
    }

    public enum MasterListEnum
    {
        Scanning_Channels,
        Uploading_Channels,
        Image_Condition,
        Document_Condition,
        ExpediteReason,
    }

    public enum DocFolderEnum
    {
        Blank,
        Others,
        Routed,
        Spam,
        Unidentified
    }

    public enum ReferenceTypeEnum
    {
        HLE,
        RESALE,
        SALES,
        SERS,
        NRIC,
        Others
    }

    public enum MainFormDocumentIdEnum
    {
        D000094,
        D000095,
        D000096,
        D000097
    }

    public enum ImageConditionEnum
    {
        NA,
        BlurSLASHIncomplete
    }

    public enum AppStatusEnum
    {
        Pending_Documents, // When the application is imported from interface file
        Verified, // when Set is verified, the application is set to Verified.
        Pending_Completeness, // when user is assigned a application
        Completeness_In_Progress, // After user start to save or confirm any document in the application
        Completeness_Cancelled, // when a appplication is cancelled.
        Completeness_Checked // when a appplication is confirmed.
    }

    public enum SetStatusEnum
    {
        Pending_Categorization, // Before being categorized
        Categorization_Failed, // Categorization has failed
        New, // After the set is categorized
        Pending_Verification, // After the set is assigned
        Verification_In_Progress, // After user start to save or confirm any document in the set
        Verified, // when set is confirmed.
        Closed // when a set is closed with no further action.
    }

    public enum SetCompletenessStatusEnum
    {
        Pending_Documents,
        Completeness_Checked,
        Verified,
        Pending_Completeness,
        Completeness_In_Progress
    }


    public enum SendToCDBStatusEnum
    {
        NotReady,
        Ready,
        Sent,
        SentButFailed,
        ModifiedSetSentButFailed,
        ModifiedInCompleteness,
        NA
    }

    public enum SendToCDBStageEnum
    {
        Verified,
        ModifiedVerified,
        Accept
    }

    public enum CompletenessStatusEnum
    {
        Pending_Documents,
        Pending_Completeness, // previous new
        Completeness_In_Progress, // previous Verification_In_Progress
        Completeness_Checked // previous verified
    }

    public enum KeywordVariableEnum
    {
        HLE_Number,
        RESALE_Number,
        SALES_Number,
        SERS_Number,
        NRIC
    }

    public enum PersonalTypeEnum
    {
        OC,
        HA
    }

    public enum ResalePersonalTypeEnum
    {
        SE,
        BU,
        OC,
        MISC
    }

    public enum PersonalTypeTableEnum
    {
        APPPERSONAL,
        DOCPERSONAL
    }

    public enum DocumentConditionEnum
    {
        NA,
        Amended,
        Duplicate
    }

    public enum LicenseofTradeBusinessTypeEnum
    {
        Hawker,
        Taxi
    }

    public enum BusinessTypeEnum
    {
        Partnership,
        Pte_Ltd,
        Sole_Proprietor
    }

    public enum DocTypeEnum
    {
        AdminUndertaking,
        Adoption,
        AHGForm,
        BankStatement,
        Baptism,
        BirthCertificatChild,
        BirthCertificate,
        BusinessProfile,
        CBR,
        CertificateCitizen,
        ChangeCorresAddress,
        ChangeFlatTypeReplac,
        CitizenshipUndertakn,
        COBDeclarationForm,
        CommissionStatement,
        CPFContribution,
        CPFGrant,
        CPFStatement,
        CPFStatementRefund,
        DeathCertificate,
        DeathCertificateEXSP,
        DeathCertificateFa,
        DeathCertificateMo,
        DeathCertificateNRIC,
        DeathCertificateSP,
        DeclaraIncomeDetails,
        DeclaraSingleParent,
        DeclarationMissing,
        DeclarationPrivProp,
        DeedPoll,
        DeedSeparation,
        DeedSeverance,
        DivorceCertChild,
        DivorceCertExSpouse,
        DivorceCertFather,
        DivorceCertificate,
        DivorceCertMother,
        DivorceCertNRIC,
        DivorceDocFinal,
        DivorceDocInitial,
        DivorceDocInterim,
        EmploymentLetter,
        EmploymentPass,
        EnhancedRequest,
        EntryPermit,
        EssentialOccupier,
        FSUndertaking,
        GLA,
        HDBLetter,
        HLE,
        IdentityCard,
        InclusionWithdrawal,
        IncomeUndertaking,
        IRASAssesement,
        IRASIR8E,
        JoinSinglesUndertakn,
        LastWillDeceased,
        LesseeUndertaking,
        LettersolicitorPOA,
        LicenseofTrade,
        LoanStatementSold,
        MarriageCertChild,
        MarriageCertificate,
        MarriageCertLtSpouse,
        MarriageCertParent,
        MarriageConsent,
        MCPSForm,
        MCPSUndertaking,
        Miscellaneous,
        MortgageLoanForm,
        NEligibleUndertaking,
        NoLoanNotification,
        NonCitizPUndertaking,
        NonCitizSUndertaking,
        NoticeofTransfer,
        NSIDcard,
        OfficialAssignee,
        Optionhousingsubsidy,
        OptionResaleLevy,
        OptoutSERS,
        OrderofCourt,
        OrphanScheme,
        OrphanUndertaking,
        OverseasIncome,
        Passport,
        PAYSLIP,
        PensionerLetter,
        PetitionforGLA,
        PowerAttorney,
        PrisonLetter,
        ProcessingFee,
        PropertyQuestionaire,
        PurchaseAgreement,
        QuesFormPrivProperty,
        ReceiptsLoanArrear,
        ReconciliatUndertakn,
        RelinquishUndertakn,
        RentalArrears,
        Resale,
        ResaleChecklistBuy,
        ResaleChecklistSell,
        Sales,
        SERS,
        SERSCompensatioClaim,
        SERSCorrBanks,
        SERSCorrLessees,
        SERSCorrSolicitors,
        SERSHUDCProjects,
        SERSProjecManagement,
        SERSS10Notices,
        SERSS8Notices,
        SERSValuationReport,
        SocialVisit,
        SpouseFormPurchase,
        SpouseFormSale,
        StatementofAccounts,
        StatementSale,
        StatutoryDeclaration,
        StatutoryDeclGeneral,
        StudentPass,
        TenantUndertaking,
        UndertakingBuySERS,
        UndertakingByOrphans,
        UndertakingRelinguis,
        UndertakingRentFlat,
        UndertakingreplFlat,
        UndertakingSERSFlat,
        UndertaknTransSERS,
        UndertaknTrustSERS,
        Unidentified,
        ValuationReport,
        WarranttoAct
    }

    public enum ParameterNameEnum
    {
        //SenderName,
        SystemEmail,
        SystemName,
        OcrEngine,
        ArchiveAudit,
        BatchJobMailingList,
        TestMailingList,
        RedirectAllEmailsToTestMailingList,
        AuthenticationMode,
        MaximumThread,
        MaxSampleDocs,
        MaximumOcrPages,
        MinimumAgeExternalFiles,
        MinimumAgeTempFiles,
        MinimumEnglishWordCount,
        MinimumEnglishWordPercentage,
        MinimumScore,
        MinimumWordLength,
        KeywordCheckScope,
        TopRankedSamplePages,
        OcrBinarize,
        OcrMorph,
        OcrBackgroundFactor,
        OcrForegroundFactor,
        OcrQuality,
        OcrDotMatrix,
        OcrDespeckle,
        HideOcrParameters,
        ErrorNotificationMailingList,
        OcrLastWorking,
        MaxNotifSent,
        MaxTimeOcrNotWorkingTrigger,
        Logging,
        DetailLogging
    }

    public enum SplitTypeEnum
    {
        Group,
        Individual
    }

    public enum ModuleNameEnum
    {
        Import,
        Verification,
        Completeness,
        Income_Computation,
        Maintenance,
        Reports,
        Search,
        FileDoc
    }

    public enum ArchiveAuditEnum
    {
        _1,
        _3,
        _6,
        _12,
        _24
    }



    public enum CategorizationModeEnum
    {
        Relevance_Ranking,
        Keywords
    }

    public enum DocStatusEnum
    {
        New, // initial state
        Pending_Verification,
        Verified, // when user click on confirm in verification module for a given document
        Completed // when user click on confirm in completeness module for a given document
    }

    public enum AuthenticationModeEnum
    {
        Local,
        AD
    }

    public enum UserActiveStatusEnum
    {
        Active,
        Inactive
    }


    public enum SectionBusinessCodeEnum
    {
        CO,
        RO,
        SO,
        SR
    }

    public enum ScanningTransactionTypeEnum
    {
        HLE,
        Sales,
        Resale,
        SERS
    }

    public enum IcNumberPrefixedEnum
    {
        S,
        T,
        F,
        G,
        X
    }


    public enum CategorisationRuleProcessingActionEnum
    {
        Stop,
        Continue
    }


    public enum CategorisationRuleOpeatorEnum
    {
        or,
        and
    }

    //Added By Edward 22.11.2013 LEAS Service
    public enum SendToLEASStatusEnum
    {
        NotReady,
        Ready,
        Sent,
        SentButFailed,
        ModifiedSetSentButFailed,
        ModifiedInCompleteness,
        NA,
        #region Added By Edward Add New LeasStatus 23/2/2014
        FailedConnErr,      
        FailedInpErr,
        FailedOutErr,
        FailedUnknown
        #endregion
    }

    class EnumManager
    {
        /// <summary>
        /// Map the CDB Doc Channel
        /// </summary>
        /// <param name="cdbDocChannel"></param>
        /// <returns></returns>
        public static string MapCdbDocChannel(string cdbDocChannel)
        {
            string result = string.Empty;

            switch (cdbDocChannel)
            {
                case "001":
                    result = CdbDocChannelEnum.MyDoc.ToString();
                    break;
                case "002":
                    result = CdbDocChannelEnum.MyHDBPage.ToString();
                    break;
                case "003":
                    result = CdbDocChannelEnum.Scan.ToString();
                    break;
                case "004":
                    result = CdbDocChannelEnum.Fax.ToString();
                    break;
                case "005":
                    result = CdbDocChannelEnum.Email.ToString();
                    break;
                case "006":
                    result = CdbDocChannelEnum.Deposit_Box.ToString();
                    break;
                case "007":
                    result = CdbDocChannelEnum.Hardcopy_Mail.ToString();
                    break;
                case "008":
                    result = CdbDocChannelEnum.Counter.ToString();
                    break;
                case "009":
                    result = CdbDocChannelEnum.Mixed.ToString();
                    break;
                case "011":     //Added By Edward 31.10.2013 011 New Channel
                    result = CdbDocChannelEnum.MyHDBPage_Common_Panel.ToString();
                    break;
                default:
                    break;
            }

            return result.Replace("_", " ");
        }

        public static string GetMetadataCertDate(string docType)
        {
            string dateField = string.Empty;

            switch (docType)
            {

                case "StudentPass":
                case "IdentityCard":
                case "PetitionforGLA":
                //case "NSIDcard":
                case "GLA":
                    dateField = DocTypeMetaDataNSIDcardEnum.DateOfIssue.ToString();
                    break;

                case "DeathCertificateFa":
                case "DeathCertificateMo":
                case "DeathCertificateSP":
                case "DeathCertificateEXSP":
                case "DeathCertificateNRIC":
                case "DeathCertificate":
                    dateField = DocTypeMetaDataDeathCertificateEnum.DateOfDeath.ToString();
                    break;

                case "DeedPoll":
                    dateField = DocTypeMetaDataDeedPollEnum.DateOfDeedPoll.ToString();
                    break;

                case "PowerAttorney":
                    dateField = DocTypeMetaDataPowerAttorneyEnum.DateOfFiling.ToString();
                    break;

                case "Baptism":
                    dateField = DocTypeMetaDataBaptismEnum.DateOfBaptism.ToString();
                    break;

                case "MarriageCertParent":
                case "MarriageCertLtSpouse":
                case "MarriageCertChild":
                case "MarriageCertificate":
                    dateField = DocTypeMetaDataMarriageCertificateEnum.DateOfMarriage.ToString();
                    break;

                case "DivorceCertFather":
                case "DivorceCertMother":
                case "DivorceCertExSpouse":
                case "DivorceCertChild":
                case "DivorceCertNRIC":
                case "DivorceCertificate":
                    dateField = DocTypeMetaDataDivorceCertificateEnum.DateOfDivorce.ToString();
                    break;

                case "DeedSeparation":
                    dateField = DocTypeMetaDataDeedSeparationEnum.DateOfSeperation.ToString();
                    break;

                case "DeedSeverance":
                    dateField = DocTypeMetaDataDeedSeveranceEnum.DateOfSeverance.ToString();
                    break;


                case "DivorceDocInterim":
                case "DivorceDocInitial":
                    dateField = DocTypeMetaDataDivorceDocInitialEnum.DateOfOrder.ToString();
                    break;

                case "DivorceDocFinal":
                    dateField = DocTypeMetaDataDivorceDocFinalEnum.DateOfFinalJudgement.ToString();
                    break;


                case "IRASAssesement":
                case "IRASIR8E":
                    dateField = DocTypeMetaDataIRASAssesementEnum.DateOfFiling.ToString();
                    break;

                case "CBR":
                case "ValuationReport":
                    dateField = DocTypeMetaDataCBREnum.DateOfReport.ToString();
                    break;
               
         

               
                case "PurchaseAgreement": //not found in matrix
                case "PensionerLetter":
                case "PropertyQuestionaire":
                case "LettersolicitorPOA":
                case "WarranttoAct":
                case "PrisonLetter":
                case "RentalArrears":
                    dateField = DocTypeMetaDataPurchaseAgreementEnum.DateOfDocument.ToString();
                    break;


                case "BusinessProfile":
                    dateField = DocTypeMetaDataBusinessProfileEnum.DateOfRegistration.ToString();
                    break;

                case "LicenseofTrade":
                    dateField = DocTypeMetaDataLicenseofTradeEnum.StartDate.ToString();
                    break;

                case "DeclarationPrivProp":
                case "DeclaraIncomeDetails":
                case "StatutoryDeclGeneral":
                case "ReconciliatUndertakn":
                case "StatutoryDeclaration":
                    dateField = DocTypeMetaDataStatutoryDeclGeneralEnum.DateOfDeclaration.ToString();
                    break;
                

                case "ReceiptsLoanArrear":
                case "LoanStatementSold":
                case "CPFStatementRefund":
                case "StatementSale":
                    dateField = DocTypeMetaDataRentalArrearsEnum.DateOfStatement.ToString();
                    break;

                case "NoticeofTransfer":
                    dateField = DocTypeMetaDataNoticeofTransferEnum.DateOfTransfer.ToString();
                    break;

                case "OrderofCourt":
                    dateField = DocTypeMetaDataOrderofCourtEnum.CourtOrderDate.ToString();
                    break;

                case "HLE":
                case "NoLoanNotification":
                case "MortgageLoanForm":
                    dateField = DocTypeMetaDataMortgageLoanFormEnum.DateOfSignature.ToString();
                    break;

                case "Passport":
                    //dateField = DocTypeMetaDataPassportEnum.DateOfExpiry.ToString();
                    break;

             
                default:
                    break;
            }

            return dateField;
        }

        public static string GetMetadataCertNo(string docType)
        {
            string value = string.Empty;

            switch (docType)
            {
                case "OfficialAssignee":
                    value = DocTypeMetaDataOfficialAssigneeEnum.BankruptcyNo.ToString();
                    break;
                case "BusinessProfile":
                    value = DocTypeMetaDataBusinessProfileEnum.UENNo.ToString();
                    break;
                case "MarriageCertParent":
                case "MarriageCertLtSpouse":
                case "MarriageCertChild":
                case "MarriageCertificate":
                    value = DocTypeMetaDataMarriageCertificateEnum.MarriageCertNo.ToString();
                    break;
                case "RentalArrears":
                    value = DocTypeMetaDataRentalArrearsEnum.HDBRef.ToString();
                    break;
                case "DivorceCertFather":
                case "DivorceCertMother":
                case "DivorceCertExSpouse":
                case "DivorceCertChild":
                case "DivorceCertNRIC":
                case "DivorceDocFinal":
                case "DivorceCertificate":
                    value = DocTypeMetaDataDivorceCertificateEnum.DivorceCaseNo.ToString();
                    break;
                default:
                    break;
            }

            return value;
        }

        public static string GetMetadataLocalForeign(string docType)
        {
            string value = string.Empty;

            switch (docType)
            {
                //case "BirthCertificate":
                //case "BirthCertificatChild":
                case "DeathCertificate":
                case "DeathCertificateFa":
                case "DeathCertificateMo":
                case "DeathCertificateSP":
                case "DeathCertificateEXSP":
                case "DeathCertificateNRIC":
                //case "Adoption":
                    value = DocTypeMetaDataBirthCertificateEnum.Tag.ToString();
                    break;

                case "MarriageCertParent":
                case "MarriageCertLtSpouse":
                case "MarriageCertChild":
                case "MarriageCertificate":
                    value = DocTypeMetaDataMarriageCertificateEnum.Tag.ToString();
                    break;

                case "DivorceCertFather":
                case "DivorceCertMother":
                case "DivorceCertExSpouse":
                case "DivorceCertChild":
                case "DivorceCertNRIC":
                case "DivorceCertificate":
                    value = DocTypeMetaDataDivorceCertificateEnum.Tag.ToString();
                    break;

                case "DeedSeparation":
                    value = DocTypeMetaDataDeedSeparationEnum.Tag.ToString();
                    break;

                case "DeedSeverance":
                    value = DocTypeMetaDataDeedSeveranceEnum.Tag.ToString();
                    break;

                case "DivorceDocInterim":
                case "DivorceDocInitial":
                    value = DocTypeMetaDataDivorceDocInitialEnum.Tag.ToString();
                    break;

                case "DivorceDocFinal":
                    value = DocTypeMetaDataDivorceDocFinalEnum.Tag.ToString();
                    break;

                default:
                    break;
            }

            return value;
        }

        public static string GetMetadataMarriageType(string docType)
        {
            string value = string.Empty;

            //Notes: the rest of other document types not here are considered to return FALSE

            switch (docType)
            {
            
                case "MarriageCertParent":
                case "MarriageCertLtSpouse":
                case "MarriageCertChild":
                case "MarriageCertificate":
                    value = DocTypeMetaDataMarriageCertificateEnum.Tag.ToString();
                    break;

                case "DivorceCertFather":
                case "DivorceCertMother":
                case "DivorceCertExSpouse":
                case "DivorceCertChild":
                case "DivorceCertNRIC":
                case "DivorceCertificate":
                case "DivorceDocInterim":
                case "DivorceDocInitial":
                case "DivorceDocFinal":
                case "DeedSeparation":
                case "DeedSeverance":
                    value = DocTypeMetaDataBirthCertificateEnum.Tag.ToString();
                    break;

                default:
                    break;
            }

            return value;
        }

        public static string GetMetadataStartDate(string docType)
        {

            //docType is the "Code" field in DocType

            string dateField = string.Empty;

            switch (docType)
            {
                case "PAYSLIP":
                case "CommissionStatement":
                case "EmploymentLetter":
                case "BankStatement":
                case "CPFContribution":
                case "StatementofAccounts":
                case "PensionerLetter":
                case "OverseasIncome":
                case "CPFStatement":
                case "StatutoryDeclaration":
                    dateField = DocTypeMetaDataPAYSLIPEnum.StartDate.ToString();
                    break;

                case "IRASAssesement":
                case "IRASIR8E":
                    dateField = DocTypeMetaDataIRASAssesementEnum.YearOfAssessment.ToString();
                    break;
                default:
                    break;
            }

            return dateField;
        }

        public static string GetMetadataEndDate(string docType)
        {
            string dateField = string.Empty;

            switch (docType)
            {

                case "Passport":
                case "EntryPermit":
                case "EmploymentPass":
                case "SocialVisit":
                    dateField = DocTypeMetaDataPassportEnum.DateOfExpiry.ToString();
                    break;
                case "PAYSLIP":
                case "CommissionStatement":
                //case "EmploymentLetter":
                case "BankStatement":
                case "CPFContribution":
                case "StatementofAccounts":
                case "PensionerLetter":
                case "OverseasIncome":
                case "CPFStatement":
                case "StatutoryDeclaration":
                    dateField = DocTypeMetaDataPAYSLIPEnum.EndDate.ToString();
                    break;

                default:
                    break;
            }

            return dateField;
        }

        //IdentityNoSub
        public static string GetMetadataIdentityNoSub(string docType)
        {
            string value = string.Empty;

            switch (docType)
            {
                case "BirthCertificatChild":
                    value = DocTypeMetaDataBirthCertificatChildEnum.IdentityNo.ToString();
                    break;
                case "SpouseFormPurchase":
                    value = DocTypeMetaDataSpouseFormPurchaseEnum.SpouseID.ToString();
                    break;
                case "PetitionforGLA":
                    value = DocTypeMetaDataSpouseFormPurchaseEnum.SpouseID.ToString();
                    break;

                case "DeathCertificate":
                    value = DocTypeMetaDataDeathCertificateEnum.IdentityNo.ToString();
                    break;
                case "DeathCertificateFa":
                    value = DocTypeMetaDataDeathCertificateFaEnum.IdentityNoOfFather.ToString();
                    break;
                case "DeathCertificateMo":
                    value = DocTypeMetaDataDeathCertificateMoEnum.IdentityNoOfMother.ToString();
                    break;
                case "DeathCertificateSP":
                    value = DocTypeMetaDataDeathCertificateSPEnum.IdentityNoOfSpouse.ToString();
                    break;
                case "DeathCertificateEXSP":
                    value = DocTypeMetaDataDeathCertificateEXSPEnum.IdentityNoOfEXSpouse.ToString();
                    break;
                case "DeathCertificateNRIC":
                    value = DocTypeMetaDataDeathCertificateNRICEnum.IdentityNoNRIC.ToString();
                    break;


                case "MarriageCertificate":
                    value = DocTypeMetaDataMarriageCertificateEnum.IdentityNoRequestor.ToString();
                    break;
                case "MarriageCertParent":
                     value = DocTypeMetaDataMarriageCertParentEnum.IdentityNoParent.ToString();
                    break;
                case "MarriageCertLtSpouse":
                     value = DocTypeMetaDataMarriageCertLtSpouseEnum.IdentityNoRequestor.ToString();
                    break;
                case "MarriageCertChild":
                     value = DocTypeMetaDataMarriageCertChildEnum.IdentityNoChild.ToString();
                    break;


                case "DivorceCertFather":
                     value = DocTypeMetaDataDivorceCertFatherEnum.IdentityNoFather.ToString();
                    break;

                case "DivorceCertMother":
                     value = DocTypeMetaDataDivorceCertMotherEnum.IdentityNoMother.ToString();
                    break;

                case "DivorceCertExSpouse":
                     value = DocTypeMetaDataDivorceCertExSpouseEnum.IdentityNoRequestor.ToString();
                    break;

                case "DivorceCertChild":
                     value = DocTypeMetaDataDivorceCertChildEnum.IdentityNoChild.ToString();
                    break;

                case "DivorceCertNRIC":
                     value = DocTypeMetaDataDivorceCertNRICEnum.IdentityNoNRIC.ToString();
                    break;

                case "DivorceCertificate":
                     value = DocTypeMetaDataDivorceCertificateEnum.IdentityNoRequestor.ToString();
                    break;


                case "DivorceDocInterim":
                     value = DocTypeMetaDataDivorceDocInterimEnum.IdentityNoRequestor.ToString();
                    break;

                case "DivorceDocInitial":
                     value = DocTypeMetaDataDivorceDocInitialEnum.IdentityNoRequestor.ToString();
                    break;

                case "DivorceDocFinal":
                     value = DocTypeMetaDataDivorceDocFinalEnum.IdentityNoRequestor.ToString();
                    break;

                case "DeedSeparation":
                     value = DocTypeMetaDataDeedSeparationEnum.IdentityNoRequestor.ToString();
                    break;

                case "DeedSeverance":
                    value = DocTypeMetaDataDeedSeveranceEnum.IdentityNoRequestor.ToString();
                    break;
                default:
                    break;

            }

            return value;
        }


        public static string GetMetadataIdentityNoSubIDType(string docType)
        {
            string value = string.Empty;

            switch (docType)
            {
                case "BirthCertificatChild":
                case "SpouseFormPurchase":
                case "PetitionforGLA":
                case "DeathCertificate":
                case "DeathCertificateFa":
                case "DeathCertificateMo":
                case "DeathCertificateSP":
                case "DeathCertificateEXSP":
                case "DeathCertificateNRIC":
                    value = DocTypeMetaDataDeathCertificateNRICEnum.IDType.ToString();
                    break;


                case "MarriageCertificate":
                    value = DocTypeMetaDataMarriageCertificateEnum.IDTypeRequestor.ToString();
                    break;
                case "MarriageCertParent":
                    value = DocTypeMetaDataMarriageCertParentEnum.IDTypeParent.ToString();
                    break;
                case "MarriageCertLtSpouse":
                    value = DocTypeMetaDataMarriageCertLtSpouseEnum.IDTypeRequestor.ToString();
                    break;
                case "MarriageCertChild":
                    value = DocTypeMetaDataMarriageCertChildEnum.IDTypeChild.ToString();
                    break;


                case "DivorceCertFather":
                    value = DocTypeMetaDataDivorceCertFatherEnum.IDTypeFather.ToString();
                    break;

                case "DivorceCertMother":
                    value = DocTypeMetaDataDivorceCertMotherEnum.IDTypeMother.ToString();
                    break;

                case "DivorceCertExSpouse":
                    value = DocTypeMetaDataDivorceCertExSpouseEnum.IDTypeExSpouse.ToString();
                    break;

                case "DivorceCertChild":
                    value = DocTypeMetaDataDivorceCertChildEnum.IDTypeChild.ToString();
                    break;

                case "DivorceCertNRIC":
                    value = DocTypeMetaDataDivorceCertNRICEnum.IDTypeNRIC.ToString();
                    break;

                case "DivorceCertificate":
                    value = DocTypeMetaDataDivorceCertificateEnum.IDTypeRequestor.ToString();
                    break;


                case "DivorceDocInterim":
                    value = DocTypeMetaDataDivorceDocInterimEnum.IDTypeRequestor.ToString();
                    break;

                case "DivorceDocInitial":
                    value = DocTypeMetaDataDivorceDocInitialEnum.IDTypeRequestor.ToString();
                    break;

                case "DivorceDocFinal":
                    value = DocTypeMetaDataDivorceDocFinalEnum.IDTypeRequestor.ToString();
                    break;

                case "DeedSeparation":
                    value = DocTypeMetaDataDeedSeparationEnum.IDTypeRequestor.ToString();
                    break;

                case "DeedSeverance":
                    value = DocTypeMetaDataDeedSeveranceEnum.IDTypeRequestor.ToString();
                    break;
                default:
                    break;

            }

            return value;
        }

    }




    public enum CdbDocChannelEnum
    {
        MyDoc,
        MyHDBPage,
        Scan,
        Fax,
        Email,
        Deposit_Box,
        Hardcopy_Mail,
        Counter,
        Mixed,
        CDB,
        MyHDBPage_Common_Panel      //Added By Edward 31.10.2013 011 New Channel
    }


    public enum ErrorLogFunctionName
    {
        MergePDFDocument,
        UnableToOpenPDFDocument
    }




    #region DOCTYPE METADATA FIELD ENUM




    public enum DocTypeMetaDataBirthCertificateEnum
    {
        Tag
    }

    public enum DocTypeMetaDataBirthCertificatChildEnum
    {
        Tag,
        IDType,
        IdentityNo,
        NameOfChild
    }

    public enum DocTypeMetaDataAdoptionEnum
    {
        Tag,
        IDType,
        NameOfChild
    }

    public enum DocTypeMetaDataDeedPollEnum
    {
        DateOfDeedPoll
    }

    public enum DocTypeMetaDataPowerAttorneyEnum
    {
        DateOfFiling,
        IdentityNoDonor1,
        IdentityNoDonor2,
        IdentityNoDonor3,
        IdentityNoDonor4,
        IDTypeDonor1,
        IDTypeDonor2,
        IDTypeDonor3,
        IDTypeDonor4,
        NameDonor1,
        NameDonor2,
        NameDonor3,
        NameDonor4
    }

    public enum DocTypeMetaDataCBREnum
    {
        DateOfReport
    }

    public enum DocTypeMetaDataIdentityCardEnum
    {
        DateOfIssue,
        Address
    }

    public enum DocTypeMetaDataNSIDcardEnum
    {
        DateOfIssue
    }

    public enum DocTypeMetaDataPassportEnum
    {
        DateOfExpiry
    }

    public enum DocTypeMetaDataStudentPassEnum
    {
        EducationLevel,
        DateOfIssue
    }

    public enum DocTypeMetaDataValueStudentPassEnum
    {
        Primary,
        Secondary,
        Tertiary
    }

    public enum DocTypeMetaDataSpouseFormPurchaseEnum
    {
        IDType,
        SpouseName,
        SpouseID
    }

    public enum DocTypeMetaDataPurchaseAgreementEnum
    {
        DateOfDocument,
    }


    public enum DocTypeMetaDataValuationReportEnum
    {
        DateOfReport,
    }

    public enum DocTypeMetaDataReceiptsLoanArrearEnum
    {
        HDBRef,
        DateOfStatement
    }

    public enum DocTypeMetaDataRentalArrearsEnum
    {
        HDBRef,
        DateOfStatement
    }

    public enum DocTypeMetaDataPetitionforGLAEnum
    {
        DateOfIssue,
        IdNo,
        IDType,
        NameDeceased
    }

    public enum DocTypeMetaDataOrderofCourtEnum
    {
        CourtOrderDate
    }

    public enum DocTypeMetaDataOfficialAssigneeEnum
    {
        BankruptcyNo
    }

    public enum DocTypeMetaDataBaptismEnum
    {
        DateOfBaptism
    }


    public enum DocTypeMetaDataNoticeofTransferEnum
    {
        DateOfTransfer
    }

    public enum DocTypeMetaDataHleEnum
    {
        DateOfSignature
    }

    public enum DocTypeMetaDataLicenseofTradeEnum
    {
        StartDate, // this is IssueDate
        BusinessType
    }

    public enum DocTypeMetaDataWarranttoActEnum
    {
        DateOfDocument
    }

    public enum DocTypeMetaDataLoanStatementSoldEnum
    {
        DateOfStatement
    }


    public enum DocTypeMetaDataCPFStatementRefundEnum
    {
        DateOfStatement,
        CR
    }

    public enum DocTypeMetaDataStatementSaleEnum
    {
        DateOfStatement
    }

    public enum DocTypeMetaDataPropertyQuestionaireEnum
    {
        DateOfDocument
    }


    public enum DocTypeMetaDataDeclaraIncomeDetailsEnum
    {
        DateOfDeclaration
    }

    public enum DocTypeMetaDataLettersolicitorPOAEnum
    {
        DateOfDocument
    }


    public enum DocTypeMetaDataCPFContributionEnum
    {
        StartDate,
        EndDate,
        ConsistentContribution,
        CompanyName1,
        CompanyName2
    }

    public enum DocTypeMetaDataMortgageLoanFormEnum
    {
        DateOfSignature
    }

    public enum DocTypeMetaDataStatutoryDeclarationEnum
    {
        StartDate,
        EndDate,
        DateOfDocument,
        Type
    }

    public enum DocTypeMetaDataStatutoryDeclGeneralEnum
    {
        DateOfDeclaration,
    }

    public enum DocTypeMetaDataDeclarationPrivPropEnum
    {
        DateOfDeclaration,
    }


    public enum DocTypeMetaDataPAYSLIPEnum
    {
        StartDate,
        EndDate,
        NameOfCompany,
        Allowance,
    }

    public enum DocTypeMetaDataOverseasIncomeEnum
    {
        StartDate,
        EndDate,
    }

    public enum DocTypeMetaDataStatementOfAccountsEnum
    {
        StartDate,
        EndDate,
        NameOfCompany,
    }

    public enum DocTypeMetaDataPensionerLetterEnum
    {
        DateOfDocument
    }

    public enum DocTypeMetaDataBusinessProfileEnum
    {
        UENNo,
        DateOfRegistration,
        BusinessType
    }

    public enum DocTypeMetaDataBankStatementEnum
    {
        StartDate,
        EndDate,
    }

    public enum DocTypeMetaDataValueStatutoryDeclarationEnum
    {
        Self_employed,
        Unemployed
    }

    public enum DocTypeMetaDataValueYesNoEnum
    {
        Yes,
        No
    }


    public enum DocTypeMetaDataValueCPFEnum
    {
        Yes,
        No
    }

    public enum DocTypeMetaDataCPFStatementEnum
    {
        StartDate,
        EndDate
    }

    public enum DocTypeMetaDataDeathCertificateEnum
    {
        IdentityNo,
        DateOfDeath,
        Tag
    }

    public enum DocTypeMetaDataDeathCertificateFaEnum
    {
        DateOfDeath,
        Tag,
        IDType,
        NameOfFather,
        IdentityNoOfFather
    }

    public enum DocTypeMetaDataDeathCertificateMoEnum
    {
        DateOfDeath,
        Tag,
        IDType,
        NameOfMother,
        IdentityNoOfMother
    }

    public enum DocTypeMetaDataDeathCertificateSPEnum
    {
        DateOfDeath,
        Tag,
        IDType,
        NameOfSpouse,
        IdentityNoOfSpouse
    }

    public enum DocTypeMetaDataDeathCertificateEXSPEnum
    {
        DateOfDeath,
        Tag,
        IDType,
        NameOfEXSpouse,
        IdentityNoOfEXSpouse
    }

    public enum DocTypeMetaDataDeathCertificateNRICEnum
    {
        DateOfDeath,
        Tag,
        IDType,
        NameNRIC,
        IdentityNoNRIC
    }

    public enum DocTypeMetaDataNoLoanNotificationEnum
    {
        DateOfSignature,
        Type
    }

    public enum DocTypeMetaDataValueNoLoanNotificationEnum
    {
        Loan,
        Bank_Loan,
        No_Loan
    }

    public enum DocTypeMetaDataIRASAssesementEnum
    {
        DateOfFiling,
        TypeOfIncome,
        YearOfAssessment
    }

    public enum DocTypeMetaDataValueIRASAssesementEnum
    {
        Trade,
        Employment,
        Both
    }

    public enum DocTypeMetaDataIRASIR8EEnum
    {
        DateOfFiling,
        TypeOfIncome,
        YearOfAssessment
    }

    public enum DocTypeMetaDataValueIRASIR8EEnum
    {
        Trade,
        Employment,
        Both
    }



  
#region  Marriage Doc Type Enum

    public enum DocTypeMetaDataMarriageCertificateEnum
    {
        DateOfMarriage,
        MarriageCertNo,
        Tag,
        IdentityNoRequestor,
        IdentityNoImageRequestor,
        IdentityNoSpouse,
        IdentityNoImageSpouse,
        IDTypeRequestor,
        IDTypeImageRequestor,
        IDTypeSpouse,
        IDTypeImageSpouse,
        NameOfRequestor,
        NameOfSpouse
    }

    public enum DocTypeMetaDataMarriageCertParentEnum
    {
        DateOfMarriage,
        MarriageCertNo,
        Tag,
        IdentityNoParent,
        IdentityNoSpouse,
        IDTypeParent,
        IDTypeSpouse,
        NameOfParent,
        NameOfSpouse
    }

    //2013-02-04
    public enum DocTypeMetaDataMarriageCertLtSpouseEnum
    {
        DateOfMarriage,
        MarriageCertNo,
        Tag,
        IdentityNoRequestor,
        IdentityNoImageRequestor,
        IdentityNoLateSpouse,
        IDTypeRequestor,
        IDTypeImageRequestor,
        IDTypeLateSpouse,
        NameOfRequestor,
        NameOfLateSpouse
    }

    public enum DocTypeMetaDataMarriageCertChildEnum
    {
        DateOfMarriage,
        MarriageCertNo,
        Tag,
        IdentityNoChild,
        IdentityNoSpouse,
        IDTypeChild,
        IDTypeSpouse,
        NameOfChild,
        NameOfSpouse
    }

#endregion Marriage Doc type





    public enum DocTypeMetaDataDeedSeparationEnum
    {
        Tag,
        DateOfSeperation,
        IDTypeRequestor,
        IDTypeSpouse,
        IdentityNoRequestor,
        IdentityNoSpouse,
        NameOfRequestor,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDeedSeveranceEnum
    {
        Tag,
        DateOfSeverance,
        IDTypeRequestor,
        IDTypeSpouse,
        IdentityNoRequestor,
        IdentityNoSpouse,
        NameOfRequestor,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDivorceCertificateEnum
    {
        DateOfDivorce,
        DivorceCaseNo,
        Tag,
        IdentityNoRequestor,
        IdentityNoSpouse,
        IDTypeRequestor,
        IDTypeSpouse,
        NameOfRequestor,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDivorceCertFatherEnum
    {
        DateOfDivorce,
        DivorceCaseNo,
        Tag,
        IdentityNoFather,
        IdentityNoSpouse,
        IDTypeFather,
        IDTypeSpouse,
        NameOfFather,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDivorceCertMotherEnum
    {
        DateOfDivorce,
        DivorceCaseNo,
        Tag,
        IdentityNoMother,
        IdentityNoSpouse,
        IDTypeMother,
        IDTypeSpouse,
        NameOfMother,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDivorceCertExSpouseEnum
    {
        DateOfDivorce,
        DivorceCaseNo,
        Tag,
        IdentityNoRequestor,
        IdentityNoExSpouse,
        IDTypeRequestor,
        IDTypeExSpouse,
        NameOfRequestor,
        NameOfExSpouse
    }

    public enum DocTypeMetaDataDivorceCertChildEnum
    {
        DateOfDivorce,
        DivorceCaseNo,
        Tag,
        IdentityNoChild,
        IdentityNoSpouse,
        IDTypeChild,
        IDTypeSpouse,
        NameOfChild,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDivorceCertNRICEnum
    {
        DateOfDivorce,
        DivorceCaseNo,
        Tag,
        IdentityNoNRIC,
        IdentityNoSpouse,
        IDTypeNRIC,
        IDTypeSpouse,
        NameOfNRIC,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDivorceDocInitialEnum
    {
        DateOfOrder,
        Tag,
        IdentityNoRequestor,
        IdentityNoSpouse,
        IDTypeRequestor,
        IDTypeSpouse,
        NameOfRequestor,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDivorceDocInterimEnum
    {
        DateOfOrder,
        Tag,
        IdentityNoRequestor,
        IdentityNoSpouse,
        IDTypeRequestor,
        IDTypeSpouse,
        NameOfRequestor,
        NameOfSpouse
    }

    public enum DocTypeMetaDataDivorceDocFinalEnum
    {
        DateOfFinalJudgement,
        DivorceCaseNo,
        Tag,
        IdentityNoRequestor,
        IdentityNoSpouse,
        IDTypeRequestor,
        IDTypeSpouse,
        NameOfRequestor,
        NameOfSpouse
    }






    public enum CustomerTypeEnum
    {
        P,
        E
    }

    public enum IDTypeEnum
    {
        UIN,
        FIN,
        XIN
    }

    public enum TagEnum
    {
        Local_Muslim,
        Local_Civil,
        Foreign
    }




    public enum TagGeneralEnum
    {
        Local,
        Foreign
    }

    #endregion


  

   

}
