using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;

namespace Wasl.Application.Features.Auth.Commands.DriverRegistration
{
    public class VerifyDriverRegistrationCommand : IRequest<ApiResponse<AuthModel>>
    {
        public string RegisterToken { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}