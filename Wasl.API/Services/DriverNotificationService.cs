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

    public async Task NotifyDriversWithRideRequestAsync(List<string> driverIds,
        Guid rideId, double latitude, double longitude,
        double dropLat, double dropLng)
    {
        await _hubContext.Clients.Users(driverIds)
            .SendAsync("ReceiveRideRequest", new
            {
                RideId = rideId,
                Lat = latitude,
                Lng = longitude,
                DropLat = dropLat,
                DropLng = dropLng,
                Message = "Request a new flight near you!"
            });
    }
}