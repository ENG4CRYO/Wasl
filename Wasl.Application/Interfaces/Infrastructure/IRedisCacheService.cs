using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IRedisCacheService
    {
        Task UpdateDriverLocationAsync(string driverId, double longitude, double latitude);
        Task RemoveDriverLocationAsync(string driverId);
        Task<(double Longitude, double Latitude)?> GetDriverLocationAsync(string driverId);
        Task<IEnumerable<string>> GetNearbyDriversAsync(double longitude, double latitude, double radiusInKm);
        Task<bool> AcquireRideLockAsync(Guid rideId, string driverId);
        Task ReleaseRideLockAsync(Guid rideId);
        Task<List<string>> GetExcludedDriversForRideAsync(Guid rideId);
        Task AddExcludedDriversToRideAsync(Guid rideId, List<string> driverIds);
    }
}
