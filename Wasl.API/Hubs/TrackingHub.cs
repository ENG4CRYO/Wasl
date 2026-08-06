using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Constants;
using Wasl.Core.Entities;
using Wasl.Core.Enums;

namespace Wasl.API.Hubs
{
    [Authorize(Roles = $"{AspRoles.Driver},{AspRoles.Rider}")]
    public class TrackingHub : Hub
    {
        private readonly IRedisCacheService _redisCache;
        private readonly ICacheService _cacheService;
        private readonly IDriverSessionCacheService _sessionCache;


        public TrackingHub(IRedisCacheService redisCache,
            ICacheService cacheService,
            IDriverSessionCacheService sessionCache)
        {
            _redisCache = redisCache;
            _cacheService = cacheService;
            _sessionCache = sessionCache;
        }

        public async Task UpdateLocation(double latitude, double longitude, string? rideId = null)
        {
            var driverId = Context.User?.FindFirst("uid")?.Value;

            if (!string.IsNullOrEmpty(driverId))
            {
                var status = await _cacheService.GetAsync<DriverApprovalStatus>($"DriverStatus:{driverId}", CancellationToken.None);


                if (status == 0)
                {
                    var dbContext = Context.GetHttpContext()?.RequestServices.GetService<IApplicationDbContext>();
                    var driver = await dbContext.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == driverId);

                    if (driver != null)
                    {
                        status = driver.ApprovalStatus;
                        await _cacheService.SetAsync($"DriverStatus:{driverId}", status, TimeSpan.FromHours(24), CancellationToken.None);
                    }
                }

                if (status == DriverApprovalStatus.Approved)
                {
                    await _redisCache.UpdateDriverLocationAsync(driverId, longitude, latitude);
                    await _cacheService.SetAsync($"DriverStatus:{driverId}", status, TimeSpan.FromHours(24), CancellationToken.None);

                    if (!string.IsNullOrWhiteSpace(rideId))
                    {
                        await Clients.Group($"Ride_{rideId}").SendAsync("ReceiveDriverLocation", latitude, longitude);
                    }
                }
                else
                {

                    Context.Abort();
                }
            }
        }
        public async Task TrackRide(string rideId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Ride_{rideId}");
        }
        public override async Task OnConnectedAsync()
        {

            bool isRider = Context.User?.IsInRole(AspRoles.Rider) ?? false;
            if (isRider)
            {
                await base.OnConnectedAsync();
                return;
            }

            var driverId = Context.User?.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(driverId))
            {
                Context.Abort();
                return;
            }

            var status = await _cacheService.GetAsync<DriverApprovalStatus>($"DriverStatus:{driverId}", CancellationToken.None);

            if (status == 0)
            {
                var dbContext = Context.GetHttpContext()?.RequestServices.GetService<IApplicationDbContext>();

                var driver = await dbContext.DriverProfiles
                    .FirstOrDefaultAsync(d => d.UserId == driverId);

                if (driver != null)
                {
                    status = driver.ApprovalStatus;
                    await _cacheService.SetAsync($"DriverStatus:{driverId}", status, TimeSpan.FromHours(24), CancellationToken.None);
                }
            }

            if (status != DriverApprovalStatus.Approved)
            {
                Console.WriteLine($"[SIGNALR] Aborting: Driver status is {status}, which is not Approved.");
                Context.Abort();
                return;
            }

            await _sessionCache.HandleConnectionAsync(driverId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            bool isDriver = Context.User?.IsInRole(AspRoles.Driver) ?? false;

            if (isDriver)
            {
                var driverId = Context.User?.FindFirst("uid")?.Value;
                if (!string.IsNullOrEmpty(driverId))
                {
                    await _redisCache.RemoveDriverLocationAsync(driverId);
                    await _sessionCache.HandleDisconnectionAsync(driverId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
