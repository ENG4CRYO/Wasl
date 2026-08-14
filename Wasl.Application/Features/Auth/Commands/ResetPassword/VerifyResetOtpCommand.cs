using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Auth.Commands.ResetPassword
{
    public class VerifyResetOtpCommand : IRequest<ApiResponse<string>>
    {
        public string ResetToken { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}