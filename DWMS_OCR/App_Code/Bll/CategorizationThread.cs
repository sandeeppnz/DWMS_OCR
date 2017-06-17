using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using DWMS_OCR.App_Code.Helper;
using DWMS_OCR.App_Code.Dal;
using System.Collections;

namespace DWMS_OCR.App_Code.Bll
{
    class CategorizationThread
    {
        int setId;
        private ArrayList categorizationSampleDocs;

        public CategorizationThread(int setId, ArrayList categorizationSampleDocs)
        {
            this.setId = setId;
            this.categorizationSampleDocs = categorizationSampleDocs;
        }

        /// <summary>
        /// Thread call back
        /// </summary>
        /// <param name="parameter"></param>
        public void Categorize()
        {
            ParameterDb parameterDb = new ParameterDb();
            bool logging = parameterDb.Logging();
            bool detailLogging = parameterDb.DetailLogging();
            try
            {
                bool success = false;

                // Assign 3 chances for the categorization to complete the process
                for (int cnt = 0; cnt < 3; cnt++)
                {                    
                    CategorizationManager categorizationManager = new CategorizationManager();
                    success = categorizationManager.StartCategorization(setId, categorizationSampleDocs);

                    if (logging) Util.DWMSLog("CategorizationThread.Categorize", String.Format("Categorization try {0}: {1}", cnt + 1, success.ToString()), EventLogEntryType.Information);

                    if (success)
                        break;
                }

                if (!success)
                {
                    // Update the status of the set
                    DocSetDb docSetDb = new DocSetDb();                    
                    docSetDb.UpdateSetStatus(setId, SetStatusEnum.Categorization_Failed);
                }

                // Delete all the document records without rawpages
                DocDb docDb = new DocDb();
                RawPageDb rawPageDb = new RawPageDb();

                Doc.DocDataTable docDt = docDb.GetDocBySetId(setId);

                foreach(Doc.DocRow doc in docDt)
                {
                    if (rawPageDb.GetRawPageByDocId(doc.Id).Rows.Count <= 0)
                    {
                        try
                        {
                            docDb.Delete(doc.Id);
                        }
                        catch (Exception)
                        {                            
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string errorSummary = string.Format("Error (CategorizationThread.ThreadPoolCallback): SetId={0}, Message={1}, StackTrace={2}",
                    setId.ToString(), e.Message, e.StackTrace);

                Util.DWMSLog("CategorizationThread.ThreadPoolCallback", errorSummary, EventLogEntryType.Error);

                try
                {
                    // Delete the documents created for the set
                    DocDb docDb = new DocDb();
                    docDb.DeleteByDocSetId(setId);
                }
                catch (Exception)
                {
                }
            }
            finally
            {
            }
        }
    }
}
