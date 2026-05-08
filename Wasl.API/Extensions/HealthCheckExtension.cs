using Wasl.Infrastructure.Data; 
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json; 

namespace Wasl.api.Extensions
{
    public static class HealthCheckExtension
    {
        public static IServiceCollection AddGlobalHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                   
                    .AddCheck("Server", () => HealthCheckResult.Healthy())
                    .AddDbContextCheck<AppDbContext>();

            return services;
        }

        public static WebApplication UseGlobalHealthChecks(this WebApplication app)
        {
            
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        status = report.Status.ToString(),
                        totalDuration = report.TotalDuration,
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            duration = e.Value.Duration.ToString(),
                            description = e.Value.Description
                        })
                    };

                    await context.Response.WriteAsJsonAsync(response);
                }
            });

            return app;
        }
    }
}