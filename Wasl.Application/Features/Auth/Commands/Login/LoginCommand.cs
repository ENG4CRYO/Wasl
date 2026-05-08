using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FluentValidation;
using AutoMapper.Configuration;


namespace Wasl.Application.Features.Auth.Commands.Login
{

    public class LoginCommand : IRequest<ApiResponse<AuthModel>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

}