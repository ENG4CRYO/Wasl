using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommand : IRequest<ApiResponse<bool>>
    {
        public string ResetToken { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
