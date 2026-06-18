using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;

namespace Wasl.Application.Features.Auth.Commands.RiderRegistration.VerifyRiderRegistration
{
    public class CompleteRiderRegistrationCommand : IRequest<ApiResponse<AuthModel>>
    {
        public string RegisterToken { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
