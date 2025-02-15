using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : BaseController
    {
        private readonly IProjectService _projectService;
        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpPost("AddWiki")]
        public IResponse<ApiResponse> AddWiki(WikiDetail project)
        {
            try
            {
                var result = _projectService.AddWikiService(project);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, project);
            }
        }

        [HttpGet("GetAllWiki/{ProjectId}")]
        public IResponse<ApiResponse> GetAllWikiById(long ProjectId)
        {
            try
            {
                var result = _projectService.GetAllWikiService(ProjectId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, ProjectId);
            }
        }

        [HttpPost("AddUpdateProjectDetail")]
        public async Task<ApiResponse> AddUpdateProjectDetail(Project projectDetail)
        {
            try
            {
                var result = await _projectService.AddUpdateProjectDetailService(projectDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, projectDetail);
            }
        }

        [HttpPost("GetAllProjectDeatil")]
        public IResponse<ApiResponse> GetAllProjectDeatil(FilterModel filterModel)
        {
            try
            {
                var result = _projectService.GetAllProjectDeatilService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetProjectPageDetail/{ProjectId}")]
        public IResponse<ApiResponse> GetProjectPageDetail([FromRoute] long ProjectId)
        {
            try
            {
                var result = _projectService.GetProjectPageDetailService(ProjectId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, ProjectId);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpDelete("DeleteTeamMember/{ProjectMemberDetailId}/{ProjectId}")]
        public IResponse<ApiResponse> DeleteTeamMember([FromRoute] int ProjectMemberDetailId, [FromRoute] int ProjectId)
        {
            try
            {
                var result = _projectService.DeleteTeamMemberService(ProjectMemberDetailId, ProjectId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { ProjectMemberDetailId = ProjectMemberDetailId, ProjectId = ProjectId });
            }
        }

        [HttpPost("GetAllProjectWithMemberDeatil")]
        public async Task<ApiResponse> GetAllProjectWithMemberDeatil(FilterModel filterModel)
        {
            try
            {
                var result = await _projectService.GetAllProjectWithMemberDeatilService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }
    }
}