using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rosetta.Tests.Stubs;

namespace Rosetta.Tests.ClearCareOnline.Api
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ResourceLoaderTests
    {
        [TestMethod]
        public async Task LoadAsync_Success()
        {
            // ARRANGE
            var bearerTokenProviderMock = new Mock<IBearerTokenProvider>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var unitUnderTest = new ResourceLoader(httpClientFactoryMock.Object, bearerTokenProviderMock.Object);

            var expected = new List<AgencyResponse>
            {
                new AgencyResponse
                {
                    id = 4321,
                    subdomain = "hisc1234"
                }
            };
            var resourceResponse = new ResourceResponse<AgencyResponse>
            {
                results = expected,
                count = 1,
                next = "",
                previous = ""
            };
            var json = System.Text.Json.JsonSerializer.Serialize(resourceResponse);
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
            var url = "http://test.url";

            bearerTokenProviderMock.Setup(mock => mock.RetrieveToken())
                .ReturnsAsync("token")
                .Verifiable();

            httpClientFactoryMock.Setup(mock => mock.CreateClient(It.IsAny<string>()))
                .Returns(client)
                .Verifiable();

            // ACT
            var results = await unitUnderTest.LoadAsync<AgencyResponse>(url);

            // ASSERT
            bearerTokenProviderMock.Verify();
            httpClientFactoryMock.Verify();
            Assert.AreEqual(1, results.Count);
        }
    }
}