using Microsoft.AspNetCore.Mvc;
using ModalLayer;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyCalenderController : BaseController
    {
        private readonly ICompanyCalendar _companyCalendar;

        public CompanyCalenderController(ICompanyCalendar companyCalendar)
        {
            _companyCalendar = companyCalendar;
        }

        [HttpPost("GetAllHoliday")]
        public IResponse<ApiResponse> GetAllHoliday(FilterModel filterModel)
        {
            var result = _companyCalendar.GetAllHolidayService(filterModel);
            return BuildResponse(result);
        }

        [HttpPost("HolidayInsertUpdate")]
        public IResponse<ApiResponse> HolidayInsertUpdate(Calendar calendar)
        {
            var result = _companyCalendar.HolidayInsertUpdateService(calendar);
            return BuildResponse(result);
        }

        [HttpDelete("DeleteHolidy/{CompanyCalendarId}")]
        public IResponse<ApiResponse> HolidayInsertUpdate([FromRoute] long CompanyCalendarId)
        {
            var result = _companyCalendar.DeleteHolidayService(CompanyCalendarId);
            return BuildResponse(result);
        }
    }
}
