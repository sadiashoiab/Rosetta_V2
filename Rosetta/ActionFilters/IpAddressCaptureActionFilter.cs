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
            var xForwardForHeader = context.HttpContext.Request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrWhiteSpace(xForwardForHeader))
            {
                ParseXForwardForAndAdd(xForwardForHeader);
            }

            _service.Add(context.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString());
        }

        private void ParseXForwardForAndAdd(string xForwardFor)
        {
            var parts = xForwardFor.Split(':');
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts.First()))
            {
                _service.Add(parts.First());
            }
        }
    }
}