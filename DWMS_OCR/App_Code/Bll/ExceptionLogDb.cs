using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.ExceptionLogTableAdapters;
using DWMS_OCR.App_Code.Dal;
using DWMS_OCR.App_Code.Helper;
using System.Diagnostics;

namespace DWMS_OCR.App_Code.Bll
{
    class ExceptionLogDb
    {
        private ExceptionLogTableAdapter _ExceptionLogTableAdapter = null;

        protected ExceptionLogTableAdapter Adapter
        {
            get
            {
                if (_ExceptionLogTableAdapter == null)
                    _ExceptionLogTableAdapter = new ExceptionLogTableAdapter();

                return _ExceptionLogTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public ExceptionLog.ExceptionLogDataTable GetExceptionLogs()
        {
            return Adapter.GetData();
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="message"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public int Insert(string channel, string refNo, DateTime date, string reason, string errorMessage, string sourceName, bool sendEmail)
        {
            ExceptionLog.ExceptionLogDataTable dt = new ExceptionLog.ExceptionLogDataTable();
            ExceptionLog.ExceptionLogRow r = dt.NewExceptionLogRow();

            r.Channel = channel;
            r.RefNo = refNo;
            r.DateOccurred = date;
            r.Reason = reason;
            r.ErrorMessage = errorMessage;

            dt.AddExceptionLogRow(r);
            Adapter.Update(dt);
            int rowAffected = r.Id;

            if (rowAffected > 0)
            {
                ParameterDb parameterDb = new ParameterDb();                

                // Send email notifications
                string recipient = string.Empty;
                string subject = string.Empty;
                string message = string.Empty;

                string systemEmail = parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail);
                string systemName = parameterDb.GetParameterValue(ParameterNameEnum.SystemName);

                if (!String.IsNullOrEmpty(refNo))
                {
                    string refType = Util.GetReferenceType(refNo);
                    
                    string departmentCode = string.Empty;

                    if (refType.Equals(ReferenceTypeEnum.HLE.ToString()))
                        departmentCode = DepartmentCodeEnum.AAD.ToString();
                    else if (refType.Equals(ReferenceTypeEnum.RESALE.ToString()))
                        departmentCode = DepartmentCodeEnum.RSD.ToString();
                    else if (refType.Equals(ReferenceTypeEnum.SALES.ToString()))
                        departmentCode = DepartmentCodeEnum.SSD.ToString();
                    else if (refType.Equals(ReferenceTypeEnum.SERS.ToString()))
                        departmentCode = DepartmentCodeEnum.PRD.ToString();

                    if (!String.IsNullOrEmpty(departmentCode))
                    {
                        DepartmentDb departmentDb = new DepartmentDb();
                        recipient = departmentDb.GetDepartmentMailingList((DepartmentCodeEnum)Enum.Parse(typeof(DepartmentCodeEnum), departmentCode));
                    }
                    else
                    {
                        recipient = parameterDb.GetParameterValue(ParameterNameEnum.ErrorNotificationMailingList);
                    }
                }
                else
                {
                    recipient = parameterDb.GetParameterValue(ParameterNameEnum.ErrorNotificationMailingList);
                }

                if (!String.IsNullOrEmpty(recipient) && sendEmail)
                {
                    // Compose the email subject and body
                    EmailTemplateDb emailTemplateDb = new EmailTemplateDb();
                    EmailTemplate.EmailTemplateDataTable emailTemplateDt = emailTemplateDb.GetEmailTemplateByCode(EmailTemplateCodeEnum.Exception_Log_Template.ToString());

                    if (emailTemplateDt.Rows.Count > 0)
                    {
                        string content = string.Empty;
                        content += "Error:" + Environment.NewLine;
                        content += reason + Environment.NewLine + Environment.NewLine;
                        content += "Details:" + Environment.NewLine;
                        content += errorMessage + Environment.NewLine;

                        EmailTemplate.EmailTemplateRow emailTemplate = emailTemplateDt[0];
                        subject = emailTemplate.Subject.Replace("[" + EmailTemplateVariablesEnum.Source.ToString() + "]", sourceName);
                        message = emailTemplate.Content;
                        message = message.Replace("[" + EmailTemplateVariablesEnum.Remark.ToString() + "]", content);
                        message = message.Replace("[" + EmailTemplateVariablesEnum.SystemName.ToString() + "]", systemName);
                    }

                    Util.SendMail(systemName, systemEmail, recipient, string.Empty, string.Empty, subject, message);
                }
            }

            return rowAffected;
        }

        public void LogException(int? setId, string channel, string refNo, string source, 
            string errorReason, string errorException, bool sendEmail)
        {
            string channelFinal = string.Empty;
            string refNoFinal = string.Empty;
            string sourceFinal = string.Empty;

            if (setId.HasValue)
            {
                sourceFinal = setId.Value.ToString();

                DocSetDb docSetDb = new DocSetDb();
                DocSet.vDocSetDataTable vDocSetTable = docSetDb.GetvDocSetById(setId.Value);

                if (vDocSetTable.Rows.Count > 0)
                {
                    DocSet.vDocSetRow vDocSet = vDocSetTable[0];

                    channelFinal = (vDocSet.IsChannelNull() ? string.Empty : vDocSet.Channel);
                    refNoFinal = (vDocSet.IsRefNoNull() ? string.Empty : vDocSet.RefNo);
                }
            }
            else
            {
                channelFinal = channel;
                refNoFinal = refNo;
                sourceFinal = source;
            }

            Insert(channelFinal, refNoFinal, DateTime.Now, errorReason, errorException, sourceFinal, sendEmail);
        }

        public void LogCDBException(int? setId, string channel, string refNo, string source,
            string errorReason, string errorException, bool sendEmail, SendToCDBStageEnum stage)
        {
            string channelFinal = string.Empty;
            string refNoFinal = string.Empty;
            string sourceFinal = string.Empty;

            //Util.CDBLog(string.Empty, "Log exception" + errorReason + errorException, EventLogEntryType.Warning);
            if (setId.HasValue)
            {
                sourceFinal = setId.Value.ToString();

                DocSetDb docSetDb = new DocSetDb();
                DocSet.vDocSetDataTable vDocSetTable = docSetDb.GetvDocSetById(setId.Value);

                if (vDocSetTable.Rows.Count > 0)
                {
                    DocSet.vDocSetRow vDocSet = vDocSetTable[0];

                    channelFinal = (vDocSet.IsChannelNull() ? string.Empty : vDocSet.Channel);
                    refNoFinal = (vDocSet.IsRefNoNull() ? string.Empty : vDocSet.RefNo);
                }
            }
            else
            {
                channelFinal = channel;
                refNoFinal = refNo;
                sourceFinal = source;
            }

            CDBInsert(channelFinal, refNoFinal, DateTime.Now, errorReason, errorException, sourceFinal, sendEmail, stage);
        }

        public int CDBInsert(string channel, string refNo, DateTime date, string reason, string errorMessage, string sourceName, bool sendEmail, SendToCDBStageEnum stage)
        {
            ExceptionLog.ExceptionLogDataTable dt = new ExceptionLog.ExceptionLogDataTable();
            ExceptionLog.ExceptionLogRow r = dt.NewExceptionLogRow();

            r.Channel = channel;
            r.RefNo = refNo;
            r.DateOccurred = date;
            r.Reason = reason;
            r.ErrorMessage = errorMessage;

            dt.AddExceptionLogRow(r);
            Adapter.Update(dt);
            int rowAffected = r.Id;

            #region Email
            //Util.CDBLog(string.Empty, "Send exception", EventLogEntryType.Warning);
            if (rowAffected > 0)
            {
                ParameterDb parameterDb = new ParameterDb();

                // Send email notifications
                string recipient = string.Empty;
                string subject = string.Empty;
                string message = string.Empty;

                string systemEmail = parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail);
                string systemName = parameterDb.GetParameterValue(ParameterNameEnum.SystemName);

                //if (!String.IsNullOrEmpty(refNo))
                //{
                //    string refType = Util.GetReferenceType(refNo);

                //    string departmentCode = string.Empty;

                //    if (refType.Equals(ReferenceTypeEnum.HLE.ToString()))
                //        departmentCode = DepartmentCodeEnum.AAD.ToString();
                //    else if (refType.Equals(ReferenceTypeEnum.RESALE.ToString()))
                //        departmentCode = DepartmentCodeEnum.RSD.ToString();
                //    else if (refType.Equals(ReferenceTypeEnum.SALES.ToString()))
                //        departmentCode = DepartmentCodeEnum.SSD.ToString();
                //    else if (refType.Equals(ReferenceTypeEnum.SERS.ToString()))
                //        departmentCode = DepartmentCodeEnum.PRD.ToString();

                //    if (!String.IsNullOrEmpty(departmentCode))
                //    {
                //        DepartmentDb departmentDb = new DepartmentDb();
                //        recipient = departmentDb.GetDepartmentMailingList((DepartmentCodeEnum)Enum.Parse(typeof(DepartmentCodeEnum), departmentCode));
                //    }
                //    else
                //    {
                //        recipient = parameterDb.GetParameterValue(ParameterNameEnum.ErrorNotificationMailingList);
                //    }
                //}
                //else
                //{
                    recipient = parameterDb.GetParameterValue(ParameterNameEnum.TestMailingList);
                //}

                if (!String.IsNullOrEmpty(recipient) && sendEmail)
                {
                    // Compose the email subject and body
                    EmailTemplateDb emailTemplateDb = new EmailTemplateDb();
                    EmailTemplate.EmailTemplateDataTable emailTemplateDt = new EmailTemplate.EmailTemplateDataTable();

                    if (stage.ToString() == SendToCDBStageEnum.Verified.ToString() || stage.ToString() == SendToCDBStageEnum.ModifiedVerified.ToString())
                    {
                        emailTemplateDt = emailTemplateDb.GetEmailTemplateByCode(EmailTemplateCodeEnum.Exception_Log_CDB_Verify_Template.ToString());
                    }
                    else if (stage.ToString() == SendToCDBStageEnum.Accept.ToString())
                    {
                        emailTemplateDt = emailTemplateDb.GetEmailTemplateByCode(EmailTemplateCodeEnum.Exception_Log_CDB_Accept_Template.ToString());
                    }


                    if (emailTemplateDt.Rows.Count > 0)
                    {
                        string content = string.Empty;
                        content += "Ref no.:" + refNo + " (" + sourceName + ")" + Environment.NewLine + Environment.NewLine;
                        content += "Error:" + Environment.NewLine;
                        content += reason + Environment.NewLine + Environment.NewLine;
                        content += "Details:" + Environment.NewLine;
                        content += errorMessage + Environment.NewLine;

                        EmailTemplate.EmailTemplateRow emailTemplate = emailTemplateDt[0];
                        subject = emailTemplate.Subject.Replace("[" + EmailTemplateVariablesEnum.Source.ToString() + "]", refNo);
                        message = emailTemplate.Content;
                        message = message.Replace("[" + EmailTemplateVariablesEnum.Remark.ToString() + "]", content);
                        message = message.Replace("[" + EmailTemplateVariablesEnum.SystemName.ToString() + "]", systemName);
                    }

                    Util.CDBLog(string.Empty, message, EventLogEntryType.Error);
                    Util.SendMail(systemName, systemEmail, recipient, string.Empty, string.Empty, subject, message);
                    //Util.CDBLog(string.Empty, "test", EventLogEntryType.Error);
                }
            }
            #endregion
            return rowAffected;
        }

        #region Added By Edward Leas Service 11/12/2013  

        public int LeasExceptionLogInsert(string channel, string refNo, DateTime date, string reason, string errorMessage)
        {
            ExceptionLog.ExceptionLogDataTable dt = new ExceptionLog.ExceptionLogDataTable();
            ExceptionLog.ExceptionLogRow r = dt.NewExceptionLogRow();

            r.Channel = channel;
            r.RefNo = refNo;
            r.DateOccurred = date;
            r.Reason = reason;
            r.ErrorMessage = errorMessage;

            dt.AddExceptionLogRow(r);
            Adapter.Update(dt);
            int rowAffected = r.Id;

            #region Email
            //if (rowAffected > 0)
            //{
            //    ParameterDb parameterDb = new ParameterDb();

            //    // Send email notifications
            //    string recipient = string.Empty;
            //    string subject = string.Empty;
            //    string message = string.Empty;

            //    string systemEmail = parameterDb.GetParameterValue(ParameterNameEnum.SystemEmail);
            //    string systemName = parameterDb.GetParameterValue(ParameterNameEnum.SystemName);
         
            //    recipient = parameterDb.GetParameterValue(ParameterNameEnum.TestMailingList);

            //    if (!String.IsNullOrEmpty(recipient) && sendEmail)
            //    {
            //        // Compose the email subject and body
            //        EmailTemplateDb emailTemplateDb = new EmailTemplateDb();
            //        EmailTemplate.EmailTemplateDataTable emailTemplateDt = new EmailTemplate.EmailTemplateDataTable();

            //        if (stage.ToString() == SendToCDBStageEnum.Verified.ToString() || stage.ToString() == SendToCDBStageEnum.ModifiedVerified.ToString())
            //        {
            //            emailTemplateDt = emailTemplateDb.GetEmailTemplateByCode(EmailTemplateCodeEnum.Exception_Log_CDB_Verify_Template.ToString());
            //        }
            //        else if (stage.ToString() == SendToCDBStageEnum.Accept.ToString())
            //        {
            //            emailTemplateDt = emailTemplateDb.GetEmailTemplateByCode(EmailTemplateCodeEnum.Exception_Log_CDB_Accept_Template.ToString());
            //        }


            //        if (emailTemplateDt.Rows.Count > 0)
            //        {
            //            string content = string.Empty;
            //            content += "Ref no.:" + refNo + " (" + sourceName + ")" + Environment.NewLine + Environment.NewLine;
            //            content += "Error:" + Environment.NewLine;
            //            content += reason + Environment.NewLine + Environment.NewLine;
            //            content += "Details:" + Environment.NewLine;
            //            content += errorMessage + Environment.NewLine;

            //            EmailTemplate.EmailTemplateRow emailTemplate = emailTemplateDt[0];
            //            subject = emailTemplate.Subject.Replace("[" + EmailTemplateVariablesEnum.Source.ToString() + "]", refNo);
            //            message = emailTemplate.Content;
            //            message = message.Replace("[" + EmailTemplateVariablesEnum.Remark.ToString() + "]", content);
            //            message = message.Replace("[" + EmailTemplateVariablesEnum.SystemName.ToString() + "]", systemName);
            //        }

            //        Util.CDBLog(string.Empty, message, EventLogEntryType.Error);
            //        Util.SendMail(systemName, systemEmail, recipient, string.Empty, string.Empty, subject, message);
            //        //Util.CDBLog(string.Empty, "test", EventLogEntryType.Error);
            //    }
            //}
            #endregion
            return rowAffected;
        }

        #endregion

        #endregion
    }
}
