using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Rosetta.Tests.ClearCareOnline.Api
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class AgencyMapperTests
    {
        [TestMethod]
        public async Task Map_NoResults_Success()
        {
            // ARRANGE
            var configurationMock = new Mock<IConfiguration>();
            var resourceLoaderMock = new Mock<IResourceLoader>();
            var unitUnderTest = new AgencyMapper(configurationMock.Object, resourceLoaderMock.Object);

            resourceLoaderMock.Setup(mock => mock.LoadAsync<AgencyResponse>(It.IsAny<string>()))
                .ReturnsAsync(new List<AgencyResponse>())
                .Verifiable();

            // ACT
            var agencies = await unitUnderTest.Map();

            // ASSERT
            resourceLoaderMock.Verify();
            Assert.AreEqual(expected: 0, agencies.Count);
        }

        [TestMethod]
        public async Task Map_NoLocationResults_Success()
        {
            // ARRANGE
            var configurationMock = new Mock<IConfiguration>();
            var resourceLoaderMock = new Mock<IResourceLoader>();
            var unitUnderTest = new AgencyMapper(configurationMock.Object, resourceLoaderMock.Object);
            var expected = new List<AgencyResponse>
            {
                new AgencyResponse
                {
                    id = 4321,
                    subdomain = "hisc1234"
                }
            };

            resourceLoaderMock.Setup(mock => mock.LoadAsync<AgencyResponse>(It.IsAny<string>()))
                .ReturnsAsync(expected)
                .Verifiable();

            // ACT
            var results = await unitUnderTest.Map();

            // ASSERT
            resourceLoaderMock.Verify();
            Assert.AreEqual(expected: expected.Count, results.Count);
            Assert.AreEqual(expected.First().id, results.First().clear_care_agency);
            Assert.IsTrue(results.First().franchise_numbers.First().EndsWith("1234"));
        }

        [TestMethod]
        public async Task Map_LocationResults_Success()
        {
            // ARRANGE
            var configurationMock = new Mock<IConfiguration>();
            var resourceLoaderMock = new Mock<IResourceLoader>();
            var unitUnderTest = new AgencyMapper(configurationMock.Object, resourceLoaderMock.Object);
            var expected = new List<AgencyResponse>
            {
                new AgencyResponse
                {
                    id = 4321,
                    subdomain = "hisc1234",
                    locations = "fake location"
                }
            };

            var expectedLocations = new List<LocationResponse>
            {
                new LocationResponse
                {
                    name = "5678",
                },
                new LocationResponse
                {
                    name = "5678 West",
                }
            };

            resourceLoaderMock.Setup(mock => mock.LoadAsync<AgencyResponse>(It.IsAny<string>()))
                .ReturnsAsync(expected)
                .Verifiable();

            resourceLoaderMock.Setup(mock => mock.LoadAsync<LocationResponse>(It.IsAny<string>()))
                .ReturnsAsync(expectedLocations)
                .Verifiable();

            // ACT
            var results = await unitUnderTest.Map();

            // ASSERT
            resourceLoaderMock.Verify();
            Assert.AreEqual(expected: expected.Count, results.Count);
            Assert.AreEqual(expected.First().id, results.First().clear_care_agency);
            Assert.IsTrue(results.First().franchise_numbers.First().EndsWith("1234"));
            Assert.AreEqual(expectedLocations.First().name, results.First().franchise_numbers.Last());
        }
    }
}
