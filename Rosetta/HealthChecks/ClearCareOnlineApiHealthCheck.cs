using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Rosetta.HealthChecks
{
    [ExcludeFromCodeCoverage]
    public class ClearCareOnlineApiHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("https://api.clearcareonline.com", cancellationToken);  
            if (!response.StatusCode.Equals(HttpStatusCode.Forbidden))  
            {  
                return await Task.FromResult(HealthCheckResult.Unhealthy(description: "https://api.clearcareonline.com not responding with 403 Forbidden as expected."));
            }  
            return await Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}