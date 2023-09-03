using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceLayer.Interface
{
    public interface IInitialRegistrationService
    {
        string InitialOrgRegistrationService(RegistrationForm registrationForm);
    }
}
