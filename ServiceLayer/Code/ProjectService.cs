using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using EMailService.Modal;
using Microsoft.AspNetCore.Hosting;
using ModalLayer.Modal;
using OpenXmlPowerTools;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static ApplicationConstants;

namespace ServiceLayer.Code
{
    public class ProjectService : IProjectService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly MicroserviceRegistry _microserviceUrlLogs;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly IHttpClientFactory _httpClientFactory;
        public ProjectService(IDb db,
            CurrentSession currentSession,
            FileLocationDetail fileLocationDetail,
            IWebHostEnvironment hostingEnvironment,
            MicroserviceRegistry microserviceUrlLogs,
            RequestMicroservice requestMicroservice,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _currentSession = currentSession;
            _fileLocationDetail = fileLocationDetail;
            _hostingEnvironment = hostingEnvironment;
            _microserviceUrlLogs = microserviceUrlLogs;
            _requestMicroservice = requestMicroservice;
            _httpClientFactory = httpClientFactory;
        }
        public string AddWikiService(WikiDetail project)
        {
            if (project.ProjectId <= 0)
                throw new HiringBellException("Invalid project id");

            Project projectDetail = _db.Get<Project>(Procedures.Project_Detail_Getby_Id, new { project.ProjectId });
            if (projectDetail == null)
                throw new HiringBellException("Invalid project selected");

            var folderPath = Path.Combine(_fileLocationDetail.DocumentFolder, _fileLocationDetail.CompanyFiles, "project_document");
            if (!Directory.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, folderPath)))
                Directory.CreateDirectory(Path.Combine(_hostingEnvironment.ContentRootPath, folderPath));
            string filename = projectDetail.ProjectName.Replace(" ", "") + ".txt";
            var filepath = Path.Combine(folderPath, filename);
            if (File.Exists(filepath))
                File.Delete(filepath);

            var txt = new StreamWriter(filepath);
            txt.Write(project.SectionDescription);
            txt.Close();

            projectDetail.PageIndexDetail = "[]";
            projectDetail.KeywordDetail = "[]";
            projectDetail.DocumentPath = filepath;
            projectDetail.AdminId = _currentSession.CurrentUserDetail.UserId;

            var result = _db.Execute<Project>(Procedures.Wiki_Detail_Upd, projectDetail, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("fail to insert or update");

            return result;
        }

        public async Task<Project> AddUpdateProjectDetailService(Project projectDetail)
        {
            string result = string.Empty;
            this.ProjectDetailValidtion(projectDetail);
            Project project = _db.Get<Project>(Procedures.Project_Detail_Getby_Id, new { projectDetail.ProjectId });
            if (project == null)
            {
                project = projectDetail;
                project.PageIndexDetail = "[]";
                project.KeywordDetail = "[]";
                project.DocumentationDetail = "[]";
            }
            else
            {
                project.ProjectName = projectDetail.ProjectName;
                project.ProjectDescription = projectDetail.ProjectDescription;
                project.IsClientProject = projectDetail.IsClientProject;
                project.ClientId = projectDetail.ClientId;
                project.HomePageUrl = projectDetail.HomePageUrl;
                project.ProjectStartedOn = projectDetail.ProjectStartedOn;
                project.ProjectEndedOn = projectDetail.ProjectEndedOn;
                project.CompanyId = projectDetail.CompanyId;
                project.DocumentPath = projectDetail.DocumentPath;
            }

            projectDetail.AdminId = _currentSession.CurrentUserDetail.UserId;
            if (projectDetail.TeamMembers != null && projectDetail.TeamMembers.Count > 0)
            {
                var data = (from n in projectDetail.TeamMembers
                            select new ProjectMemberDetail
                            {
                                ProjectMemberDetailId = n.ProjectMemberDetailId > 0 ? n.ProjectMemberDetailId : 0,
                                ProjectId = Convert.ToInt32(DbProcedure.getParentKey(projectDetail.ProjectId)),
                                EmployeeId = n.EmployeeId,
                                DesignationId = n.DesignationId,
                                FullName = n.FullName,
                                Email = n.Email,
                                IsActive = n.IsActive,
                                Grade = n.Grade,
                                MemberType = n.MemberType,
                                AssignedOn = DateTime.UtcNow,
                                LastDateOnProject = null
                            }).ToList<object>();
                result = await _db.BatchInsetUpdate(
                    Procedures.Project_Detail_Insupd,
                    project,
                    data);

                if (string.IsNullOrEmpty(result))
                    throw new HiringBellException("Fail to Insert or Update");

                project.TeamMembers = projectDetail.TeamMembers;
            }
            else
            {
                result = _db.Execute<Project>(Procedures.Project_Detail_Insupd, project, true);
                if (string.IsNullOrEmpty(result))
                    throw new HiringBellException("Fail to Insert or Update");

                project.ProjectId = Int32.Parse(result);
            }

            return project;
        }

        public Project GetAllWikiService(long ProjectId)
        {
            var result = _db.Get<Project>(Procedures.Project_Detail_Getby_Id, new { ProjectId });
            if (File.Exists(result.DocumentPath))
            {
                var txt = File.ReadAllText(result.DocumentPath);
                result.DocumentationDetail = txt;
            }
            return result;
        }

        public List<Project> GetAllProjectDeatilService(FilterModel filterModel)
        {
            var result = _db.GetList<Project>(Procedures.Project_Detail_Filter, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize,
                EmployeeId = _currentSession.CurrentUserDetail.UserId,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            });
            if (result == null)
                throw new HiringBellException("Unable to load project list data.");

            return result;
        }

        public async Task<(List<Project>, List<ProjectMemberDetail>)> GetAllProjectWithMemberDeatilService(FilterModel filterModel)
        {
            var result = _db.GetList<Project, ProjectMemberDetail>(Procedures.PROJECT_DETAIL_WITH_MEMBERS_FILTER, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
            });

            if (result.Item1.Any())
            {
                foreach (var project in result.Item1)
                {
                    project.ProjectDescription = await ReadTextFile(project.ProjectDescriptionFilePath);
                }
            }

            return await Task.FromResult(result);
        }

        private async Task<string> ReadTextFile(string filePath)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_microserviceUrlLogs.ResourceBaseUrl}{filePath}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }

        public async Task<string> GetDocxHtmlService(FileDetail fileDetail)
        {
            var filPath = $"{_microserviceUrlLogs.ResourceBaseUrl}{fileDetail.FilePath}";
            var url = $"{_microserviceUrlLogs.ConvertDocxToHtml}";

            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetPayload(filPath)
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            return await _requestMicroservice.PostRequest<string>(microserviceRequest);
        }

        private void ProjectDetailValidtion(Project project)
        {
            if (string.IsNullOrEmpty(project.ProjectName))
                throw new HiringBellException("Project name is null or empty");

            if (project.CompanyId <= 0)
                throw new HiringBellException("Compnay is not selected. Please selete your company.");

        }

        public DataSet GetProjectPageDetailService(long ProjectId)
        {
            var result = _db.FetchDataSet(Procedures.Project_Get_Page_Data, new { ProjectId = ProjectId });

            if (result.Tables.Count != 4)
                throw HiringBellException.ThrowBadRequest("Project detail not found. Please contact to admin.");

            result.Tables[0].TableName = "Project";
            result.Tables[1].TableName = "Clients";
            result.Tables[2].TableName = "Employees";
            result.Tables[3].TableName = "TeamMembers";
            return result;
        }

        public List<ProjectMemberDetail> DeleteTeamMemberService(int projectMemberDetailId, int projectId)
        {
            if (projectMemberDetailId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid team members selected. Please select a valid member");

            if (projectId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid prohect selected");

            var teamMembers = _db.GetList<ProjectMemberDetail>(Procedures.Project_Member_Getby_Projectid, new { ProjectId = projectId });
            if (teamMembers == null || teamMembers.Count == 0)
                throw HiringBellException.ThrowBadRequest("Team member record not found");

            var teamMember = teamMembers.Find(x => x.ProjectMemberDetailId == projectMemberDetailId);
            if (teamMember.DesignationId == (int)EmployeesRole.ProjectManager || teamMember.DesignationId == (int)EmployeesRole.TeamLead || teamMember.DesignationId == (int)EmployeesRole.ProjectArchitect)
                throw HiringBellException.ThrowBadRequest("You can't be deleted these member");

            teamMember.IsActive = false;
            var result = _db.Execute<ProjectMemberDetail>(Procedures.Team_Member_Upd, teamMember, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to delete team member");

            teamMembers = teamMembers.FindAll(x => x.IsActive == true);
            return teamMembers;
        }
    }
}
