using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
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
            var result = _projectService.AddWikiService(project);
            return BuildResponse(result);  
        }
        [HttpGet("GetAllWiki/{ProjectId}")]
        public IResponse<ApiResponse> GetAllWikiById(long ProjectId)
        {
            var result = _projectService.GetAllWikiService(ProjectId);
            return BuildResponse(result);
        }

        [HttpPost("AddUpdateProjectDetail")]
        public async Task<ApiResponse> AddUpdateProjectDetail(Project projectDetail)
        {
            var result = await _projectService.AddUpdateProjectDetailService(projectDetail);
            return BuildResponse(result);
        }

        [HttpPost("GetAllProjectDeatil")]
        public IResponse<ApiResponse> GetAllProjectDeatil(FilterModel filterModel)
        {
            var result = _projectService.GetAllProjectDeatilService(filterModel);
            return BuildResponse(result);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetProjectPageDetail/{ProjectId}")]
        public IResponse<ApiResponse> GetProjectPageDetail([FromRoute] long ProjectId)
        {
            var result = _projectService.GetProjectPageDetailService(ProjectId);
            return BuildResponse(result);
        }

        [Authorize(Roles= Role.Admin)]
        [HttpDelete("DeleteTeamMember/{ProjectMemberDetailId}/{ProjectId}")]
        public IResponse<ApiResponse> DeleteTeamMember([FromRoute] int ProjectMemberDetailId, [FromRoute] int ProjectId)
        {
            var result = _projectService.DeleteTeamMemberService(ProjectMemberDetailId, ProjectId);
            return BuildResponse(result);  
        }


    }
}
