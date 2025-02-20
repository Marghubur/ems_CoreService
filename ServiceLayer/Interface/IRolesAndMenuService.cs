using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IRolesAndMenuService
    {
        Task<string> AddUpdatePermission(RolesAndMenu rolesAndMenus);
        DataSet GetsRolesandMenu(int accessLevelId);
        Task<List<AddRole>> GetRoles();
        Task<List<AddRole>> AddRole(AddRole addRole);
    }
}
