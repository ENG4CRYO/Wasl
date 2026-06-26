using System;

namespace Wasl.Application.Dtos.Admin
{
    public class PendingDriverListDto
    {
        public string DriverId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }
}