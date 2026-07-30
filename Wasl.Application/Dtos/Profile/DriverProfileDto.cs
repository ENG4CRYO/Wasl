using Wasl.Core.Enums;

namespace Wasl.Application.Dtos.Profile
{
    public class DriverProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public DriverApprovalStatus ApprovalStatus { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public decimal Balance { get; set; }
    }
}
