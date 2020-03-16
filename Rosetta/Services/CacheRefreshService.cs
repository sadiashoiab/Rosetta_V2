using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rosetta.Services
{
    public class CacheRefreshService: IHostedService, IDisposable
    {
        private readonly ILogger<CacheRefreshService> _logger;
        private readonly IRosettaStoneService _rosettaStoneService;
        private Timer _occurrenceTimer;
        private bool disposed = false;

        public CacheRefreshService(ILogger<CacheRefreshService> logger, IRosettaStoneService rosettaStoneService)
        {
            _logger = logger;
            _rosettaStoneService = rosettaStoneService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var absoluteExpirationInSeconds = await _rosettaStoneService.GetAbsoluteExpiration();
            var occurrenceInSeconds = absoluteExpirationInSeconds / 2;

            if (occurrenceInSeconds > 0)
            {
                _logger.LogInformation($"OccurrenceInSeconds is set to: {occurrenceInSeconds}, Creating CacheRefreshService timer to refresh the cache");
                
                _logger.LogInformation("Loading cache from storage...");
                await _rosettaStoneService.LoadCacheFromStorage();
                _logger.LogInformation("Finished loading cache from storage.");
                
                _occurrenceTimer = new Timer(RefreshCache,
                    null,
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(occurrenceInSeconds));
            }
            else
            {
                _logger.LogError($"CacheRefreshService timer will NOT be created.  OccurrenceInSeconds is set to: {occurrenceInSeconds}");
            }
        }

        private void RefreshCache(object state)
        {
            _logger.LogInformation("CacheRefreshService is refreshing the cache");
            Task.Run(async delegate
                {
                    await _rosettaStoneService.RefreshCache();
                });
            _logger.LogInformation("CacheRefreshService is DONE refreshing the cache");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("CacheRefreshService timer is stopping.");
            _occurrenceTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _occurrenceTimer.Dispose();
                // Free any other managed objects here.
                //
            }

            disposed = true;
        }
    }
}
