using Serilog;

namespace Wasl.api.Extensions
{
    public static class SerilogExtension
    {
        public static void SetupBootstrapLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();
        }

        public static void RegisterSerilog(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services));
        }
    }
}
