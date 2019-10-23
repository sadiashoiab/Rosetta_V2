using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Rosetta.Services;

namespace Rosetta.ActionFilters
{
    [ExcludeFromCodeCoverage]
    public class IpAddressCaptureActionFilter : ActionFilterAttribute
    {
        private readonly IIpAddressCaptureService _service;

        public IpAddressCaptureActionFilter(IIpAddressCaptureService service)
        {
            _service = service;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var keys = context.HttpContext.Request.Headers.Keys.ToArray();
            var allHeaders = new List<string>();
            foreach (var key in keys)
            {
                var value = context.HttpContext.Request.Headers[key];
                allHeaders.Add($"{key}:{value}");
            }

            var headers = string.Join(',', allHeaders);
            _service.Add(headers);
            _service.Add(context.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString());
        }
    }
}