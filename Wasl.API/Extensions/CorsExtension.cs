using Microsoft.Extensions.Configuration;

namespace Wasl.api.Extensions
{
    public static class CorsExtension
    {
        public static IServiceCollection AddGlobalCors(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddCors(options =>
            {
               
                options.AddPolicy("AllowAll", builder =>
                    builder.AllowAnyOrigin()   
                           .AllowAnyMethod()  
                           .AllowAnyHeader());

                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    options.AddPolicy("Production", builder =>
                        builder.WithOrigins(allowedOrigins)
                               .AllowAnyMethod()
                               .AllowAnyHeader()
                               .AllowCredentials());
                }
            });

            return services;
        }
    }
}
