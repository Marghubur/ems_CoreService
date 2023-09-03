using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IProjectService
    {
        string AddWikiService(WikiDetail project);
        Project GetAllWikiService(long ProjectId);
        Task<Project> AddUpdateProjectDetailService(Project projectDetail);
        List<Project> GetAllProjectDeatilService(FilterModel filterModel);
        DataSet GetProjectPageDetailService(long ProjectId);
        List<ProjectMemberDetail> DeleteTeamMemberService(int projectMemberDetailId, int projectId);
    }
}
