using BottomhalfCore.Services.Code;
using EMailService.Modal.CronJobs;
using Microsoft.Extensions.Options;
using ModalLayer.Modal;
using MySql.Data.MySqlClient;
using ServiceLayer.Code.Leaves;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class RunLeaveEndYearService : IRunLeaveEndYearService
    {
        private readonly YearEndCalculation _yearEndCalculation;
        private readonly MasterDatabase _masterDatabase;
        public RunLeaveEndYearService(YearEndCalculation yearEndCalculation, IOptions<MasterDatabase> options)
        {
            _yearEndCalculation = yearEndCalculation;
            _masterDatabase = options.Value;
        }

        public async Task LoadDbConfiguration()
        {
            List<LeaveYearEnd> leaveYearEnds = new List<LeaveYearEnd>();
            var databaseConfiguration = await GetAllDbConfiguration();
            databaseConfiguration.ForEach(x =>
            {
                leaveYearEnds.Add(new LeaveYearEnd
                {
                    ProcessingDateTime = DateTime.UtcNow,
                    ConnectionString = $"server={x.Server};port={x.Port};database={x.Database};User Id={x.UserId};password={x.Password};Connection Timeout={x.ConnectionTimeout};Connection Lifetime={_masterDatabase.Connection_Lifetime};Min Pool Size={_masterDatabase.Min_Pool_Size};Max Pool Size={_masterDatabase.Max_Pool_Size};Pooling={_masterDatabase.Pooling};",
                });

            });

            leaveYearEnds.ForEach(async x => await _yearEndCalculation.RunLeaveYearEndCycle(x));

            await Task.CompletedTask;
        }

        private async Task<List<DbConfigModal>> GetAllDbConfiguration()
        {
            List<DbConfigModal> databaseConfiguration = new List<DbConfigModal>();
            string cs = $"server={_masterDatabase.Server};port={_masterDatabase.Port};database={_masterDatabase.Database};User Id={_masterDatabase.User_Id};password={_masterDatabase.Password};Connection Timeout={_masterDatabase.Connection_Timeout};Connection Lifetime={_masterDatabase.Connection_Lifetime};Min Pool Size={_masterDatabase.Min_Pool_Size};Max Pool Size={_masterDatabase.Max_Pool_Size};Pooling={_masterDatabase.Pooling};";
            using (var connection = new MySqlConnection(cs))
            {
                using (MySqlCommand command = new MySqlCommand())
                {
                    try
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = "select * from database_connections";
                        using (MySqlDataAdapter dataAdapter = new MySqlDataAdapter())
                        {
                            var dataSet = new DataSet();
                            connection.Open();
                            dataAdapter.SelectCommand = command;
                            dataAdapter.Fill(dataSet);

                            if (dataSet.Tables == null || dataSet.Tables.Count != 1 || dataSet.Tables[0].Rows.Count == 0)
                                throw new Exception("Fail to load the master data");

                            databaseConfiguration = Converter.ToList<DbConfigModal>(dataSet.Tables[0]);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            return await Task.FromResult(databaseConfiguration);
        }
    }
}