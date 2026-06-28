using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wasl.API.Hubs;
using Wasl.API.Resources;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Infrastructure;

namespace Wasl.API.Services;

public class DriverNotificationService : IDriverNotificationService
{
    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DriverNotificationService(IHubContext<TrackingHub> hubContext,
        IStringLocalizer<SharedResource> localizer)
    {
        _hubContext = hubContext;
        _localizer = localizer;
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
                Message = _localizer["SignalRReceiveRideRequest"]
            });
    }

    public async Task NotifyUserRideCancelledAsync(string userId, string message)
    {
        if (string.IsNullOrEmpty(userId)) return;
        await _hubContext.Clients.User(userId).SendAsync("RideCancelled", message);
    }

    public async Task SendProfileReviewedNotificationAsync(string driverId, bool isApproved, string message)
    {
        await _hubContext.Clients.User(driverId).SendAsync("ProfileReviewed", new
        {
            IsApproved = isApproved,
            Message = message
        });
    }

    public async Task NotifyRiderRideAcceptedAsync(string riderId, Guid rideId, string driverId)
    {
        if (string.IsNullOrEmpty(riderId)) return;
        await _hubContext.Clients.User(riderId).SendAsync("RideAccepted", new
        {
            RideId = rideId,
            DriverId = driverId,
            Message = _localizer["Rides.RideAcceptedByDriver"] ?? "تم قبول رحلتك، السائق في الطريق إليك."
        });
    }

    public async Task NotifyRiderDriverArrivedAsync(string riderId, Guid rideId)
    {
        if (string.IsNullOrEmpty(riderId)) return;
        await _hubContext.Clients.User(riderId).SendAsync("DriverArrived", new
        {
            RideId = rideId,
            Message = _localizer["Rides.DriverArrived"] ?? "السائق وصل وهو في انتظارك بالخارج."
        });
    }

    public async Task NotifyRiderRideStartedAsync(string riderId, Guid rideId)
    {
        if (string.IsNullOrEmpty(riderId)) return;
        await _hubContext.Clients.User(riderId).SendAsync("RideStarted", new
        {
            RideId = rideId,
            Message = _localizer["Rides.RideStarted"] ?? "بدأت الرحلة، نتمنى لك طريقاً آمناً."
        });
    }

    public async Task NotifyRiderRideCompletedAsync(string riderId, Guid rideId)
    {
        if (string.IsNullOrEmpty(riderId)) return;
        await _hubContext.Clients.User(riderId).SendAsync("RideCompleted", new
        {
            RideId = rideId,
            Message = _localizer["Rides.RideCompleted"] ?? "انتهت الرحلة، شكراً لاستخدامك وصل."
        });
    }


}