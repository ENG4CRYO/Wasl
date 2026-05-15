using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Core.Entities.BaseEntity;
using Wasl.Core.Enums;

namespace Wasl.Core.Entities
{
    public class Ride : BaseAuditableEntity<Guid>
    {
        public string RiderId { get; set; } = string.Empty;
        public ApplicationUser Rider { get; set; } = default!;

        public string? DriverId { get; set; }
        public ApplicationUser? Driver { get; set; }

        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }

        public decimal CalculatedPrice { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public RideStatus Status { get; set; } = RideStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CanceledAt { get; set; }

        public RideReview? Review { get; set; }
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
