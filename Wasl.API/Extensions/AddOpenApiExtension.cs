using Wasl.Application.Helpers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wasl.API.Extensions
{
    public static class AddOpenApiExtension
    {
        public static IServiceCollection AddOpenApiConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    var domainUrl = configuration.GetSection("DomainUrl").Value;
                    if (!string.IsNullOrEmpty(domainUrl))
                    {
                        document.Servers = new List<OpenApiServer>
                        {
                            new OpenApiServer { Url = domainUrl }
                        };
                    }

                    document.Info = new OpenApiInfo
                    {
                        Title = "Wasl API",
                        Version = "v1",
                        Description = ScalarDocumentInfo.GetScalarDocumentInfo(),
                    };

                    document.Components ??= new OpenApiComponents();

                    var jwtSchemeName = "Bearer";
                    var jwtSecurityScheme = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "JWT Token for User Authentication"
                    };

                    document.AddComponent(jwtSchemeName, jwtSecurityScheme);

                    document.Security ??= new List<OpenApiSecurityRequirement>();
                    document.Security.Add(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference(jwtSchemeName, document),
                            new List<string>()
                        }
                    });

                    return Task.CompletedTask;
                });
            });

            return services;
        }
    }
}