using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wasl.API.Hubs;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Infrastructure;

namespace Wasl.API.Services;

public class DriverNotificationService : IDriverNotificationService
{
    private readonly IHubContext<TrackingHub> _hubContext;

    public DriverNotificationService(IHubContext<TrackingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task HideRideRequestFromDriversAsync(List<string> driverIds, Guid rideId)
    {
        if (driverIds == null || driverIds.Count == 0) return;

        await _hubContext.Clients.Users(driverIds).SendAsync("HideRideRequest", rideId.ToString());
    }

    public async Task NotifyDriversWithRideRequestAsync(List<string> driverIds,
        Guid rideId, double latitude, double longitude,
        double dropLat, double dropLng, decimal price)
    {
        await _hubContext.Clients.Users(driverIds)
            .SendAsync("ReceiveRideRequest", new
            {
                RideId = rideId,
                Lat = latitude,
                Lng = longitude,
                DropLat = dropLat,
                DropLng = dropLng,
                calculatedPrice = price,
                Message = "A new ride request is near you!"
            });
    }

    public async Task NotifyUserRideCancelledAsync(string userId, string message)
    {
        if (string.IsNullOrEmpty(userId)) return;
        await _hubContext.Clients.User(userId).SendAsync("RideCancelled", message);
    }
}