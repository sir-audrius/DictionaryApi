using System;
using System.Threading;
using System.Threading.Tasks;
using DictionaryApi.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DictionaryApi.Storage
{
    public class CleanupService : IHostedService, IDisposable
    {
        private readonly ExpirationConfig _expirationConfig;
        private readonly IValueStorage _valueStorage;
        private Timer _timer;

        public CleanupService(IValueStorage valueStorage, IOptions<ExpirationConfig> expirationConfig)
        {
            _valueStorage = valueStorage;
            _expirationConfig = expirationConfig.Value;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ExecuteCleanup, null, TimeSpan.FromSeconds(_expirationConfig.CleanupPeriodInSeconds),
                TimeSpan.FromSeconds(_expirationConfig.CleanupPeriodInSeconds));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void ExecuteCleanup(object state)
        {
            _valueStorage.Cleanup();
        }
    }
}