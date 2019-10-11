using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using ClearCareOnline.Api.Services;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rosetta.Tests.Stubs;

namespace Rosetta.Tests.ClearCareOnline.Api
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class BearerTokenProviderTests
    {
        private static TestContext _context;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            _context = context;
            _context.Properties["appCache"] = new CachingService();
        }

        [TestMethod]
        public async Task RetrieveToken_Success()
        {
            // ARRANGE
            var appCache = (IAppCache) _context.Properties["appCache"];
            var bearerToken = new BearerTokenResponse
            {
                access_token = "token"
            };
            var json = System.Text.Json.JsonSerializer.Serialize(bearerToken);
            var clientHandlerStub = new DelegatingHandlerStub((request, cancellationToken) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8)
                    };
                    return Task.FromResult(response);
                }
            );
            var client = new HttpClient(clientHandlerStub);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(mock => mock.CreateClient(It.IsAny<string>()))
                .Returns(client)
                .Verifiable();

            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(mock => mock[It.IsAny<string>()])
                .Returns("http://test.url")
                .Verifiable();

            var keyVaultMock = new Mock<IAzureKeyVaultService>();

            var unitUnderTest =
                new BearerTokenProvider(appCache, 
                    httpClientFactoryMock.Object,
                    configurationMock.Object,
                    keyVaultMock.Object);

            // ACT
            var result = await unitUnderTest.RetrieveToken();

            // ASSERT
            configurationMock.Verify();
            httpClientFactoryMock.Verify();
            Assert.IsNotNull(result);
            Assert.AreEqual(bearerToken.access_token, result);
        }

        [ExpectedException(typeof(HttpRequestException))]
        [TestMethod]
        public async Task RetrieveToken_NonSuccess()
        {
            // ARRANGE
            var appCache = (IAppCache) _context.Properties["appCache"];
            appCache.CacheProvider.Remove("_BearerTokenProvider_bearerToken");
            var bearerToken = new BearerTokenResponse
            {
                access_token = "token"
            };
            var json = System.Text.Json.JsonSerializer.Serialize(bearerToken);
            var clientHandlerStub = new DelegatingHandlerStub((request, cancellationToken) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent(json, Encoding.UTF8)
                    };
                    return Task.FromResult(response);
                }
            );
            var client = new HttpClient(clientHandlerStub);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(mock => mock.CreateClient(It.IsAny<string>()))
                .Returns(client)
                .Verifiable();

            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(mock => mock[It.IsAny<string>()])
                .Returns("http://test.url")
                .Verifiable();

            var keyVaultMock = new Mock<IAzureKeyVaultService>();

            var unitUnderTest =
                new BearerTokenProvider(appCache, 
                    httpClientFactoryMock.Object,
                    configurationMock.Object,
                    keyVaultMock.Object);

            // ACT
            var _ = await unitUnderTest.RetrieveToken();

            // ASSERT
        }
    }
}