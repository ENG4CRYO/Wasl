using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public decimal TotalFare { get; set; }
        public decimal CompanyCommission { get; set; }
        public decimal DriverNetEarnings { get; set; }
        public string? PaymentToken { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        [ConcurrencyCheck]
        public RideStatus Status { get; set; } = RideStatus.Pending;

        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CanceledAt { get; set; }

        public RideReview? Review { get; set; }
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
