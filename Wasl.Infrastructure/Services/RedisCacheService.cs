using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Interfaces.Infrastructure;

namespace Wasl.Infrastructure.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _db;
        private const string ActiveDriversGeoKey = "Tracking:ActiveDrivers";

        public RedisCacheService(IConnectionMultiplexer redisConnection)
        {
            _db = redisConnection.GetDatabase();
        }
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
    }
}
