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
        private const string _apiUrl = "https://api.clearcareonline.com";

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            using var client = new HttpClient();
            var responseMessage = await client.GetAsync(_apiUrl, cancellationToken);  
            if (!responseMessage.StatusCode.Equals(HttpStatusCode.Forbidden))  
            {
                var message = $"{_apiUrl} not responding with 403 Forbidden as expected, but responded with StatusCode: {responseMessage.StatusCode}";

                if (responseMessage.Content != null)
                {
                    var responseContent = await responseMessage.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        message += $" Content: {responseContent}";
                    }
                }

                return await Task.FromResult(HealthCheckResult.Unhealthy(description: message));
            }  
            return await Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}