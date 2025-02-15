using Bot.CoreBottomHalf.CommonModal.API;
using EMailService.Modal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyCalenderController : BaseController
    {
        private readonly ICompanyCalendar _companyCalendar;
        private readonly HttpContext _httpContext;
        public CompanyCalenderController(ICompanyCalendar companyCalendar, IHttpContextAccessor httpContext)
        {
            _companyCalendar = companyCalendar;
            _httpContext = httpContext.HttpContext;
        }

        [HttpPost("GetAllHoliday")]
        public IResponse<ApiResponse> GetAllHoliday(FilterModel filterModel)
        {
            try
            {
                var result = _companyCalendar.GetAllHolidayService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost("HolidayInsertUpdate")]
        public IResponse<ApiResponse> HolidayInsertUpdate(CompanyCalendarDetail calendar)
        {
            try
            {
                var result = _companyCalendar.HolidayInsertUpdateService(calendar);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, calendar);
            }
        }

        [HttpDelete("DeleteHolidy/{CompanyCalendarId}")]
        public IResponse<ApiResponse> DeleteHolidyByCompId([FromRoute] long CompanyCalendarId)
        {
            try
            {
                var result = _companyCalendar.DeleteHolidayService(CompanyCalendarId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, CompanyCalendarId);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("UploadHolidayExcel")]
        public async Task<ApiResponse> UploadHolidayExcel()
        {
            try
            {
                IFormFileCollection file = _httpContext.Request.Form.Files;
                var result = await _companyCalendar.ReadHolidayDataService(file);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }
    }
}