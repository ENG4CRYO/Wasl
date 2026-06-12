using AutoMapper;
using Wasl.Application.Common.Behaviors;
using Wasl.Application.Helpers;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Services;
using Wasl.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Wasl.Application.Interfaces.Services;

namespace Wasl.Application.Extensions
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<JWT>();
            services.AddScoped<ITokenHelper, TokenHelper>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddLocalization();

            services.AddScoped<IOtpService,OtpService>();


          

        }
    }
}
