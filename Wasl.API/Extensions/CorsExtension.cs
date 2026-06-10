using Microsoft.Extensions.DependencyInjection;

namespace Wasl.api.Extensions
{
    public static class CorsExtension
    {
        public static IServiceCollection AddGlobalCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                    builder.SetIsOriginAllowed(_ => true) 
                           .AllowAnyMethod()             
                           .AllowAnyHeader()             
                           .AllowCredentials()); 
            });

            return services;
        }
    }
}