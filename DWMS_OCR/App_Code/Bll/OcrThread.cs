using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace DWMS_OCR.App_Code.Bll
{
    class OcrThread
    {
        int rawPageId;
        string filePath;
        ManualResetEvent doneEvent;

        RawPageDb rawPageDb;
        EventLog eventLog;
        ErrorLogDb errorLogDb;

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="rawPageId"></param>
        /// <param name="filePath"></param>
        /// <param name="doneEvent"></param>
        /// <param name="eventLog"></param>
        public OcrThread(int rawPageId, string filePath, ManualResetEvent doneEvent, EventLog eventLog)
        {
            this.rawPageId = rawPageId;
            this.filePath = filePath;
            this.doneEvent = doneEvent;
            this.eventLog = eventLog;

            rawPageDb = new RawPageDb();
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

                OcrManager ocrManager = new OcrManager(eventLog);
                ocrManager.GetOcrText(filePath, out ocrText);

                rawPageDb.Update(rawPageId, ocrText, true);                
            }
            catch (Exception e)
            {
                string errorSummary = string.Format("Error (OcrThread.ThreadPoolCallback): File={0}, Message={1}, StackTrace={2}"
                    , filePath, e.Message, e.StackTrace);

                eventLog.WriteEntry(errorSummary);

                RawPageDb rawPageDb2 = new RawPageDb();
                rawPageDb2.Update(rawPageId, true);

                errorLogDb.Insert("OcrThread.ThreadPoolCallback", errorSummary, DateTime.Now);
            }
            finally
            {
                // Sleep the thread to do some work
                Thread.Sleep(5000);

                eventLog.WriteEntry("Thread done: " + filePath);

                this.doneEvent.Set();
            }
        }
    }
}
