using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Auth.Commands.RiderRegistration.InitiateRiderRegistration
{
    public class InitiateRiderRegistrationCommand : IRequest<ApiResponse<string>>
    {
        public string Email { get; set; } = string.Empty;
    }
}
