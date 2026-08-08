namespace Wasl.Application.Features.Rides.Commands.AcceptRide;

public class DriverRideAcceptedInfoDto
{
    public Guid RideId { get; set; }
    public string DriverId { get; set; } = default!;
    public string DriverName { get; set; } = default!;
    public string DriverProfilePictureUrl { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public int VehicleYear { get; set; }
    public string VinNumber { get; set; } = string.Empty;
    public string Message { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
}
