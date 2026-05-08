using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using MediatR;

namespace Wasl.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<ApiResponse<AuthModel>>
    {
        public string Token { get; set; } = default!;
    }
}