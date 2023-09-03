using ModalLayer.Modal;
using System.Data;

namespace ServiceLayer.Interface
{
    public interface IManageUserCommentService
    {
        string PostUserCommentService(UserComments userComments);
        DataSet GetCommentsService(string EmailId);
    }
}
