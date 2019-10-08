using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ClearCareOnline.Api.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rosetta.Tests.Stubs;

namespace Rosetta.Tests.ClearCareOnline.Api.Extensions
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ExtensionTests
    {
        [TestMethod]
        public void DoNotAddCacheControl()
        {
            // ARRANGE
            var request = new HttpRequestMessage();

            // ACT
            request.AddCacheControl();

            // ASSERT
            Assert.IsTrue(request.Headers.CacheControl == null);
        }

        [TestMethod]
        public void AddCacheControl()
        {
            // ARRANGE
            var request = new HttpRequestMessage();

            // ACT
            request.AddCacheControl(new CacheControlHeaderValue {NoCache = true});

            // ASSERT
            Assert.IsTrue(request.Headers.CacheControl.NoCache);
        }

        [TestMethod]
        public void HttpGet_WithoutToken()
        {
            // ARRANGE
            var json = "{\"status\":\"active\"}";
            var clientHandlerStub = new DelegatingHandlerStub((request, cancellationToken) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8)
                    };
                    return Task.FromResult(response);
                }
            );
            var unitUnderTest = new HttpClient(clientHandlerStub);

            // ACT
            var results = unitUnderTest.HttpGet("http://test.url");

            // ASSERT
            Assert.IsNotNull(results);
        }

        [TestMethod]
        public void HttpGet_WithToken()
        {
            // ARRANGE
            var json = "{\"status\":\"active\"}";
            var clientHandlerStub = new DelegatingHandlerStub((request, cancellationToken) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8)
                    };
                    return Task.FromResult(response);
                }
            );
            var unitUnderTest = new HttpClient(clientHandlerStub);

            // ACT
            var results = unitUnderTest.HttpGet("http://test.url", "token");

            // ASSERT
            Assert.IsNotNull(results);
        }

        [TestMethod]
        public void HttpPost()
        {
            // ARRANGE
            var json = "{\"status\":\"active\"}";
            var clientHandlerStub = new DelegatingHandlerStub((request, cancellationToken) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8)
                    };
                    return Task.FromResult(response);
                }
            );
            var unitUnderTest = new HttpClient(clientHandlerStub);

            // ACT
            var results = unitUnderTest.HttpPost("http://test.url", "bodyContent");

            // ASSERT
            Assert.IsNotNull(results);
        }
    }
}