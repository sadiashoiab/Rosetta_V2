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
                    It.IsAny<It.IsAnyType>(), 
                    It.IsAny<Exception>(), 
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeast(2));

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
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            rosettaStoneServiceMock.Verify();
            loggerMock.Verify(logger => 
                logger.Log(LogLevel.Error, 
                    It.IsAny<EventId>(), 
                    It.IsAny<It.IsAnyType>(), 
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
                    It.IsAny<It.IsAnyType>(), 
                    It.IsAny<Exception>(), 
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);

            uut.Dispose();
        }
    }
}
