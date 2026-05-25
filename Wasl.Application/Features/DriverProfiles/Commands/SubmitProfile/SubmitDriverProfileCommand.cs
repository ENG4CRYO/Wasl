using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;

namespace Wasl.Application.Features.DriverProfiles.Commands.SubmitProfile
{
    public class SubmitDriverProfileCommand : IRequest<ApiResponse<string>>
    {
        public string VehicleModel { get; set; } = string.Empty;
        public int VehicleYear { get; set; }
        public string VinNumber { get; set; } = string.Empty;

        public IFormFile VehicleImage { get; set; } = null!;
        public IFormFile LicenseFrontImage { get; set; } = null!;
        public IFormFile LicenseBackImage { get; set; } = null!;
        public IFormFile SelfieImage { get; set; } = null!;
    }
}
