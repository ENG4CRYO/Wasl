using AutoMapper.Configuration;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.Register
{
    public class RegisterCommand : IRequest<ApiResponse<AuthModel>>
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string UserName { get; set; } = default!;

        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
