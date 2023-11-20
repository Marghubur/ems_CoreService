using BottomhalfCore.DatabaseLayer.Common.Code;
using EMailService.Modal;
using ModalLayer.Modal;
using NUnit.Framework.Internal.Execution;
using ServiceLayer.Interface;
using System.Collections.Generic;

namespace ServiceLayer.Code
{
    public class ShiftService : IShiftService
    {
        private readonly IDb _db;
        private readonly CurrentSession _session;

        public ShiftService(IDb db, CurrentSession session)
        {
            _db = db;
            _session = session;
        }

        public List<ShiftDetail> GetAllShiftService(FilterModel filterModel)
        {
            var result = _db.GetList<ShiftDetail>(Procedures.Work_Shifts_Filter, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
                filterModel.SortBy
            });
            return result;
        }

        public List<ShiftDetail> UpdateWorkShiftService(ShiftDetail shiftDetail)
        {
            ValidateWorkShift(shiftDetail);
            var existingShift = _db.Get<ShiftDetail>(Procedures.Work_Shifts_Getby_Id, new { WorkShiftId = shiftDetail.WorkShiftId });
            if (existingShift == null)
                throw HiringBellException.ThrowBadRequest("No record found for the given shift. Please contact to admin.");

            existingShift.Department = shiftDetail.Department;
            existingShift.WorkFlowCode = shiftDetail.WorkFlowCode;
            existingShift.ShiftTitle = shiftDetail.ShiftTitle;
            existingShift.Description = shiftDetail.Description;
            existingShift.IsMon = shiftDetail.IsMon;
            existingShift.IsTue = shiftDetail.IsTue;
            existingShift.IsWed = shiftDetail.IsWed;
            existingShift.IsThu = shiftDetail.IsThu;
            existingShift.IsFri = shiftDetail.IsFri;
            existingShift.IsSat = shiftDetail.IsSat;
            existingShift.IsSun = shiftDetail.IsSun;
            existingShift.TotalWorkingDays = shiftDetail.TotalWorkingDays;
            existingShift.StartDate = shiftDetail.StartDate;
            existingShift.EndDate = shiftDetail.EndDate;
            existingShift.OfficeTime = shiftDetail.OfficeTime;
            existingShift.Duration = shiftDetail.Duration;
            existingShift.Status = shiftDetail.Status;
            existingShift.LunchDuration = shiftDetail.LunchDuration;
         
            return WorkShiftInsertUpdateService(shiftDetail);
        }

        public List<ShiftDetail> InsertWorkShiftService(ShiftDetail shiftDetail)
        {
            ValidateWorkShift(shiftDetail);
            return WorkShiftInsertUpdateService(shiftDetail);
        }

        private List<ShiftDetail> WorkShiftInsertUpdateService(ShiftDetail shiftDetail)
        {
            shiftDetail.AdminId = _session.CurrentUserDetail.UserId;
            var result = _db.Execute<ShiftDetail>(Procedures.Work_Shifts_Insupd, shiftDetail, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to insert or update shift detail");

            FilterModel filterModel = new FilterModel
            {
                SearchString = $"1=1 and CompanyId={shiftDetail.CompanyId}"
            };
            return this.GetAllShiftService(filterModel);
        }

        private void ValidateWorkShift(ShiftDetail shiftDetail)
        {
            if (shiftDetail.CompanyId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid company selected. Please login again");

            if (shiftDetail.Department <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid department selected");

            if (string.IsNullOrEmpty(shiftDetail.WorkFlowCode))
                throw HiringBellException.ThrowBadRequest("Work flow code is null or empty");

            if (string.IsNullOrEmpty(shiftDetail.ShiftTitle))
                throw HiringBellException.ThrowBadRequest("Shift title is null or empty");

            if (shiftDetail.TotalWorkingDays <= 0)
                throw HiringBellException.ThrowBadRequest("Working days is zero or invalid");

            if (shiftDetail.StartDate == null)
                throw HiringBellException.ThrowBadRequest("Start date is null or empty");

            if (shiftDetail.EndDate == null)
                throw HiringBellException.ThrowBadRequest("End date is null or empty");

            if (string.IsNullOrEmpty(shiftDetail.OfficeTime))
                throw HiringBellException.ThrowBadRequest("Office time is null or empty");

            if (shiftDetail.Duration <= 0)
                throw HiringBellException.ThrowBadRequest("Department is zero or invalid");

            if (shiftDetail.LunchDuration <= 0)
                throw HiringBellException.ThrowBadRequest("Lunch duration is zero or invalid");
        }

        public ShiftDetail GetWorkShiftByIdService(int WorkShiftId)
        {
            var result = _db.Get<ShiftDetail>(Procedures.Work_Shifts_Getby_Id, new { WorkShiftId = WorkShiftId });
            return result;
        }

        public ShiftDetail GetWorkShiftByEmpIdService(int EmployeeId)
        {
            var result = _db.Get<ShiftDetail>(Procedures.Work_Shifts_Getby_Empid, new { EmployeeId = EmployeeId });
            return result;
        }
    }
}
