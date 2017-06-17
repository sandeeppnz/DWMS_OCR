using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
            {
                new DWMS_OCR.OcrService.DWMS_OCR_Service(),
                new DWMS_OCR.OcrService.DWMS_SampleDocOCR_Service(),
                new DWMS_OCR.MaintenanceService.DWMS_Maintenance(),
                new DWMS_OCR.CdbService.DWMS_CDB_Service(),
                new DWMS_OCR.LeasService.DWMS_LEAS_Service()
            };
                ServiceBase.Run(ServicesToRun);
           }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.ToString(), EventLogEntryType.Error);
            }
        }
    }
}
