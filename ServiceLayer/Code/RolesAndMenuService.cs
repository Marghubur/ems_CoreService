using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using ModalLayer.Modal;
using ServiceLayer.Caching;
using ServiceLayer.Interface;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class RolesAndMenuService : IRolesAndMenuService
    {
        private readonly IDb _db;
        public RolesAndMenuService(IDb db)
        {
            _db = db;
        }
        public async Task<string> AddUpdatePermission(RolesAndMenu rolesAndMenus)
        {
            var permissionMenu = (from n in rolesAndMenus.Menu
                                  select new RoleAccessibilityMapping
                                  {
                                      RoleAccessibilityMappingId = -1,
                                      AccessLevelId = rolesAndMenus.AccessLevelId,
                                      AccessCode = n.AccessCode,
                                      AccessibilityId = n.Permission
                                  }).ToList<RoleAccessibilityMapping>();

            
            //var result = await _db.BatchInsertUpdateAsync("sp_role_accessibility_mapping_InsUpd", ds.Tables[0], false);
            var rowsAffected = await _db.BulkExecuteAsync("sp_role_accessibility_mapping_InsUpd", permissionMenu);

            return ApplicationConstants.Successfull;
        }

        public DataSet GetsRolesandMenu(int accessLevelId)
        {
            DataSet result = null;
            if (accessLevelId > 0)
            {
                result = _db.FetchDataSet("sp_RolesAndMenu_GetAll", new
                {
                    accessLevelId
                });
            }
            return result;
        }

        public DataSet GetRoles()
        {
            DataSet result = _db.GetDataSet("sp_AccessLevel_Sel");
            return result;
        }

        public DataSet AddRole(AddRole addRole)
        {
            if (string.IsNullOrEmpty(addRole.RoleName))
                throw new HiringBellException("Role name is null or empty");

            if (string.IsNullOrEmpty(addRole.AccessCodeDefination))
                throw new HiringBellException("Access code defination is null or empty");

            string accessLevelId = "-1";
            DbParam[] dbParams = new DbParam[]
            {
                new DbParam(addRole.RoleName, typeof(string), "_RoleName"),
                new DbParam(addRole.AccessCodeDefination, typeof(string), "_AccessCodeDefination"),
                new DbParam(accessLevelId, typeof(string), "_AccessLevelId")
            };

            var result = _db.FetchDataSet("sp_AccessLevel_InsUpd", dbParams, true);
            return result;
        }
    }
}
