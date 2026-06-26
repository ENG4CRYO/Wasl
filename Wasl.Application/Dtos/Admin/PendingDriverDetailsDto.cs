using System;

namespace Wasl.Application.Dtos.Admin
{
    public class PendingDriverDetailsDto
    {
        public string DriverId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Address { get; set; }
        public string VehicleModel { get; set; } = string.Empty;
        public int VehicleYear { get; set; }
        public string VinNumber { get; set; } = string.Empty;
        public string? VehicleImagesUrl { get; set; }
        public string? LicenseFrontUrl { get; set; }
        public string? LicenseBackUrl { get; set; }
        public string? SelfieUrl { get; set; }

        public DateTime SubmittedAt { get; set; }
    }
}