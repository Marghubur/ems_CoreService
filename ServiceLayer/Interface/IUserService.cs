using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Profile;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IUserService
    {
        Task<DataSet> GetEmployeeAndChientListService();
        Task<ProfileDetail> UploadUserInfo(string userId, ProfessionalUser userInfo, IFormFileCollection FileCollection, int UserTypeId);
        ProfileDetail GetUserDetail(long EmployeeId);
        string GenerateResume(long userId);
        ProfileDetail UpdateProfile(ProfessionalUser professionalUser, int UserTypeId, int IsProfileImageRequest = 0);
        Task<Files> UploadResume(string userId, ProfessionalUser professionalUser, IFormFileCollection FileCollection, int UserTypeId);
        Task<string> UploadDeclaration(string UserId, int UserTypeId, UserDetail userDetail, IFormFileCollection FileCollection, List<Files> fileDetail);
    }
}
