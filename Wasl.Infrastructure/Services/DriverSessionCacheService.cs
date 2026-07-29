using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Models.Cache;

namespace Wasl.Infrastructure.Services
{
    public class DriverSessionCacheService : IDriverSessionCacheService
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<DriverSessionCacheService> _logger;
        private const string SessionKeyPrefix = "DriverSession:";
        private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(24);

        public DriverSessionCacheService(IConnectionMultiplexer redis, ILogger<DriverSessionCacheService> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task HandleConnectionAsync(string driverId)
        {
            var key = $"{SessionKeyPrefix}{driverId}";
            var existing = await _db.StringGetAsync(key);

            if (existing.HasValue && !existing.IsNullOrEmpty)
            {
                var session = JsonSerializer.Deserialize<DriverSessionCacheModel>(existing.ToString());
                if (session != null)
                {
                    session.DisconnectedAt = null;
                    await _db.StringSetAsync(key, JsonSerializer.Serialize(session), SessionTtl);
                    _logger.LogInformation("Driver {DriverId} reconnected, cleared DisconnectedAt", driverId);
                    return;
                }
            }

            var newSession = new DriverSessionCacheModel
            {
                DriverId = driverId,
                StartTime = DateTime.UtcNow,
                DisconnectedAt = null
            };

            await _db.StringSetAsync(key, JsonSerializer.Serialize(newSession), SessionTtl);
            _logger.LogInformation("Driver {DriverId} connected, new session started at {StartTime}", driverId, newSession.StartTime);
        }

        public async Task HandleDisconnectionAsync(string driverId)
        {
            var key = $"{SessionKeyPrefix}{driverId}";
            var existing = await _db.StringGetAsync(key);

            if (existing.HasValue && !existing.IsNullOrEmpty)
            {
                var session = JsonSerializer.Deserialize<DriverSessionCacheModel>(existing.ToString());
                if (session != null)
                {
                    session.DisconnectedAt = DateTime.UtcNow;
                    await _db.StringSetAsync(key, JsonSerializer.Serialize(session), SessionTtl);
                    _logger.LogInformation("Driver {DriverId} disconnected at {DisconnectedAt}", driverId, session.DisconnectedAt);
                }
            }
        }

        public async Task<List<DriverSessionCacheModel>> GetExpiredSessionsAsync(int gracePeriodMinutes)
        {
            var expired = new List<DriverSessionCacheModel>();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var now = DateTime.UtcNow;

            await foreach (var key in server.KeysAsync(pattern: $"{SessionKeyPrefix}*"))
            {
                var value = await _db.StringGetAsync(key);
                if (!value.HasValue || value.IsNullOrEmpty)
                    continue;

                var session = JsonSerializer.Deserialize<DriverSessionCacheModel>(value.ToString());
                if (session?.DisconnectedAt == null)
                    continue;

                if ((now - session.DisconnectedAt.Value).TotalMinutes > gracePeriodMinutes)
                {
                    expired.Add(session);
                }
            }

            return expired;
        }

        public async Task RemoveSessionAsync(string driverId)
        {
            var key = $"{SessionKeyPrefix}{driverId}";
            await _db.KeyDeleteAsync(key);
            _logger.LogInformation("Removed session for driver {DriverId}", driverId);
        }
    }
}
