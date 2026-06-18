using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Auth.Commands.DriverRegistration.VerifyDriverOtp
{
    public class VerifyDriverOtpCommand : IRequest<ApiResponse<string>>
    {
        public string SessionToken { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
