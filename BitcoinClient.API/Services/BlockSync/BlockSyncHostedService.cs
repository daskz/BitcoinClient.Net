using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BitcoinClient.API.Services.BlockSync
{
    public class BlockSyncHostedService : IHostedService, IDisposable
    {
        private Timer _timer;
        public IServiceProvider Services { get; }

        public BlockSyncHostedService(IServiceProvider services)
        {
            Services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(async state => await DoWorkAsync(state), null, TimeSpan.Zero, TimeSpan.FromMinutes(2));
            return Task.CompletedTask;
        }

        private async Task DoWorkAsync(object state)
        {
            using (var scope = Services.CreateScope())
            {
                var synchronizer = scope.ServiceProvider.GetRequiredService<IBlockSynchronizer>();
                await synchronizer.Execute();
            }
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
    }
}
