using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Features.Rides.Commands.AcceptRide;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IDriverNotificationService
    {
        Task NotifyDriversWithRideRequestAsync(List<string> driverIds,
            Guid rideId, double latitude, double longitude,
            double dropLat, double dropLng, decimal price,
            string paymentMethod, string riderName, string riderPhone);

        Task HideRideRequestFromDriversAsync(List<string> driverIds, Guid rideId);
        Task NotifyUserRideCancelledAsync(string userId, string message);

        Task SendProfileReviewedNotificationAsync(string driverId, bool isApproved, string message);


        Task NotifyRiderRideAcceptedAsync(string riderId, DriverRideAcceptedInfoDto info);
        Task NotifyRiderDriverArrivedAsync(string riderId, Guid rideId);
        Task NotifyRiderRideStartedAsync(string riderId, Guid rideId);
        Task NotifyRiderRideCompletedAsync(string riderId, Guid rideId);

        /// <summary>
        /// Broadcasts to the Ride_{rideId} group that the driver lost connectivity.
        /// Does NOT change the ride business state.
        /// </summary>
        Task NotifyRideGroupDriverDisconnectedAsync(Guid rideId);

        /// <summary>
        /// Pushes the authoritative ride snapshot to a user after reconnection / cold start.
        /// </summary>
        Task SendRideStatusSyncAsync(string userId, ActiveRideDto snapshot);
    }
}
