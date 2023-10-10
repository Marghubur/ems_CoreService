using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;

namespace ServiceLayer.Interface
{
    public interface IInitialRegistrationService
    {
        string InitialOrgRegistrationService(RegistrationForm registrationForm, Files files, IFormFileCollection fileCollection);
    }
}
