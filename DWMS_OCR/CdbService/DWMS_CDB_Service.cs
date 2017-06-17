using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using DWMS_OCR.App_Code.Helper;
using System.IO;
using DWMS_OCR.App_Code.Bll;
using DWMS_OCR.App_Code.Dal;
using System.Xml;
using DWMS_OCR.VerifyDocWebRef;
using DWMS_OCR.AcceptDocWebRef;

namespace DWMS_OCR.CdbService
{
    partial class DWMS_CDB_Service : ServiceBase
    {
        CDBVerify cdbVerify;
        CDBModifiedVerified cdbAccept;
        CDBCompleteAccept cdbCompleteAccept;



        public DWMS_CDB_Service()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists(Constants.DWMSCDBLogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(Constants.DWMSCDBLogSource, Constants.DWMSCDBLog);
            }

            eventLog.Source = Constants.DWMSCDBLogSource;
            eventLog.Log = Constants.DWMSCDBLog;

            cdbVerify = new CDBVerify();
            cdbAccept = new CDBModifiedVerified();
            cdbCompleteAccept = new CDBCompleteAccept();

        }

        protected override void OnStart(string[] args)
        {
            // Disable the timer and stop it
            timer.Stop();
            timer.Enabled = false;

            Util.CDBLog(string.Empty, "DWMS_CDB_Service Started.", EventLogEntryType.Information);

            // Enable the timer and start it
            timer.Enabled = true;
            timer.Start();


            cdbVerify.TriggerTest = CDBVerifyUtil.isTestXMLInputVerify();
            cdbVerify.TriggerSendToCDBVerify = CDBVerifyUtil.isSendToCDBVerify();
            cdbVerify.TriggerUpdateResultToDatabase = CDBVerifyUtil.isCDBUpdateResultToDBVerify();
            cdbVerify.RunOnce = CDBVerifyUtil.isTestRun();
            cdbVerify.XmlOutput = CDBVerifyUtil.isWriteXMLOuput();

            cdbAccept.TriggerTest = CDBVerifyUtil.isTestXMLInputAccept();
            cdbAccept.TriggerSendToCDBModifiedVerifiedDocs = CDBVerifyUtil.isSendToCDBAccept();
            cdbAccept.TriggerUpdateResultToDatabase = CDBVerifyUtil.isCDBUpdateResultToDBAccept();
            cdbAccept.RunOnce = CDBVerifyUtil.isTestRun();
            cdbAccept.XmlOutput = CDBVerifyUtil.isWriteXMLOuput();

            cdbCompleteAccept.TriggerTest = CDBVerifyUtil.isTestXMLInputCompleteAccept();
            cdbCompleteAccept.TriggerSendToCDBCompleteAccept = CDBVerifyUtil.isSendToCDBCompleteAccept();

            cdbCompleteAccept.TriggerUpdateResultToDatabase = CDBVerifyUtil.isCDBUpdateResultToDBCompleteAccept();
            cdbCompleteAccept.RunOnce = CDBVerifyUtil.isTestRun();
            cdbCompleteAccept.XmlOutput = CDBVerifyUtil.isWriteXMLOuput();
        }


        protected override void OnContinue()
        {
            base.OnContinue();

            Util.CDBLog(string.Empty, "DWMS_CDB_Service Continued.", EventLogEntryType.Information);
            timer.Start();
        }


        protected override void OnPause()
        {
            base.OnPause();

            Util.CDBLog(string.Empty, "DWMS_CDB_Service Paused.", EventLogEntryType.Information);
            timer.Stop();
        }



        protected override void OnStop()
        {
            Util.CDBLog(string.Empty, "DWMS_CDB_Service Stopped.", EventLogEntryType.Information);

            timer.Stop();
            timer.Enabled = false;
        }



        protected override void OnShutdown()
        {
            base.OnShutdown();

            Util.CDBLog(string.Empty, "DWMS_CDB_Service Shut down.", EventLogEntryType.Information);
            timer.Stop();
        }



        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Stop the timer. Start only after all the processes have been completed.
            timer.Stop();
            timer.Enabled = false;


            //Util.CDBLog(string.Empty, "Initiating DWMS to CDB Service for verified document sets", EventLogEntryType.Information);

            try
            {
                #region CDB Verify
                if (CDBVerifyUtil.RunVerify())
                {                    
                    if (!cdbVerify.TriggerTest)
                    {
                        cdbVerify.SendAllDocsUponVerificationVerified();
                        Util.CDBLog(string.Empty, "AFTER cdbVerify.SendAllDocsUponVerificationVerified()", EventLogEntryType.Information);
                    }
                    else
                    {
                        #region xml input
                        cdbVerify.RunXmlTest();
                        Util.CDBLog(string.Empty, "AFTER cdbVerify.RunXmlTest()", EventLogEntryType.Information);
                        //Stop Service
                        //ServiceController serviceController = new ServiceController("DWMS_CDB_Service");
                        //serviceController.Stop();
                        #endregion
                    } 
                }

                #endregion

                #region CDB Accept
                if (CDBVerifyUtil.ModifiedVerified())
                {                    
                    if (!cdbAccept.TriggerTest)
                    {
                        cdbAccept.SendModifiedDocsUponCompletenessChecked();
                        Util.CDBLog(string.Empty, "AFTER cdbAccept.SendModifiedDocsUponCompletenessChecked()", EventLogEntryType.Information);
                    }
                    else
                    {
                        #region xml input
                        cdbAccept.RunXmlTest();
                        Util.CDBLog(string.Empty, "AFTER cdbAccept.RunXmlTest()", EventLogEntryType.Information);
                        //ServiceController serviceController = new ServiceController("DWMS_CDB_Service");
                        //serviceController.Stop();
                        #endregion
                    } 
                }
                #endregion


                #region CDB Complete
                if (CDBVerifyUtil.RunCompleteAccept())
                {                    
                    if (!cdbCompleteAccept.TriggerTest)
                    {
                        cdbCompleteAccept.SendAllDocsUponCompletenessChecked();
                        Util.CDBLog(string.Empty, "AFTER cdbCompleteAccept.SendAllDocsUponCompletenessChecked()", EventLogEntryType.Information);
                    }
                    else
                    {
                        #region xml input
                        cdbCompleteAccept.RunXmlTest();
                        Util.CDBLog(string.Empty, "AFTER cdbCompleteAccept.RunXmlTest()", EventLogEntryType.Information);
                        //ServiceController serviceController = new ServiceController("DWMS_CDB_Service");
                        //serviceController.Stop();
                        #endregion
                    }
                }
                #endregion



				#region stop service if configured to run once
                if (cdbVerify.RunOnce || cdbAccept.RunOnce || cdbCompleteAccept.RunOnce)
                {
                    ServiceController serviceController = new ServiceController("DWMS_CDB_Service");
                    
                    serviceController.Stop();
                    Util.CDBLog(string.Empty, "AFTER serviceController.Stop()", EventLogEntryType.Information);
                }
                #endregion
				
				
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Error (DWMS_CDB_Service.timer_Elapsed): Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);
                Util.CDBLog("DWMS_CDB_Service.timer_Elapsed", errorMessage, EventLogEntryType.Error);
            }

            // Start the timer again.
            timer.Enabled = true;
            timer.Start();
        }
    }










}
