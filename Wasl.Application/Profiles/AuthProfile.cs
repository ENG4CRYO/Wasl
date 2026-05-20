using AutoMapper;
using Wasl.Core.Entities;
using Wasl.Application.Dtos.AuthModel;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Features.Auth.Commands.Register;
using Wasl.Application.Features.Auth.Commands.RiderRegistration.InitiateRiderRegistration;
using Wasl.Application.Features.Auth.Commands.DriverRegistration.InitiateDriverRegistration;

namespace Wasl.Application.Profiles
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {
            
            CreateMap<RegisterCommand, ApplicationUser>();
            CreateMap<AuthModel, ApplicationUser>();
            CreateMap<ApplicationUser, AuthModel>();
            CreateMap<InitiateRiderRegistrationCommand, ApplicationUser>();
            CreateMap<InitiateDriverRegistrationCommand, ApplicationUser>();

        }
    }
}
