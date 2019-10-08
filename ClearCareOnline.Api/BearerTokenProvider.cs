using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ClearCareOnline.Api.Extensions;
using ClearCareOnline.Api.Models;
using LazyCache;
using Microsoft.Extensions.Configuration;

namespace ClearCareOnline.Api
{
    public class BearerTokenProvider : IBearerTokenProvider
    {
        private const string _cacheKeyPrefix = "_BearerTokenProvider_";
        
        private readonly CacheControlHeaderValue _noCacheControlHeaderValue = new CacheControlHeaderValue {NoCache = true};
        private readonly IAppCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _tokenUrl;
        
        public BearerTokenProvider(IAppCache cache, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _tokenUrl = configuration["ClearCareTokenUrl"];
        }
        
        public async Task<string> RetrieveToken()
        {
            // note: the bearer tokens coming back from api.clearcareonline.com expire after 1 year, so instead of having our cache expire in a year
            //       hard coding to 1 month out.
            var bearerToken = await _cache.GetOrAddAsync($"{_cacheKeyPrefix}bearerToken", GetBearerToken, DateTimeOffset.Now.AddMonths(1));
            return bearerToken.access_token;
        }

        private async Task<BearerTokenResponse> GetBearerToken()
        {
            // todo: remove the hard coding and retrieve from environment
            var clientId = "";
            var clientSecret = "";
            var username = "";
            var password = "";
            var bodyContent = $"grant_type=password&client_id={clientId}&client_secret={clientSecret}&username={username}&password={password}";
            var client = _httpClientFactory.CreateClient("BearerTokenHttpClient");
            var responseMessage = await client.HttpPost(_tokenUrl, bodyContent, _noCacheControlHeaderValue);
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error retrieving bearer token from {_tokenUrl}.");
            }

            var json = await responseMessage.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<BearerTokenResponse>(json);
            return tokenResponse;
        }
    }
}