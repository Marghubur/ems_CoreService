using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ServiceLayer.Interface
{
    public interface IDashboardService
    {
        DataSet GetSystemDashboardService(AttendenceDetail userDetails);
    }
}
