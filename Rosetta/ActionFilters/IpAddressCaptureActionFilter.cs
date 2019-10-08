using System.Diagnostics.CodeAnalysis;
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
            _service.Add(context.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString());
        }
    }
}