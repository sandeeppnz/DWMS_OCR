using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.DocPersonalTableAdapters;
using DWMS_OCR.App_Code.Dal;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Bll
{
    class DocPersonalDb
    {
        private DocPersonalTableAdapter _DocPersonalTableAdapter = null;

        protected DocPersonalTableAdapter Adapter
        {
            get
            {
                if (_DocPersonalTableAdapter == null)
                    _DocPersonalTableAdapter = new DocPersonalTableAdapter();

                return _DocPersonalTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public DocPersonal.DocPersonalDataTable GetDocPersonals()
        {
            return Adapter.GetData();
        }


     


        #endregion

        #region Insert Methods
        public int Insert(int docSetId, string nric, string name, string folder, string relationship)
        {
            DocPersonal.DocPersonalDataTable dt = new DocPersonal.DocPersonalDataTable();
            DocPersonal.DocPersonalRow r = dt.NewDocPersonalRow();

            r.DocSetId = docSetId;
            r.Nric = (nric.Length > 10 ? nric.Substring(0, 10) : nric);
            r.Name = name;
            r.Folder = folder;

            r.IdType = Retrieve.GetIdTypeByNRIC(nric);

            if (relationship.Equals(RelationshipEnum.Husband.ToString()) ||
                relationship.Equals(RelationshipEnum.Wife.ToString()))
                r.Relationship = relationship;

            dt.AddDocPersonalRow(r);
            Adapter.Update(dt);
            int id = r.Id;
            return id;
        }
        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        public bool DeleteByDocSetId(int docSetId)
        {
            return Adapter.DeleteByDocSetId(docSetId) > 0;
        }
        #endregion
    }
}
