using BottomhalfCore.DatabaseLayer.Common.Code;
using Microsoft.AspNetCore.Hosting;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ApplicationConstants;

namespace ServiceLayer.Code
{
    public class ProjectService : IProjectService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IHostingEnvironment _hostingEnvironment;
        public ProjectService(IDb db, CurrentSession currentSession, FileLocationDetail fileLocationDetail, IHostingEnvironment hostingEnvironment)
        {
            _db = db;
            _currentSession = currentSession;
            _fileLocationDetail = fileLocationDetail;
            _hostingEnvironment = hostingEnvironment;
        }
        public string AddWikiService(WikiDetail project)
        {
            if (project.ProjectId <= 0)
                throw new HiringBellException("Invalid project id");

            Project projectDetail = _db.Get<Project>("sp_project_detail_getby_id", new { project.ProjectId });
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

            var result = _db.Execute<Project>("sp_wiki_detail_upd", projectDetail, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("fail to insert or update");

            return result;
        }

        public async Task<Project> AddUpdateProjectDetailService(Project projectDetail)
        {
            string result = string.Empty;
            this.ProjectDetailValidtion(projectDetail);
            Project project = _db.Get<Project>("sp_project_detail_getby_id", new { projectDetail.ProjectId });
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
                    "sp_project_detail_insupd",
                    project,
                    data);

                if (string.IsNullOrEmpty(result))
                    throw new HiringBellException("Fail to Insert or Update");

                project.TeamMembers = projectDetail.TeamMembers;
            }
            else
            {
                result = _db.Execute<Project>("sp_project_detail_insupd", project, true);
                if (string.IsNullOrEmpty(result))
                    throw new HiringBellException("Fail to Insert or Update");

                project.ProjectId = Int32.Parse(result);
            }

            return project;
        }

        public Project GetAllWikiService(long ProjectId)
        {
            var result = _db.Get<Project>("sp_project_detail_getby_id", new { ProjectId });
            if (File.Exists(result.DocumentPath))
            {
                var txt = File.ReadAllText(result.DocumentPath);
                result.DocumentationDetail = txt;
            }
            return result;
        }

        public List<Project> GetAllProjectDeatilService(FilterModel filterModel)
        {
            var result = _db.GetList<Project>("sp_project_detail_getall", new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });
            if (result == null)
                throw new HiringBellException("Unable to load projext list data.");

            return result;
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
            var result = _db.FetchDataSet("sp_project_get_page_data", new { ProjectId = ProjectId });

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

            var teamMembers = _db.GetList<ProjectMemberDetail>("sp_project_member_getby_projectid", new { ProjectId = projectId });
            if (teamMembers == null || teamMembers.Count == 0)
                throw HiringBellException.ThrowBadRequest("Team member record not found");

            var teamMember = teamMembers.Find(x => x.ProjectMemberDetailId == projectMemberDetailId);
            if (teamMember.DesignationId == (int)EmployeesRole.ProjectManager || teamMember.DesignationId == (int)EmployeesRole.TeamLead || teamMember.DesignationId == (int)EmployeesRole.ProjectArchitect)
                throw HiringBellException.ThrowBadRequest("You can't be deleted these member");

            teamMember.IsActive = false;
            var result = _db.Execute<ProjectMemberDetail>("sp_team_member_upd", teamMember, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to delete team member");

            teamMembers = teamMembers.FindAll(x => x.IsActive == true);
            return teamMembers;
        }
    }
}
