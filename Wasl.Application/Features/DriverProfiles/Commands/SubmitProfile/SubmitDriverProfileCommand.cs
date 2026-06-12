using MediatR;

using Wasl.Application.Common;
using Wasl.Application.Common.Models;

namespace Wasl.Application.Features.DriverProfiles.Commands.SubmitProfile
{
    public class SubmitDriverProfileCommand : IRequest<ApiResponse<string>>
    {
        public string VehicleModel { get; set; } = string.Empty;
        public int VehicleYear { get; set; }
        public string VinNumber { get; set; } = string.Empty;

        public UploadedFile VehicleImage { get; set; }
        public UploadedFile LicenseFrontImage { get; set; }
        public UploadedFile LicenseBackImage { get; set; }
        public UploadedFile SelfieImage
        {
            get; set;
        }
    }
}
