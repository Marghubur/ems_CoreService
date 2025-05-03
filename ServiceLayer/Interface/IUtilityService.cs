using Bt.Lib.PipelineConfig.Model;
using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IUtilityService
    {
        bool CheckIsJoinedInCurrentFinancialYear(DateTime doj, CompanySetting companySetting);
        Task<List<T>> ReadExcelData<T>(IFormFileCollection files);
        Task SendNotification(dynamic requestBody, KafkaTopicNames kafkaTopicNames);
        Task<List<T>> ReadExcelData<T>(IFormFile file, List<RenameColumns> renameMap = null);
    }
}
