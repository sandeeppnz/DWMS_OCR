using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace DWMS_OCR.App_Code.Bll
{
    class SampleDocOcrThread
    {
        int samplePageId;
        string filePath;
        ManualResetEvent doneEvent;

        SamplePageDb samplePageDb;
        EventLog eventLog;
        ErrorLogDb errorLogDb;

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="samplePageId"></param>
        /// <param name="filePath"></param>
        /// <param name="doneEvent"></param>
        /// <param name="eventLog"></param>
        public SampleDocOcrThread(int samplePageId, string filePath, ManualResetEvent doneEvent, EventLog eventLog)
        {
            this.samplePageId = samplePageId;
            this.filePath = filePath;
            this.doneEvent = doneEvent;
            this.eventLog = eventLog;

            samplePageDb = new SamplePageDb();
            errorLogDb = new ErrorLogDb();
        }

        /// <summary>
        /// Thread call back
        /// </summary>
        /// <param name="parameter"></param>
        public void ThreadPoolCallback(object parameter)
        {
            try
            {
                string ocrText = string.Empty;

                OcrManager ocrManager = new OcrManager();
                ocrManager.GetOcrText(filePath, out ocrText);

                samplePageDb.Update(samplePageId, ocrText, true);                
            }
            catch (Exception e)
            {
                eventLog.WriteEntry(string.Format("Error (SampleDocOcrThread.ThreadPoolCallback): File={0}, Message={1}"
                    ,filePath , e.Message));

                samplePageDb.Update(samplePageId, true);

                errorLogDb.Insert("SampleDocOcrThread.ThreadPoolCallback", e.Message + ";" + filePath, DateTime.Now);
            }
            finally
            {
                // Sleep the thread to do some work
                Thread.Sleep(1000);

                this.doneEvent.Set();
            }
        }
    }
}
