using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using ClearCareOnline.Api.Services;
using LazyCache;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rosetta.Models;
using Rosetta.Services;

namespace Rosetta.Tests.Rosetta
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class RosettaStoneServiceTests
    {
        private static TestContext _context;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            _context = context;
            _context.Properties["appCache"] = new CachingService();
        }

        [TestMethod]
        public async Task GetStatus_Success()
        {
            // ARRANGE
            var loggerMock = new Mock<ILogger<RosettaStoneService>>();
            var appCacheMock = new Mock<IAppCache>();
            var agencyMapperMock = new Mock<IMapper<AgencyFranchiseMap>>();
            var ipAddressCaptureServiceMock = new Mock<IIpAddressCaptureService>();
            var keyVaultMock = new Mock<IAzureKeyVaultService>();
            var storageMock = new Mock<IAzureStorageBlobCacheService>();
            var unitUnderTest = new RosettaStoneService(loggerMock.Object, appCacheMock.Object, agencyMapperMock.Object, ipAddressCaptureServiceMock.Object, keyVaultMock.Object, storageMock.Object);

            // ACT
            var result = await unitUnderTest.GetStatus();

            // ASSERT
            Assert.IsInstanceOfType(result, typeof(Status));
            Assert.AreEqual("active", result.status);
        }

        [TestMethod]
        public void ClearCache_Success()
        {
            // ARRANGE
            var loggerMock = new Mock<ILogger<RosettaStoneService>>();
            var appCacheMock = new Mock<IAppCache>();
            var agencyMapperMock = new Mock<IMapper<AgencyFranchiseMap>>();
            var ipAddressCaptureServiceMock = new Mock<IIpAddressCaptureService>();
            var keyVaultMock = new Mock<IAzureKeyVaultService>();
            var storageMock = new Mock<IAzureStorageBlobCacheService>();
            var unitUnderTest = new RosettaStoneService(loggerMock.Object, appCacheMock.Object, agencyMapperMock.Object, ipAddressCaptureServiceMock.Object, keyVaultMock.Object, storageMock.Object);

            var sequence = new MockSequence();
            appCacheMock.InSequence(sequence)
                .Setup(mock => mock.Remove("_RosettaStoneService_agencies"))
                .Verifiable();
            appCacheMock.InSequence(sequence)
                .Setup(mock => mock.Remove("_BearerTokenProvider_bearerToken"))
                .Verifiable();

            // ACT
            unitUnderTest.ClearCache();

            // ASSERT
            appCacheMock.Verify();
        }

        [TestMethod]
        public async Task GetAgencies_Success()
        {
            // ARRANGE
            var loggerMock = new Mock<ILogger<RosettaStoneService>>();
            var appCache = (IAppCache) _context.Properties["appCache"];
            appCache.CacheProvider.Remove("_RosettaStoneService_agencies");

            var agencyMapperMock = new Mock<IMapper<AgencyFranchiseMap>>();
            var ipAddressCaptureServiceMock = new Mock<IIpAddressCaptureService>();
            var keyVaultMock = new Mock<IAzureKeyVaultService>();
            var storageMock = new Mock<IAzureStorageBlobCacheService>();
            var unitUnderTest = new RosettaStoneService(loggerMock.Object, appCache, agencyMapperMock.Object, ipAddressCaptureServiceMock.Object, keyVaultMock.Object, storageMock.Object);

            var mapResults = new List<AgencyFranchiseMap>
            {
                new AgencyFranchiseMap
                {
                    franchise_numbers = new[] {"4", "5", "6"},
                    clear_care_agency = 2
                },
                new AgencyFranchiseMap
                {
                    franchise_numbers = new[] {"1", "2", "3"},
                    clear_care_agency = 1
                }
            };

            agencyMapperMock.Setup(mock => mock.Map())
                .ReturnsAsync(mapResults)
                .Verifiable();

            await unitUnderTest.RefreshCache();

            // ACT
            var results = await unitUnderTest.GetAgencies();

            // ASSERT
            agencyMapperMock.Verify();
            Assert.AreEqual(2, results.Count);
            Assert.IsInstanceOfType(results.Last(), typeof(RosettaAgency));
            Assert.AreEqual(2, results.Last().clear_care_agency);
            Assert.AreEqual(1, results.First().clear_care_agency);
        }

        [TestMethod]
        public async Task GetFranchises_Success()
        {
            // ARRANGE
            var loggerMock = new Mock<ILogger<RosettaStoneService>>();
            var appCache = (IAppCache) _context.Properties["appCache"];
            appCache.CacheProvider.Remove("_RosettaStoneService_agencies");
            var agencyMapperMock = new Mock<IMapper<AgencyFranchiseMap>>();
            var ipAddressCaptureServiceMock = new Mock<IIpAddressCaptureService>();
            var keyVaultMock = new Mock<IAzureKeyVaultService>();
            var storageMock = new Mock<IAzureStorageBlobCacheService>();
            var unitUnderTest = new RosettaStoneService(loggerMock.Object, appCache, agencyMapperMock.Object, ipAddressCaptureServiceMock.Object, keyVaultMock.Object, storageMock.Object);

            var mapResults = new List<AgencyFranchiseMap>
            {
                new AgencyFranchiseMap
                {
                    franchise_numbers = new[] {"4", "5", "6"},
                    clear_care_agency = 2
                },
                new AgencyFranchiseMap
                {
                    franchise_numbers = new[] {"1", "2", "3"},
                    clear_care_agency = 1
                }
            };

            agencyMapperMock.Setup(mock => mock.Map())
                .ReturnsAsync(mapResults)
                .Verifiable();

            await unitUnderTest.RefreshCache();

            // ACT
            var results = await unitUnderTest.GetFranchises();

            // ASSERT
            agencyMapperMock.Verify();
            Assert.AreEqual(6, results.Count);
            Assert.IsInstanceOfType(results.Last(), typeof(RosettaFranchise));
            Assert.AreEqual(2, results.Last().clear_care_agency);
            Assert.AreEqual(1, results.First().clear_care_agency);
        }

        [TestMethod]
        public async Task GetFranchise_Success()
        {
            // ARRANGE
            var loggerMock = new Mock<ILogger<RosettaStoneService>>();
            var appCache = (IAppCache) _context.Properties["appCache"];
            appCache.CacheProvider.Remove("_RosettaStoneService_agencies");
            var agencyMapperMock = new Mock<IMapper<AgencyFranchiseMap>>();
            var ipAddressCaptureServiceMock = new Mock<IIpAddressCaptureService>();
            var keyVaultMock = new Mock<IAzureKeyVaultService>();
            var storageMock = new Mock<IAzureStorageBlobCacheService>();
            var unitUnderTest = new RosettaStoneService(loggerMock.Object, appCache, agencyMapperMock.Object, ipAddressCaptureServiceMock.Object, keyVaultMock.Object, storageMock.Object);

            var mapResults = new List<AgencyFranchiseMap>
            {
                new AgencyFranchiseMap
                {
                    franchise_numbers = new[] {"4", "5", "6"},
                    clear_care_agency = 2
                },
                new AgencyFranchiseMap
                {
                    franchise_numbers = new[] {"1", "2", "3"},
                    clear_care_agency = 1
                }
            };

            agencyMapperMock.Setup(mock => mock.Map())
                .ReturnsAsync(mapResults)
                .Verifiable();

            await unitUnderTest.RefreshCache();

            // ACT
            var result = await unitUnderTest.GetFranchise("4");

            // ASSERT
            agencyMapperMock.Verify();
            Assert.IsInstanceOfType(result, typeof(RosettaFranchise));
            Assert.AreEqual(mapResults.First().clear_care_agency, result.clear_care_agency);
        }
    }
}