using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
