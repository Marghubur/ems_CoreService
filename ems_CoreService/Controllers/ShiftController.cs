using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftController : BaseController
    {
        private readonly IShiftService _shiftService;

        public ShiftController(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        [HttpPost("GetAllWorkShift")]
        public IResponse<ApiResponse> GetAllWorkShift(FilterModel filterModel)
        {
            var result = _shiftService.GetAllShiftService(filterModel);
            return BuildResponse(result);
        }

        [HttpPost("UpdateWorkShift")]
        public IResponse<ApiResponse> UpdateWorkShift(ShiftDetail shiftDetail)
        {
            var result = _shiftService.UpdateWorkShiftService(shiftDetail);
            return BuildResponse(result);
        }

        [HttpPost("CreateWorkShift")]
        public IResponse<ApiResponse> InsertWorkShift(ShiftDetail shiftDetail)
        {
            var result = _shiftService.InsertWorkShiftService(shiftDetail);
            return BuildResponse(result);
        }

        [HttpGet("GetWorkShift/{WorkShiftId}")]
        public IResponse<ApiResponse> InsertWorkShift([FromRoute] int WorkShiftId)
        {
            var result = _shiftService.GetWorkShiftByIdService(WorkShiftId);
            return BuildResponse(result);
        }

        [HttpGet("GetWorkShiftByEmpId/{EmployeeId}")]
        public IResponse<ApiResponse> GetWorkShiftByEmpId([FromRoute] int EmployeeId)
        {
            var result = _shiftService.GetWorkShiftByEmpIdService(EmployeeId);
            return BuildResponse(result);
        }
    }
}
