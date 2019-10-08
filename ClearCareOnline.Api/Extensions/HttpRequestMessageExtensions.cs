using System.Net.Http;
using System.Net.Http.Headers;

namespace ClearCareOnline.Api.Extensions
{
    public static class HttpRequestMessageExtensions
    {
        public static void AddCacheControl(this HttpRequestMessage request, CacheControlHeaderValue cacheControlHeaderValue = null)
        {
            if (cacheControlHeaderValue != null)
            {
                request.Headers.CacheControl = cacheControlHeaderValue;
            }
        }
    }
}