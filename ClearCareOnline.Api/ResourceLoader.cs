using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ClearCareOnline.Api.Extensions;
using ClearCareOnline.Api.Models;

namespace ClearCareOnline.Api
{
    public class ResourceLoader : IResourceLoader
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBearerTokenProvider _bearerTokenProvider;

        public ResourceLoader(IHttpClientFactory httpClientFactory, IBearerTokenProvider bearerTokenProvider)
        {
            _httpClientFactory = httpClientFactory;
            _bearerTokenProvider = bearerTokenProvider;
        }

        public async Task<IList<T>> LoadAsync<T>(string url)
        {
            var resource = new List<T>();
            
            do
            {
                var response = await RetrieveResources<T>(url);
                url = response.next;
                resource.AddRange(response.results);
            } while (!string.IsNullOrWhiteSpace(url));

            return resource;
        }

        private async Task<ResourceResponse<T>> RetrieveResources<T>(string url)
        {
            var bearerToken = await _bearerTokenProvider.RetrieveToken();
            var client = _httpClientFactory.CreateClient("ClearCareHttpClient");
            var responseMessage = await client.HttpGet(url, bearerToken, new CacheControlHeaderValue {NoCache = true});
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error retrieving resources from url: {url} with type: {typeof(T)} and status: {responseMessage.StatusCode}.");
            }

            var json = await responseMessage.Content.ReadAsStringAsync();
            var response = System.Text.Json.JsonSerializer.Deserialize<ResourceResponse<T>>(json);
            return response;
        }
    }
}