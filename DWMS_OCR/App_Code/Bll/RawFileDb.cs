using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.RawFileTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class RawFileDb
    {
        private RawFileTableAdapter _RawFileTableAdapter = null;

        protected RawFileTableAdapter Adapter
        {
            get
            {
                if (_RawFileTableAdapter == null)
                    _RawFileTableAdapter = new RawFileTableAdapter();

                return _RawFileTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get all the Raw files.
        /// </summary>
        /// <returns>RawFile table</returns>
        public RawFile.RawFileDataTable GetRawFiles()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the RawFile by set id.
        /// </summary>
        /// <param name="docSetId">Set id</param>
        /// <returns>RawFile table</returns>
        public RawFile.RawFileDataTable GetRawFilesByDocSetId(int docSetId)
        {
            return Adapter.GetDataByDocSetId(docSetId);
        }

        /// <summary>
        /// Get the RawFile by raw page id.
        /// </summary>
        /// <param name="rawPageId">Raw page id</param>
        /// <returns>RawFile table</returns>
        public RawFile.RawFileDataTable GetRawFilesByRawPageId(int rawPageId)
        {
            return Adapter.GetDataByRawPageId(rawPageId);
        }

        /// <summary>
        /// Get Raw file by doc set id and file name.
        /// </summary>
        /// <param name="docSetId">Set id</param>
        /// <param name="fileName">File name</param>
        /// <returns>RawFile table</returns>
        public RawFile.RawFileDataTable GetRawFilesBySetIdAndFileName(int docSetId, string fileName)
        {
            return Adapter.GetDataBySetIdAndFileName(docSetId, fileName);
        }
        #endregion

        #region Insert Methods
        /// <summary>
        /// Insert the raw file.
        /// </summary>
        /// <param name="docSetId">Set id</param>
        /// <param name="fileName">File name</param>
        /// <param name="fileData">File data</param>
        /// <returns>RawFile id</returns>
        public int Insert(int docSetId, string fileName, byte[] fileData)
        {
            RawFile.RawFileDataTable dt = new RawFile.RawFileDataTable();
            RawFile.RawFileRow r = dt.NewRawFileRow();

            r.DocSetId = docSetId;
            r.FileName = fileName;
            r.FileData = fileData;
            r.SkipCategorization = false;

            dt.AddRawFileRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }

        /// <summary>
        /// Insert the raw file with docChannel and isverified to determine if the file needs to skip categorization.
        /// </summary>
        /// <param name="docSetId">Set id</param>
        /// <param name="fileName">File name</param>
        /// <param name="fileData">File data</param>
        /// <returns>RawFile id</returns>
        public int Insert(int docSetId, string fileName, byte[] fileData, string docId, string docChannel, bool isVerified)
        {
            RawFile.RawFileDataTable dt = new RawFile.RawFileDataTable();
            RawFile.RawFileRow r = dt.NewRawFileRow();

            //r.SkipCategorization = false;
            r.DocSetId = docSetId;
            r.FileName = fileName;
            r.FileData = fileData;
            //if (isVerified || (isVerified && docChannel.ToString() == "002" && !docId.ToString().ToLower().StartsWith("u")))
            if (isVerified || (docChannel.ToString() == "002" && !docId.ToString().ToLower().StartsWith("u")))
                r.SkipCategorization = true;
            else if (isVerified || (docChannel.ToString() == "011" && !docId.ToString().ToLower().StartsWith("u"))) //Edited By Edward 09.11.2013 011 New Channel
                r.SkipCategorization = true;
            else if (isVerified || (docChannel.ToString() == "001" && !docId.ToString().ToLower().StartsWith("u"))) //Added By Edward 19/3/2014 SkipCategorization for Phase 1
                r.SkipCategorization = true;
            else
                r.SkipCategorization = false;

            dt.AddRawFileRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }

        #endregion
    }
}
