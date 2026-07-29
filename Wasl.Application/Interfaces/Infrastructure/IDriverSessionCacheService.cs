using Wasl.Application.Models.Cache;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IDriverSessionCacheService
    {
        Task HandleConnectionAsync(string driverId);
        Task HandleDisconnectionAsync(string driverId);
        Task<List<DriverSessionCacheModel>> GetExpiredSessionsAsync(int gracePeriodMinutes);
        Task RemoveSessionAsync(string driverId);
    }
}
