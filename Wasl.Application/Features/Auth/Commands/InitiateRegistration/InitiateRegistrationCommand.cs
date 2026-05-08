using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.InitiateRegistration
{
    public class InitiateRegistrationCommand : IRequest<ApiResponse<string>>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
