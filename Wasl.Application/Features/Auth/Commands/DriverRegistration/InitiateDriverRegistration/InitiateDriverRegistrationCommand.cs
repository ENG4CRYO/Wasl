using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Auth.Commands.DriverRegistration.InitiateDriverRegistration
{
    public class InitiateDriverRegistrationCommand : IRequest<ApiResponse<string>>
    {
        public string Email { get; set; } = string.Empty;
    }
}
