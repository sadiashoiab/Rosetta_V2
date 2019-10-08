using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ClearCareOnline.Api.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> HttpPost(this HttpClient client, string url, string bodyContent, CacheControlHeaderValue cacheControlHeaderValue = null)
        {
            var content = new StringContent(bodyContent);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.AddCacheControl(cacheControlHeaderValue);
            requestMessage.Content = content;
            
            var responseMessage = await client.SendAsync(requestMessage);
            return responseMessage;
        }

        public static async Task<HttpResponseMessage> HttpGet(this HttpClient client, string url, CacheControlHeaderValue cacheControlHeaderValue = null)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.AddCacheControl(cacheControlHeaderValue);
            var responseMessage = await client.SendAsync(requestMessage);
            return responseMessage;
        }

        public static async Task<HttpResponseMessage> HttpGet(this HttpClient client, string url, string bearerToken, CacheControlHeaderValue cacheControlHeaderValue = null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            return await client.HttpGet(url, cacheControlHeaderValue);
        }
    }
}