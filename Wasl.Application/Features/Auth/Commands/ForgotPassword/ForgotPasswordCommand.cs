using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using MediatR;

namespace Wasl.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommand : IRequest<ApiResponse<string>>
    {
        public string Email { get; set; } = string.Empty;
    }
}
