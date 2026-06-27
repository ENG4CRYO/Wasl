using Wasl.Core.Enums;

namespace Wasl.Application.Dtos.Admin
{
    public class DriverListDto
    {
        public string DriverId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DriverApprovalStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}