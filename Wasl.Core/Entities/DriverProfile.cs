using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Core.Entities.BaseEntity;
using Wasl.Core.Enums;

namespace Wasl.Core.Entities
{
    public class DriverProfile : BaseAuditableEntity<int>
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = default!;
        public string VehicleModel { get; set; } = string.Empty;
        public int VehicleYear { get; set; }
        public string VinNumber { get; set; } = string.Empty;
        public string? VehicleImagesUrl { get; set; }
        public string? LicenseFrontUrl { get; set; }
        public string? LicenseBackUrl { get; set; }
        public string? SelfieUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public DriverApprovalStatus ApprovalStatus { get; set; } = DriverApprovalStatus.Pending;

    }
}
