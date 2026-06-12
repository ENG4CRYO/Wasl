public class SubmitProfileApiRequest
{
    public string VehicleModel { get; set; } = string.Empty;
    public int VehicleYear { get; set; }
    public string VinNumber { get; set; } = string.Empty;

    public IFormFile? VehicleImage { get; set; }
    public IFormFile? LicenseFrontImage { get; set; }
    public IFormFile? LicenseBackImage { get; set; }
    public IFormFile? SelfieImage { get; set; }
}