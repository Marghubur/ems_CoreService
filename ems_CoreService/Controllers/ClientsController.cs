using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : BaseController
    {
        private readonly IClientsService _clientsService;
        private readonly HttpContext _httpContext;
        public ClientsController(IClientsService clientsService, IHttpContextAccessor httpContext)
        {
            _clientsService = clientsService;
            _httpContext = httpContext.HttpContext;
        }

        [HttpGet("GetClientById/{ClientId}/{IsActive}/{UserTypeId}")]
        public ApiResponse GetClientById(long ClientId, bool IsActive, int UserTypeId)
        {
            var Result = _clientsService.GetClientDetailById(ClientId, IsActive, UserTypeId);
            return BuildResponse(Result, HttpStatusCode.OK);
        }

        [HttpPost("RegisterClient/{IsUpdating}")]
        public async Task<ApiResponse> RegisterClient(bool isUpdating)
        {
            //    var Result = await _clientsService.RegisterClient(client, isUpdating);
            //    return BuildResponse(Result, HttpStatusCode.OK);
            StringValues Client = default(string);
            
            _httpContext.Request.Form.TryGetValue("clientDetail", out Client);
            if (Client.Count > 0 )
            {
                Organization client = JsonConvert.DeserializeObject<Organization>(Client);
                IFormFileCollection files = _httpContext.Request.Form.Files;
                var Result = await _clientsService.RegisterClient(client, files, isUpdating);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            else
            {
                return BuildResponse(this.responseMessage, HttpStatusCode.BadRequest);
            }
        }

        [HttpPost("GetClients")]
        public ApiResponse GetClients(FilterModel filterModel)
        {
            var Result = _clientsService.GetClients(filterModel);
            return BuildResponse(Result, HttpStatusCode.OK);
        }

        [HttpDelete("DeactivateClient")]
        public ApiResponse DeactivateClient(Employee employee)
        {
            var result = _clientsService.DeactivateClient(employee);
            return BuildResponse(result);
        }
    }
}
