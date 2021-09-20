using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Expiration;

namespace OSR4Rights.Web.BackgroundServices
{
    // todo make into a Background Service?
    public class TusExpiredFilesCleanupService : IHostedService, IDisposable
    {
        private readonly ITusExpirationStore _expirationStore;
        private readonly ExpirationBase _expiration;
        private readonly ILogger<TusExpiredFilesCleanupService> _logger;
        private Timer _timer;

        public TusExpiredFilesCleanupService(ILogger<TusExpiredFilesCleanupService> logger, DefaultTusConfiguration config)
        {
            _logger = logger;
            _expirationStore = (ITusExpirationStore)config.Store;
            _expiration = config.Expiration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_expiration == null)
            {
                _logger.LogInformation("Not running cleanup job as no expiration has been set.");
                return;
            }

            await RunCleanup(cancellationToken);
            _timer = new Timer(async (e) => await RunCleanup((CancellationToken)e), cancellationToken, TimeSpan.Zero, _expiration.Timeout);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task RunCleanup(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Running cleanup job...");
                var numberOfRemovedFiles = await _expirationStore.RemoveExpiredFilesAsync(cancellationToken);
                _logger.LogInformation($"Removed {numberOfRemovedFiles} expired files. Scheduled to run again in {_expiration.Timeout.TotalMilliseconds} ms");
            }
            catch (Exception exc)
            {
                _logger.LogWarning("Failed to run cleanup job: " + exc.Message);
            }
        }
    }

}
