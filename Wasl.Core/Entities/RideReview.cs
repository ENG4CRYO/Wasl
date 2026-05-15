using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Core.Entities.BaseEntity;

namespace Wasl.Core.Entities
{
    public class RideReview : BaseAuditableEntity<int>
    {
        public Guid RideId { get; set; }
        public Ride Ride { get; set; } = default!;

        public string RiderId { get; set; } = string.Empty;
        public ApplicationUser Rider { get; set; } = default!;

        public string DriverId { get; set; } = string.Empty;
        public ApplicationUser Driver { get; set; } = default!;

        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
