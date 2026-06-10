using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IDriverNotificationService
    {
        Task NotifyDriversWithRideRequestAsync(List<string> driverIds,
            Guid rideId, double latitude, double longitude,
            double dropLat, double dropLng);
    }
}
