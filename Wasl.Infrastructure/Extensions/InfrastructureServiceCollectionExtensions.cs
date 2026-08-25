using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Wasl.Application.Helpers;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Entities;
using Wasl.Infrastructure.BackgroundJobs;
using Wasl.Infrastructure.Data;
using Wasl.Infrastructure.Models;
using Wasl.Infrastructure.Services;
using Wasl.Infrastructure.Settings;

namespace Wasl.Infrastructure.Extensions
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, b =>
                    b.MigrationsAssembly(typeof(InfrastructureServiceCollectionExtensions).Assembly.FullName)
                ));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
            services.AddTransient<IEmailService, EmailService>();
            services.AddHostedService<EmailBackgroundSender>();


            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
            services.Configure<JWT>(configuration.GetSection("JWT"));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                var jwtKey = configuration["JWT:Key"];
                var jwtIssuer = configuration["JWT:Issuer"];
                var jwtAudience = configuration["JWT:Audience"];

                if (string.IsNullOrEmpty(jwtKey))
                {
                    throw new InvalidOperationException("JWT Key is missing from configuration.");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.Value!.Contains("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddTransient<ITemplateService, TemplateService>();
            services.AddHttpContextAccessor();
            services.AddScoped<IFileService, LocalFileService>();


            var redisConnectionString = configuration.GetConnectionString("Redis");
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString!));


            services.AddSingleton<RedisCacheService>();
            services.AddSingleton<ICacheService>(provider => provider.GetRequiredService<RedisCacheService>());
            services.AddSingleton<IRedisCacheService>(provider => provider.GetRequiredService<RedisCacheService>());
            services.AddSingleton<IDriverSessionCacheService, DriverSessionCacheService>();
            services.AddHostedService<DriverSessionSyncHostedService>();
 

            services.AddHangfire(config => config
                 .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                 .UseSimpleAssemblyNameTypeSerializer()
                 .UseRecommendedSerializerSettings()
                 .UsePostgreSqlStorage(config =>
                 config.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));

            services.AddHangfireServer();

            services.AddScoped<IRideDispatchService, RideDispatchService>();
            services.AddScoped<IPaymentGatewayService, MockPaymentGatewayService>();
            services.AddScoped<IActiveRideReader, ActiveRideReader>();

            return services;
        }
    }
}