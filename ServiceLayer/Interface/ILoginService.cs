using ModalLayer.Modal;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ILoginService
    {
        Task<LoginResponse> AuthenticateUser(UserDetail authUser);
        Task<LoginResponse> FetchAuthenticatedProviderDetail(UserDetail authUser);
        Task<bool> RegisterNewCompany(RegistrationForm registrationForm);
        Boolean RemoveUserDetailService(string Token);
        UserDetail GetUserDetail(AuthUser authUser);
        string ResetEmployeePassword (UserDetail authUser);
        Task<string> ForgotPasswordService(string email);
    }
}
