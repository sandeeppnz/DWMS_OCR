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
using DWMS_OCR.PersonInfoUpdateWebRef;


namespace DWMS_OCR.LeasService
{
    partial class DWMS_LEAS_Service : ServiceBase
    {
        LeasPersonInfoUpdate leasPersonInfoUpdate;

        public DWMS_LEAS_Service()
        {
            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists(Constants.DWMSLEASLogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(Constants.DWMSLEASLogSource, Constants.DWMSLEASLog);
            }

            eventLog.Source = Constants.DWMSLEASLogSource;
            eventLog.Log = Constants.DWMSLEASLog;
            leasPersonInfoUpdate = new LeasPersonInfoUpdate();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            try
            {
                timer.Stop();
                timer.Enabled = false;

                Util.LEASLog(string.Empty, "DWMS_LEAS_Service Started.", EventLogEntryType.Information);

                timer.Enabled = true;
                timer.Start();

                leasPersonInfoUpdate.XmlOutput = LeasPersonUpdateInfoUtil.isWriteXMLOuput();
                
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Error (DWMS_LEAS_Service.timer_Elapsed): Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);
                Util.LEASLog("DWMS_LEAS_Service.timer_Elapsed", errorMessage, EventLogEntryType.Error);
            }                                                       
        }



        protected override void OnContinue()
        {
            base.OnContinue();

            Util.LEASLog(string.Empty, "DWMS_LEAS_Service Continued.", EventLogEntryType.Information);
            timer.Start();
        }

        protected override void OnPause()
        {
            base.OnPause();

            Util.LEASLog(string.Empty, "DWMS_LEAS_Service Paused.", EventLogEntryType.Information);
            timer.Stop();
        }


        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            Util.LEASLog(string.Empty, "DWMS_LEAS_Service Stopped.", EventLogEntryType.Information);

            timer.Stop();
            timer.Enabled = false;
        }

       

        protected override void OnShutdown()
        {
            base.OnShutdown();

            Util.LEASLog(string.Empty, "DWMS_LEAS_Service Shut down.", EventLogEntryType.Information);
            timer.Stop();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Stop the timer. Start only after all the processes have been completed.
            
            timer.Stop();
            timer.Enabled = false;

            try
            {                
                leasPersonInfoUpdate.SendDocsToLeas();
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Error (DWMS_LEAS_Service.timer_Elapsed): Message={0}, StackTrace={1}",
                    ex.Message, ex.StackTrace);
                Util.LEASLog("DWMS_LEAS_Service.timer_Elapsed", errorMessage, EventLogEntryType.Error);
            }

            timer.Enabled = true;
            timer.Start();
        }
    }
}
