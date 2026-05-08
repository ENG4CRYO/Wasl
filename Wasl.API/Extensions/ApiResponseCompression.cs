using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace Wasl.api.Extensions
{
    public static class ApiResponseCompression
    {
        public static IServiceCollection AddApiResponseCompression(this IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.SmallestSize;
            });

            return services;
        }
    }
}
