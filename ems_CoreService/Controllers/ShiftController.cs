using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;

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
            try
            {
                var result = _shiftService.GetAllShiftService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost("UpdateWorkShift")]
        public IResponse<ApiResponse> UpdateWorkShift(ShiftDetail shiftDetail)
        {
            try
            {
                var result = _shiftService.UpdateWorkShiftService(shiftDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, shiftDetail);
            }
        }

        [HttpPost("CreateWorkShift")]
        public IResponse<ApiResponse> InsertWorkShift(ShiftDetail shiftDetail)
        {
            try
            {
                var result = _shiftService.InsertWorkShiftService(shiftDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, shiftDetail);
            }
        }

        [HttpGet("GetWorkShift/{WorkShiftId}")]
        public IResponse<ApiResponse> InsertWorkShift([FromRoute] int WorkShiftId)
        {
            try
            {
                var result = _shiftService.GetWorkShiftByIdService(WorkShiftId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, WorkShiftId);
            }
        }

        [HttpGet("GetWorkShiftByEmpId/{EmployeeId}")]
        public IResponse<ApiResponse> GetWorkShiftByEmpId([FromRoute] int EmployeeId)
        {
            try
            {
                var result = _shiftService.GetWorkShiftByEmpIdService(EmployeeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }
    }
}