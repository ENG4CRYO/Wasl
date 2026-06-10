using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Constants;

namespace Wasl.API.Hubs
{
    [Authorize (Roles = AspRoles.Driver)]
    public class TrackingHub : Hub
    {
        private readonly IRedisCacheService _redisCache;

        public TrackingHub(IRedisCacheService redisCache)
        {
            _redisCache = redisCache;
        }

        public async Task UpdateLocation(double latitude, double longitude)
        {
            var driverId = Context.User?.FindFirst("uid")?.Value;

            if (!string.IsNullOrEmpty(driverId))
            {
                await _redisCache.UpdateDriverLocationAsync(driverId, longitude, latitude);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var driverId = Context.User?.FindFirst("uid")?.Value;

            if (!string.IsNullOrEmpty(driverId))
            {

                await _redisCache.RemoveDriverLocationAsync(driverId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
