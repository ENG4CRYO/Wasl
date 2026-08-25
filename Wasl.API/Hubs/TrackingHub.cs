using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wasl.Application.Interfaces;
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
        private readonly IActiveRideReader _activeRideReader;
        private readonly IDriverNotificationService _notificationService;


        public TrackingHub(IRedisCacheService redisCache,
            ICacheService cacheService,
            IDriverSessionCacheService sessionCache,
            IActiveRideReader activeRideReader,
            IDriverNotificationService notificationService)
        {
            _redisCache = redisCache;
            _cacheService = cacheService;
            _sessionCache = sessionCache;
            _activeRideReader = activeRideReader;
            _notificationService = notificationService;
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

        /// <summary>
        /// Rejoins the Ride_{rideId} group after a reconnection and immediately pushes
        /// the authoritative ride snapshot (RideStatusSync) to the caller.
        /// Only participants (Rider or assigned Driver) of the ride are allowed.
        /// </summary>
        public async Task ReconnectToRide(string rideId)
        {
            var userId = Context.User?.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(rideId, out var parsedRideId))
            {
                throw new HubException("Invalid ride identifier.");
            }

            var snapshot = await _activeRideReader.GetRideIfParticipantAsync(userId, parsedRideId);

            if (snapshot == null)
            {
                throw new HubException("You are not a participant in this ride.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Ride_{parsedRideId}");

            await _notificationService.SendRideStatusSyncAsync(userId, snapshot);
        }

        public override async Task OnConnectedAsync()
        {

            bool isRider = Context.User?.IsInRole(AspRoles.Rider) ?? false;
            var userId = Context.User?.FindFirst("uid")?.Value;

            if (isRider)
            {
                // Push the current ride state so a reconnecting/cold-start rider recovers instantly.
                if (!string.IsNullOrEmpty(userId))
                {
                    var riderSnapshot = await _activeRideReader.GetActiveRideForUserAsync(userId);
                    if (riderSnapshot != null)
                    {
                        await _notificationService.SendRideStatusSyncAsync(userId, riderSnapshot);
                    }
                }

                await base.OnConnectedAsync();
                return;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Context.Abort();
                return;
            }

            var driverId = userId;

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

            // Push the current ride state so a reconnecting/cold-start driver recovers instantly.
            var driverSnapshot = await _activeRideReader.GetActiveRideForUserAsync(driverId);
            if (driverSnapshot != null)
            {
                await _notificationService.SendRideStatusSyncAsync(driverId, driverSnapshot);
            }

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
                    // Notify the ride group that the driver lost connectivity.
                    // This does NOT change the ride business state (Accepted/Arrived/InProgress stay as-is).
                    var activeRideId = await _activeRideReader.GetActiveRideIdForDriverAsync(driverId);
                    if (activeRideId.HasValue)
                    {
                        await _notificationService.NotifyRideGroupDriverDisconnectedAsync(activeRideId.Value);
                    }

                    await _redisCache.RemoveDriverLocationAsync(driverId);
                    await _sessionCache.HandleDisconnectionAsync(driverId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
