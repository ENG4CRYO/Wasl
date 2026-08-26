using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Wasl.Application.Helpers;

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

                    var tagGroups = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "Driver 🚗",
                            ["tags"] = new JsonArray { "Driver Auth", "Driver Profile" }
                        },
                        new JsonObject
                        {
                            ["name"] = "Rider 👤",
                            ["tags"] = new JsonArray { "Rider Auth" }
                        },
                        new JsonObject
                        {
                            ["name"] = "Common ⚙️",
                            ["tags"] = new JsonArray { "Common Authentication" }
                        },
                        new JsonObject
                        {
                            ["name"] = "Rides",
                            ["tags"] = new JsonArray { "Rides" }
                        },
                        new JsonObject
                        {
                            ["name"] = "Payments 💳",
                            ["tags"] = new JsonArray { "Payments" }
                        },
                        new JsonObject
                        {
                            ["name"] = "Driver Earnings 💰",
                            ["tags"] = new JsonArray { "Driver Earnings" }
                        },
                        new JsonObject
                        {
                            ["name"] = "User Profile",
                            ["tags"] = new JsonArray { "Profile" }
                        },
                        new JsonObject
                        {
                            ["name"] = "Wallet",
                            ["tags"] = new JsonArray { "Wallet" }
 
                        },
                        new JsonObject
                        {
                            ["name"] = "Traking",
                            ["tags"] = new JsonArray { "Tracking" }
                        }
                    };

                    document.Extensions ??= new Dictionary<string, IOpenApiExtension>();

                    document.Extensions["x-tagGroups"] = new JsonNodeExtension(tagGroups);
     
                    return Task.CompletedTask;
                });
            });

            return services;
        }
    }
}