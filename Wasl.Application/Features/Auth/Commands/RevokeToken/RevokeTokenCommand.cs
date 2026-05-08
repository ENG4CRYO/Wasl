using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.RevokeToken
{
    public class RevokeTokenCommand : IRequest<ApiResponse<bool>>
    {
        public string Token { get; set; } = default!;   
    }
}
