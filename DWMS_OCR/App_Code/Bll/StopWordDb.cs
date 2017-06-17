using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.StopWordTableAdapters;
using DWMS_OCR.App_Code.Dal;
using System.Data;
using System.Collections;

namespace DWMS_OCR.App_Code.Bll
{
    class StopWordDb
    {
        private StopWordTableAdapter _StopWordTableAdapter = null;

        protected StopWordTableAdapter Adapter
        {
            get
            {
                if (_StopWordTableAdapter == null)
                    _StopWordTableAdapter = new StopWordTableAdapter();

                return _StopWordTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public StopWord.StopWordDataTable GetStopWords()
        {
            return Adapter.GetData();
        }

        public ArrayList GetStopWordsToArrayList()
        {
            ArrayList result = new ArrayList();

            StopWord.StopWordDataTable stopWordsDt = GetStopWords();

            foreach(StopWord.StopWordRow stopWord in stopWordsDt)
            {
                result.Add(stopWord.Word);
            }

            return result;
        }
        
        #endregion

        #region Checking Methods
        public bool IsStopWord(string word)
        {
            if (word.ToLower().Substring(0, 1).CompareTo("0") < 0) return true;

            int i = int.Parse(Adapter.GetWordCount(word).ToString());
            return i > 0;
        }
        #endregion
    }
}
