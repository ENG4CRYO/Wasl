using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IDriverNotificationService
    {
        Task NotifyDriversWithRideRequestAsync(List<string> driverIds,
            Guid rideId, double latitude, double longitude,
            double dropLat, double dropLng, decimal price);

        Task HideRideRequestFromDriversAsync(List<string> driverIds, Guid rideId);
        Task NotifyUserRideCancelledAsync(string userId, string message);

        Task SendProfileReviewedNotificationAsync(string driverId, bool isApproved, string message);


        Task NotifyRiderRideAcceptedAsync(string riderId, Guid rideId, string driverId);
        Task NotifyRiderDriverArrivedAsync(string riderId, Guid rideId);
        Task NotifyRiderRideStartedAsync(string riderId, Guid rideId);
        Task NotifyRiderRideCompletedAsync(string riderId, Guid rideId);
    }
}
