using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Constants;
using Wasl.Core.Enums;

namespace Wasl.API.Hubs
{
    [Authorize (Roles = AspRoles.Driver)]
    public class TrackingHub : Hub
    {
        private readonly IRedisCacheService _redisCache;
        private readonly ICacheService _cacheService;
        public TrackingHub(IRedisCacheService redisCache, ICacheService cacheService)
        {
            _redisCache = redisCache;
            _cacheService = cacheService;
        }

        public async Task UpdateLocation(double latitude, double longitude)
        {
            var driverId = Context.User?.FindFirst("uid")?.Value;

            if (!string.IsNullOrEmpty(driverId))
            {

                var status = await _cacheService.GetAsync<DriverApprovalStatus>($"DriverStatus:{driverId}", CancellationToken.None);

                if (status == DriverApprovalStatus.Approved)
                {
                    await _redisCache.UpdateDriverLocationAsync(driverId, longitude, latitude);
                }
                else
                {
                    Context.Abort();
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            var driverId = Context.User?.FindFirst("uid")?.Value;

            if (!string.IsNullOrEmpty(driverId))
            {

                var status = await _cacheService.GetAsync<DriverApprovalStatus>($"DriverStatus:{driverId}", CancellationToken.None);

                if (status != DriverApprovalStatus.Approved)
                {
                    Context.Abort();
                    return;
                }
            }

            await base.OnConnectedAsync();
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
