using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Constants;
using Wasl.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Wasl.API.Hubs
{
    [Authorize (Roles = AspRoles.Driver)]
    public class TrackingHub : Hub
    {
        private readonly IRedisCacheService _redisCache;
        private readonly ICacheService _cacheService;


        public TrackingHub(IRedisCacheService redisCache,
            ICacheService cacheService)
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
