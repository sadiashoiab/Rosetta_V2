using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rosetta.Services;

namespace Rosetta.Tests.Rosetta
{
    [TestClass]
    public class CacheRefreshServiceTests
    {
        [TestMethod]
        public async Task CacheRefreshServiceStarts_Success()
        {
            // ARRANGE
            var loggerMock = new Mock<ILogger<CacheRefreshService>>();
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();

            rosettaStoneServiceMock.Setup(mock => mock.GetAbsoluteExpiration()).ReturnsAsync(2).Verifiable();

            var uut = new CacheRefreshService(loggerMock.Object, rosettaStoneServiceMock.Object);
            var cancellationToken = new CancellationToken();

            // ACT
            await uut.StartAsync(cancellationToken);

            // ASSERT
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            rosettaStoneServiceMock.Verify();
            rosettaStoneServiceMock.Verify(mock => mock.RefreshCache());
            loggerMock.Verify(logger => 
                logger.Log(LogLevel.Information, 
                    It.IsAny<EventId>(), 
                    It.Is<It.IsAnyType>((v, _) => v.ToString().Equals("OccurrenceInSeconds is set to: 1, Creating CacheRefreshService timer to refresh the cache")), 
                    It.IsAny<Exception>(), 
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);

            loggerMock.Verify(logger => 
                logger.Log(LogLevel.Information, 
                    It.IsAny<EventId>(), 
                    It.Is<It.IsAnyType>((v, _) => v.ToString().Equals("CacheRefreshService is refreshing the cache")), 
                    It.IsAny<Exception>(), 
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);

            uut.Dispose();
        }

        [TestMethod]
        public async Task CacheRefreshServiceStarts_Error()
        {
            // ARRANGE
            var loggerMock = new Mock<ILogger<CacheRefreshService>>();
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();

            rosettaStoneServiceMock.Setup(mock => mock.GetAbsoluteExpiration()).ReturnsAsync(1).Verifiable();

            var uut = new CacheRefreshService(loggerMock.Object, rosettaStoneServiceMock.Object);
            var cancellationToken = new CancellationToken();

            // ACT
            await uut.StartAsync(cancellationToken);

            // ASSERT
            rosettaStoneServiceMock.Verify();
            loggerMock.Verify(logger => 
                logger.Log(LogLevel.Error, 
                    It.IsAny<EventId>(), 
                    It.Is<It.IsAnyType>((v, _) => v.ToString().Equals("CacheRefreshService timer will NOT be created.  OccurrenceInSeconds is set to: 0")), 
                    It.IsAny<Exception>(), 
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);

            uut.Dispose();
        }

        [TestMethod]
        public async Task CacheRefreshServiceStop_Success()
        {
            // ARRANGE
            var loggerMock = new Mock<ILogger<CacheRefreshService>>();
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();
            var uut = new CacheRefreshService(loggerMock.Object, rosettaStoneServiceMock.Object);
            var cancellationToken = new CancellationToken();

            // ACT
            await uut.StopAsync(cancellationToken);

            // ASSERT
            loggerMock.Verify(logger => 
                logger.Log(LogLevel.Debug, 
                    It.IsAny<EventId>(), 
                    It.Is<It.IsAnyType>((v, _) => v.ToString().Equals("CacheRefreshService timer is stopping.")), 
                    It.IsAny<Exception>(), 
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);

            uut.Dispose();
        }
    }
}
