using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Interfaces.Infrastructure;

namespace Wasl.Infrastructure.Services
{
    public class RedisCacheService : IRedisCacheService, ICacheService
    {
        private readonly IDatabase _db;
        private const string ActiveDriversGeoKey = "Tracking:ActiveDrivers";

        public RedisCacheService(IConnectionMultiplexer redisConnection)
        {
            _db = redisConnection.GetDatabase();
        }

        #region IRedisCacheService Implementation (Tracking & Locks)

        public async Task<IEnumerable<string>> GetNearbyDriversAsync(double longitude, double latitude, double radiusInKm)
        {
            var results = await _db.GeoRadiusAsync(
                key: ActiveDriversGeoKey,
                longitude: longitude,
                latitude: latitude,
                radius: radiusInKm,
                unit: GeoUnit.Kilometers,
                count: -1,
                order: Order.Ascending);

            return results.Select(x => x.Member.ToString());
        }

        public async Task RemoveDriverLocationAsync(string driverId)
        {
            await _db.GeoRemoveAsync(ActiveDriversGeoKey, driverId);
        }

        public async Task UpdateDriverLocationAsync(string driverId, double longitude, double latitude)
        {
            await _db.GeoAddAsync(ActiveDriversGeoKey, longitude, latitude, driverId);
        }

        public async Task<bool> AcquireRideLockAsync(Guid rideId, string driverId)
        {
            var lockKey = $"RideLock:{rideId}";
            return await _db.StringSetAsync(lockKey, driverId, TimeSpan.FromMinutes(5), When.NotExists);
        }
        public async Task ReleaseRideLockAsync(Guid rideId)
        {
            var lockKey = $"RideLock:{rideId}";
            await _db.KeyDeleteAsync(lockKey);
        }

        #endregion

        #region ICacheService Implementation (OTP & Sessions)

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var value = await _db.StringGetAsync(key);

            if (!value.HasValue || value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString());
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan experationTime, CancellationToken cancellationToken = default)
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, serializedValue, experationTime);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _db.KeyDeleteAsync(key);
        }

        #endregion

        public async Task<List<string>> GetExcludedDriversForRideAsync(Guid rideId)
        {
            var key = $"ride:{rideId}:excluded";

            var members = await _db.SetMembersAsync(key);

            if (members == null || members.Length == 0)
                return new List<string>();

            return members.Select(m => m.ToString()).ToList();
        }


        public async Task AddExcludedDriversToRideAsync(Guid rideId, List<string> driverIds)
        {
            if (driverIds == null || !driverIds.Any()) return;

            var key = $"ride:{rideId}:excluded";

            var redisValues = driverIds.Select(id => (RedisValue)id).ToArray();

            await _db.SetAddAsync(key, redisValues);

        
            await _db.KeyExpireAsync(key, TimeSpan.FromMinutes(10));
        }
    }
}