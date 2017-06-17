using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DWMS_OCR.App_Code.Dal.ProfileTableAdapters;
using DWMS_OCR.App_Code.Dal;

namespace DWMS_OCR.App_Code.Bll
{
    class ProfileDb
    {
        private ProfileTableAdapter _ProfileTableAdapter = null;

        protected ProfileTableAdapter Adapter
        {
            get
            {
                if (_ProfileTableAdapter == null)
                    _ProfileTableAdapter = new ProfileTableAdapter();

                return _ProfileTableAdapter;
            }
        }

        #region Retrieve Methods
        /// <summary>
        /// Get the document sets
        /// </summary>
        /// <returns></returns>
        public Profile.ProfileDataTable GetProfiles()
        {
            return Adapter.GetData();
        }

        /// <summary>
        /// Get the document set by id
        /// </summary>
        /// <returns></returns>
        public Profile.ProfileDataTable GetProfileByName(string name)
        {
            return Adapter.GetDataByName(name);
        }

        public Guid? GetSystemGuid()
        {
            Guid? result = null;

            Profile.ProfileDataTable dt = GetProfileByName("SYSTEM");

            if(dt.Rows.Count > 0)
            {
                Profile.ProfileRow dr = dt[0];
                result = dr.UserId;
            }

            return result;
        }


        public string GetUserNameByUserId(Guid userId)
        {
            return Adapter.GetUserNameByUserId(userId);
        }

        /// <summary>
        /// To Check if Specific Email is inside the list, distincted by Set Id to know the Section ID
        /// </summary>
        /// <param name="username"></param>
        /// <param name="setId"></param>
        /// <returns></returns>
        public static bool GetCountByEmailSetId(string OIC, int setId)
        {
            return (ProfileDs.GetCountByEmailSetId(OIC, setId) > 0);
        }

        public void GetSystemAccountInfo(out int sectionId, out int departmentId)
        {
            sectionId = -1;
            departmentId = -1;

            Profile.ProfileDataTable dt = GetProfileByName(SystemAccountEnum.SYSTEM.ToString());

            if (dt.Rows.Count > 0)
            {
                Profile.ProfileRow dr = dt[0];

                sectionId = dr.Section;

                SectionDb sectionDb = new SectionDb();
                Section.SectionDataTable sectionDt = sectionDb.GetSectionById(sectionId);

                if (sectionDt.Rows.Count > 0)
                {
                    Section.SectionRow sectionDr = sectionDt[0];

                    departmentId = sectionDr.Department;
                }
            }
        }
        #endregion

        #region Insert Methods
        #endregion

        #region Update Methods
        #endregion

        #region Delete Methods
        #endregion
    }
}
