using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.PipelineConfig.Model;
using EMailService.Modal;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class ResignationService(IDb _db,
                                    CurrentSession _currentSession,
                                    IUtilityService _utilityService) : IResignationService
    {
        public async Task<dynamic> GetEmployeeResignationByIdService(long employeeId)
        {
            if (employeeId < 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            (EmployeeNoticePeriod employeeNoticePeriod, CompanySetting companySetting) = _db.GetMulti<EmployeeNoticePeriod, CompanySetting>(Procedures.EMPLOYEE_NOTICE_PERIOD_GETBY_EMPID, new
            {
                EmployeeId = employeeId,
                _currentSession.CurrentUserDetail.CompanyId
            });

            if (companySetting == null)
                throw HiringBellException.ThrowBadRequest("Company setting not found. Please contact to admin");

            return await Task.FromResult(new { EmployeeNoticePeriod = employeeNoticePeriod, CompanySetting = companySetting });
        }

        public async Task<string> SubmitResignationService(EmployeeNoticePeriod employeeNoticePeriod)
        {
            validateResignationDetail(employeeNoticePeriod);
            var result = await _db.ExecuteAsync(Procedures.EMPLOYEE_NOTICE_PERIOD_INSUPD, new
            {
                employeeNoticePeriod.EmployeeNoticePeriodId,
                employeeNoticePeriod.EmployeeId,
                employeeNoticePeriod.ResignType,
                ResignationStatus = (int)ItemStatus.Pending,
                employeeNoticePeriod.EmployeeComment,
                OfficialLastWorkingDay = DateTime.UtcNow.AddDays(employeeNoticePeriod.CompanyNoticePeriodInDays),
                AttachmentPath = "[]",
                employeeNoticePeriod.ApprovedOn,
                _currentSession.CurrentUserDetail.AdminId
            }, true);
            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to submit resignation detail");

            await AddEmployeeExitClearanceChainDetail(employeeNoticePeriod.EmployeeId);

            return result.statusMessage;
        }

        private async Task AddEmployeeExitClearanceChainDetail(long employeId)
        {
            var employeeExitClearanceChain = await GetEmployeeExitClearanceChain(employeId);
            if (employeeExitClearanceChain.Any())
            {
                var result = await _db.BulkExecuteAsync(Procedures.EMPLOYEE_EXIT_CLEARANCE_INS_UPD, (
                    from n in employeeExitClearanceChain
                    select new
                    {
                        n.EmployeeExitClearanceId,
                        n.EmployeeId,
                        n.ClearanceByName,
                        n.IsDepartment,
                        n.HandledBy,
                        n.ApprovalStatusId,
                        n.Comments
                    }).ToList(), true);

                if (result != employeeExitClearanceChain.Count)
                    throw HiringBellException.ThrowBadRequest("Fail to add employee exit clearance chain");
            }
        }

        private void validateResignationDetail(EmployeeNoticePeriod employeeNoticePeriod)
        {
            if (employeeNoticePeriod.EmployeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            if (string.IsNullOrEmpty(employeeNoticePeriod.EmployeeComment))
                throw HiringBellException.ThrowBadRequest("Invalid employee comment");

            if (string.IsNullOrEmpty(employeeNoticePeriod.ResignType))
                throw HiringBellException.ThrowBadRequest("Invalid reason selected");
        }

        public async Task<List<EmployeeResignation>> GetAllEmployeeResignationService(FilterModel filterModel)
        {
            var result = _db.GetList<EmployeeResignation>(Procedures.EMPLOYEE_RESIGNATION_FILTER, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });
            return await Task.FromResult(result);
        }

        public async Task<List<EmployeeAssetsAllocation>> GetEmployeeAssetsAllocationByEmpIdService(long employeeId)
        {
            var employeeAssetsAllocations = await GetEmployeeAssetsAllocationByEmpId(employeeId);
            if (employeeAssetsAllocations.Any())
            {
                employeeAssetsAllocations.ForEach(x =>
                {
                    var dictList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(x.AssetsDetail);
                    x.AssetDetails = dictList.SelectMany(dict => dict).Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value)).ToList();
                });
            }
            return employeeAssetsAllocations;
        }

        private async Task<List<EmployeeAssetsAllocation>> GetEmployeeAssetsAllocationByEmpId(long employeeId)
        {
            if (employeeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee selected");

            var result = _db.GetList<EmployeeAssetsAllocation>(Procedures.EMPLOYEE_ASSETS_ALLOCATION_GET_BY_EMPID, new
            {
                EmployeeId = employeeId
            });

            return await Task.FromResult(result);
        }

        public async Task<string> ApproveEmployeeAssetsAllocationService(List<EmployeeAssetsAllocation> employeeAssetsAllocations, long employeeId)
        {
            ValidateEmployeeAssetsDetail(employeeAssetsAllocations, employeeId);

            var employeeAssets = await GetEmployeeAssetsAllocationByEmpId(employeeId);
            if (!employeeAssets.Any())
                throw HiringBellException.ThrowBadRequest("No assets found");

            employeeAssets.ForEach(x =>
            {
                var currentAssetAllocation = employeeAssetsAllocations.Find(i => i.EmployeeAssetsAllocationId == x.EmployeeAssetsAllocationId);
                x.ReturnStatus = currentAssetAllocation.ReturnStatus;
                x.ReturnedOn = DateTime.UtcNow;
                x.ReturnedHandledBy = _currentSession.CurrentUserDetail.UserId;
                x.CommentsOnReturnedItem = currentAssetAllocation.CommentsOnReturnedItem;
            });

            var result = await _db.BulkExecuteAsync(Procedures.EMPLOYEE_ASSETS_ALLOCATION_INS_UPD, (
                from n in employeeAssets
                select new
                {
                    n.EmployeeAssetsAllocationId,
                    n.EmployeeId,
                    n.AssetsName,
                    n.AssetsDetail,
                    n.AllocatedOn,
                    n.AllocatedBy,
                    n.ReturnStatus,
                    n.ReturnedOn,
                    n.CommentsOnReturnedItem,
                    n.ReturnedHandledBy
                }).ToList(),
                true);

            if (result != employeeAssets.Count)
                throw HiringBellException.ThrowBadRequest("Fail to approve the employee assets allocation");

            return "Approved";
        }

        private void ValidateEmployeeAssetsDetail(List<EmployeeAssetsAllocation> employeeAssetsAllocations, long employeeId)
        {
            if (employeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invaid employee selected");

            foreach (var item in employeeAssetsAllocations)
            {
                if (string.IsNullOrEmpty(item.CommentsOnReturnedItem))
                    throw HiringBellException.ThrowBadRequest($"Please add comment on asset {item.AssetsName}");

                if (item.EmployeeAssetsAllocationId == 0)
                    throw HiringBellException.ThrowBadRequest("Invalid asset selected");
            }
        }

        private async Task<List<EmployeeExitClearance>> UpdateEmployeeExitCleranceStatus(long employeeId, string comment, int status)
        {
            var employeeExitClerance = _db.GetList<EmployeeExitClearance>(Procedures.EMPLOYEE_EXIT_CLEARANCE_GET_BY_EMPID, new
            {
                EmployeeId = employeeId
            });

            if (employeeExitClerance == null)
                throw HiringBellException.ThrowBadRequest("Employee ecit clearance record not found");

            var comments = new List<string>();
            var exitClearanceRecord = employeeExitClerance.Find(x => x.HandledBy == _currentSession.CurrentUserDetail.UserId);
            if (exitClearanceRecord == null)
                throw HiringBellException.ThrowBadRequest("You don't have priviliage to take this action");

            exitClearanceRecord.ApprovalStatusId = status;
            if (exitClearanceRecord.Comments != "[]")
                comments = JsonConvert.DeserializeObject<List<string>>(exitClearanceRecord.Comments);

            comments.Add(comment);
            exitClearanceRecord.Comments = JsonConvert.SerializeObject(comments);

            var result = await _db.ExecuteAsync(Procedures.EMPLOYEE_EXIT_CLEARANCE_INS_UPD, exitClearanceRecord, true);
            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to update employee exit clearance");

            return employeeExitClerance;
        }

        public async Task<List<EmployeeResignation>> ApproveEmployeeResignationService(long employeeId, string comment)
        {
            if (employeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invaid employee selected");

            if (string.IsNullOrEmpty(comment))
                throw HiringBellException.ThrowBadRequest("Please add comment");

            var employeeExitClearance = await UpdateEmployeeExitCleranceStatus(employeeId, comment, (int)ItemStatus.Approved);
            var isAllLevelApproved = employeeExitClearance.TrueForAll(x => x.ApprovalStatusId == (int)ItemStatus.Approved);

            if (isAllLevelApproved)
                await InactiveEmployeeAndSendNotification(employeeId);

            return await GetAllEmployeeResignationService(new FilterModel());
        }

        private async Task InactiveEmployeeAndSendNotification(long employeeId)
        {
            var employee = _db.Get<EmployeeResignation>(Procedures.EMPLOYEE_RESIGNATION_FILTER, new
            {
                SearchString = $"1=1 and en.EmployeeId = {employeeId}",
                SortBy = string.Empty,
                PageIndex = 1,
                PageSize = 10
            });

            if (employee == null)
                throw HiringBellException.ThrowBadRequest("Employee detail not found");

            var result = await _db.ExecuteAsync(Procedures.EMPLOYEE_INACTIVE_UPDATE, new
            {
                EmployeeUid = employeeId
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to in-active the employee");

            ExitProcessConfirmationModal exitProcessConfirmationModal = new ExitProcessConfirmationModal
            {
                EmployeeEmail = new List<string> { "marghub12@gmail.com" },
                kafkaServiceName = KafkaServiceName.Notification,
                LocalConnectionString = _currentSession.LocalConnectionString,
                CompanyEmail = employee.CompanyEmail,
                EmployeeName = employee.FullName,
                CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                CompanyAssetsReturnStatus = "Yes",
                CompanyContactNumber = employee.CompanyMobileNo,
                Designation = "HR",
                FullAndFinalSattlementStatus = "Completed",
                HRClearanceStatus = "Completed",
                LastWorkingDay = employee.OfficialLastWorkingDay.ToString("dddd, dd MMMM yyyy"),
                Name = _currentSession.CurrentUserDetail.FirstName + " " + _currentSession.CurrentUserDetail.LastName,
                RelievingAndExperienceLetterStatus = "Generated"
            };

            _ = Task.Run(async () =>
            {
                await _utilityService.SendNotification(exitProcessConfirmationModal, KafkaTopicNames.ATTENDANCE_REQUEST_ACTION);
            });
        }

        public async Task<List<EmployeeResignation>> RejectEmployeeResignationService(long employeeId, string comment)
        {
            if (employeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invaid employee selected");

            if (string.IsNullOrEmpty(comment))
                throw HiringBellException.ThrowBadRequest("Please add comment");

            await UpdateEmployeeExitCleranceStatus(employeeId, comment, (int)ItemStatus.Rejected);

            return await GetAllEmployeeResignationService(new FilterModel());
        }

        private async Task<List<EmployeeExitClearance>> GetEmployeeExitClearanceChain(long employeeId)
        {
            var employeeExitClearances = new List<EmployeeExitClearance>();
            var (employee, employeeExitConfiguration) = _db.GetList<Employee, EmployeeExitConfiguration>(Procedures.EMPLOYEE_AND_EMPLOYEE_EXIT_CONFIGURATION, new
            {
                EmployeeId = employeeId
            });

            if (employeeExitConfiguration.Any())
            {
                employeeExitConfiguration.ForEach(x =>
                {
                    long handleBy = 0;
                    if (x.ClearanceByRole && x.RoleName.Equals("Reporting Manager", StringComparison.OrdinalIgnoreCase))
                        handleBy = employee[0].ReportingManagerId;
                    else if (x.ClearanceByRole && x.RoleName.Equals("HR", StringComparison.OrdinalIgnoreCase))
                        handleBy = employee[0].SalaryGroupId;

                    employeeExitClearances.Add(new EmployeeExitClearance
                    {
                        EmployeeExitClearanceId = 0,
                        ApprovalStatusId = (int)ItemStatus.Pending,
                        EmployeeId = employeeId,
                        Comments = "[]",
                        ClearanceByName = x.RoleName,
                        IsDepartment = x.ClearanceByDepartment,
                        HandledBy = handleBy
                    });
                });
            }

            return await Task.FromResult(employeeExitClearances);
        }

        public async Task<List<EmployeeResignation>> EmployeeResignAssignToMeService(long employeeId)
        {
            if (employeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invaid employee selected");

            var result = _db.Get<EmployeeExitClearance>(Procedures.EMPLOYEE_EXIT_ASSIGN_BYID, new
            {
                EmployeeId = employeeId,
                AssigneId = _currentSession.CurrentUserDetail.UserId
            });

            if (result == null)
                throw HiringBellException.ThrowBadRequest("Your department is not define in configuration. Please contact to admin");

            result.HandledBy = _currentSession.CurrentUserDetail.UserId;

            var status = await _db.ExecuteAsync(Procedures.EMPLOYEE_EXIT_CLEARANCE_INS_UPD, result, true);
            if (string.IsNullOrEmpty(status.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to assign");

            return await GetAllEmployeeResignationService(new FilterModel());
        }

        public async Task<List<EmployeeExitConfiguration>> ManageEmployeeExitConfigurationService(List<EmployeeExitConfiguration> employeeExitConfigurations)
        {
            ConfigureEmployeeExitConfig(employeeExitConfigurations);

            var result = await _db.BulkExecuteAsync<EmployeeExitConfiguration>("", employeeExitConfigurations, true);

            return await Task.FromResult(employeeExitConfigurations);
        }

        private void ConfigureEmployeeExitConfig(List<EmployeeExitConfiguration> employeeExitConfigurations)
        {
            foreach (var item in employeeExitConfigurations)
            {
                if (item.DepartmentId > 0)
                {
                    item.ClearanceByRole = false;
                    item.RoleName = "";
                } else
                {
                    item.ClearanceByDepartment = false;
                    item.DepartmentId = 0;
                }
            }
        }
    }
}