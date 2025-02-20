using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using EMailService.Modal;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System.Collections.Generic;
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

        public async Task<List<AddRole>> GetRoles()
        {
            var result = _db.GetList<AddRole>("sp_AccessLevel_Sel");
            return await Task.FromResult(result);
        }

        public async Task<List<AddRole>> AddRole(AddRole addRole)
        {
            if (string.IsNullOrEmpty(addRole.RoleName))
                throw new HiringBellException("Role name is null or empty");

            if (string.IsNullOrEmpty(addRole.AccessCodeDefination))
                throw new HiringBellException("Access code defination is null or empty");

            var result = _db.Execute<AddRole>(Procedures.ACCESSLEVEL_INSUPD, new
            {
                addRole.RoleName,
                addRole.AccessCodeDefination,
                AccessLevelId = "-1"
            }, true);

            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to add new role");

            return await GetRoles();
        }
    }
}
