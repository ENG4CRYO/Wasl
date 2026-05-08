using AutoMapper;
using Wasl.Core.Entities;
using Wasl.Application.Dtos.AuthModel;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Features.Auth.Commands.Register;
using Wasl.Application.Features.Auth.Commands.InitiateRegistration;

namespace Wasl.Application.Profiles
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {
            
            CreateMap<RegisterCommand, ApplicationUser>();
            CreateMap<AuthModel, ApplicationUser>();
            CreateMap<ApplicationUser, AuthModel>();
            CreateMap<InitiateRegistrationCommand, ApplicationUser>();
        }
    }
}
