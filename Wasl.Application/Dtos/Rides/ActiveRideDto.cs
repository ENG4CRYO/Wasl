using System;

namespace Wasl.Application.Dtos.Rides
{
    /// <summary>
    /// Authoritative snapshot of a ride used for state recovery after
    /// reconnection or app restart (REST + SignalR RideStatusSync).
    /// </summary>
    public class ActiveRideDto
    {
        public Guid RideId { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;

        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }

        public decimal CalculatedPrice { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }

        public string RiderId { get; set; } = string.Empty;
        public string RiderName { get; set; } = string.Empty;
        public string RiderPhone { get; set; } = string.Empty;

        public string? DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public int VehicleYear { get; set; }
        public string VinNumber { get; set; } = string.Empty;

        public double? DriverLatitude { get; set; }
        public double? DriverLongitude { get; set; }
    }
}
