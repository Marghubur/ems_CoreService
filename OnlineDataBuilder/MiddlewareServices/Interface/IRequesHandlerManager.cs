using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Education.MiddlewareServices.Interface
{
    public interface IRequesHandlerManager
    {
        Task HandleRequest(HttpContext context, RequestDelegate next, IConfiguration configuration);
    }
}
