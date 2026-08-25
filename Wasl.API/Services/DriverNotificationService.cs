using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wasl.API.Hubs;
using Wasl.API.Resources;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Features.Rides.Commands.AcceptRide;
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
        double dropLat, double dropLng, decimal price,
        string paymentMethod, string riderName, string riderPhone)
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
                PaymentMethod = paymentMethod,
                RiderName = riderName,
                RiderPhone = riderPhone,
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

    public async Task NotifyRiderRideAcceptedAsync(string riderId, DriverRideAcceptedInfoDto info)
    {
        if (string.IsNullOrEmpty(riderId)) return;
        await _hubContext.Clients.User(riderId).SendAsync("RideAccepted", new
        {
            info.RideId,
            info.DriverId,
            info.DriverName,
            info.DriverProfilePictureUrl,
            info.VehicleModel,
            info.VehicleYear,
            info.VinNumber,
            info.PhoneNumber,
            info.DriverLatitude,
            info.DriverLongitude,
            Message = info.Message
        });
    }

    public async Task NotifyRiderDriverArrivedAsync(string riderId, Guid rideId)
    {
        if (string.IsNullOrEmpty(riderId)) return;
        await _hubContext.Clients.User(riderId).SendAsync("DriverArrived", new
        {
            RideId = rideId,
            Message = _localizer["Rides.DriverArrived"] 
        });
    }

    public async Task NotifyRiderRideStartedAsync(string riderId, Guid rideId)
    {
        if (string.IsNullOrEmpty(riderId)) return;
        await _hubContext.Clients.User(riderId).SendAsync("RideStarted", new
        {
            RideId = rideId,
            Message = _localizer["Rides.RideStarted"] 
        });
    }

    public async Task NotifyRiderRideCompletedAsync(string riderId, Guid rideId)
    {
        if (string.IsNullOrEmpty(riderId)) return;
        await _hubContext.Clients.User(riderId).SendAsync("RideCompleted", new
        {
            RideId = rideId,
            Message = _localizer["Rides.RideCompleted"] 
        });
    }

    public async Task NotifyRideGroupDriverDisconnectedAsync(Guid rideId)
    {
        await _hubContext.Clients.Group($"Ride_{rideId}").SendAsync("DriverDisconnected", new
        {
            RideId = rideId,
            Message = _localizer["Rides.DriverConnectionLost"]
        });
    }

    public async Task SendRideStatusSyncAsync(string userId, ActiveRideDto snapshot)
    {
        if (string.IsNullOrEmpty(userId) || snapshot == null) return;
        await _hubContext.Clients.User(userId).SendAsync("RideStatusSync", new
        {
            snapshot.RideId,
            Status = snapshot.Status,
            StatusName = snapshot.StatusName,
            PickupLatitude = snapshot.PickupLatitude,
            PickupLongitude = snapshot.PickupLongitude,
            DropoffLatitude = snapshot.DropoffLatitude,
            DropoffLongitude = snapshot.DropoffLongitude,
            CalculatedPrice = snapshot.CalculatedPrice,
            PaymentMethod = snapshot.PaymentMethod,
            RequestedAt = snapshot.RequestedAt,
            AcceptedAt = snapshot.AcceptedAt,
            StartedAt = snapshot.StartedAt,
            RiderId = snapshot.RiderId,
            RiderName = snapshot.RiderName,
            RiderPhone = snapshot.RiderPhone,
            DriverId = snapshot.DriverId,
            DriverName = snapshot.DriverName,
            DriverPhone = snapshot.DriverPhone,
            VehicleModel = snapshot.VehicleModel,
            VehicleYear = snapshot.VehicleYear,
            VinNumber = snapshot.VinNumber,
            DriverLatitude = snapshot.DriverLatitude,
            DriverLongitude = snapshot.DriverLongitude,
            Message = _localizer["Rides.RideStateSynchronized"]
        });
    }


}