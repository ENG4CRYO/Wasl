using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Entities;

namespace Wasl.Infrastructure.BackgroundJobs
{
    public class DriverSessionSyncHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDriverSessionCacheService _cacheService;
        private readonly ILogger<DriverSessionSyncHostedService> _logger;

        public DriverSessionSyncHostedService(
            IServiceScopeFactory scopeFactory,
            IDriverSessionCacheService cacheService,
            ILogger<DriverSessionSyncHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _cacheService = cacheService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DriverSessionSyncHostedService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    var expiredSessions = await _cacheService.GetExpiredSessionsAsync(gracePeriodMinutes: 3);

                    if (expiredSessions.Count == 0)
                        continue;

                    foreach (var session in expiredSessions)
                    {
                        var endTime = session.DisconnectedAt ?? session.StartTime;
                        var durationMinutes = Math.Max(0, (int)(endTime - session.StartTime).TotalMinutes);

                        var log = new DriverOnlineLog
                        {
                            Id = Guid.NewGuid(),
                            DriverId = session.DriverId,
                            StartTime = session.StartTime,
                            EndTime = endTime,
                            DurationMinutes = durationMinutes
                        };

                        dbContext.DriverOnlineLogs.Add(log);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);

                    foreach (var session in expiredSessions)
                    {
                        await _cacheService.RemoveSessionAsync(session.DriverId);
                    }

                    _logger.LogInformation("Synced {Count} expired driver sessions to database", expiredSessions.Count);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while syncing driver sessions");
                }
            }
        }
    }
}
