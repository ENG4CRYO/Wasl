using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Wasl.api.Extensions;
using Wasl.api.Factories;
using Wasl.api.Hubs;
using Wasl.API.Extensions;
using Wasl.API.Helper.CustomCssScalar;
using Wasl.API.Hubs;
using Wasl.API.Middlewares;
using Wasl.API.Services;
using Wasl.Application.Common;
using Wasl.Application.Common.Models;
using Wasl.Application.Extensions;
using Wasl.Application.Helpers;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Entities;
using Wasl.Infrastructure.Data.Seeders;
using Wasl.Infrastructure.Extensions;

SerilogExtension.SetupBootstrapLogger();

try
{
    Log.Information("Starting Web Application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.RegisterSerilog();
    builder.Services.AddGlobalRateLimiter();
    builder.Services.AddGlobalHealthChecks(builder.Configuration);
    builder.Services.AddApiResponseCompression();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IDriverNotificationService,DriverNotificationService>();
    builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

    builder.Services.AddCorsConfiguration(builder.Configuration);

    builder.Services.AddOpenApi(options =>
    {

        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Wasl API";
            document.Info.Version = "v1";

            document.Info.Description = ScalarDocumentInfo.GetScalarDocumentInfo();

            document.Info.Contact = new OpenApiContact
            {
                Name = "Mustafa Aqeel"
            };

            return Task.CompletedTask;
        });
    });
    builder.Services.Configure<RidePricingSettings>(
    builder.Configuration.GetSection("RidePricingSettings"));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApiConfig(builder.Configuration);

    builder.Services.AddInfrastructureService(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddFluentValidationAutoValidation(config =>
    config.OverrideDefaultResultFactoryWith<CustomResultFactory>());

    builder.Services.AddApiVersion();

    builder.Services.AddControllers(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });





    var supportedCultures = new[] { "en", "ar" };
    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    builder.Services.AddSignalR();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            await AdminSeeder.SeedAdminAsync(userManager, roleManager, configuration);
        }
        catch (System.Exception ex)
        {

            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "حدث خطأ أثناء بذر بيانات مدير النظام.");
        }
    }

    app.UseRequestLocalization(localizationOptions);

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<Wasl.Infrastructure.Data.AppDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error While Create Database");
        }
    }


    app.UseSerilogRequestLogging();

    app.UseMiddleware<GlobalErrorHandlerMiddleware>();

    app.UseResponseCompression();

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Theme = ScalarTheme.Moon;
        options.Layout = ScalarLayout.Modern;
        options.WithTitle("Wasl API")
               .WithCustomCss(CssScalar.GetCss());
    });

   
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);

    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseHttpsRedirection();
  

    app.UseSecurityHeaders(PolicyCollection.policyCollection(app));
    app.UseGlobalHealthChecks();
    app.UseRouting();
    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseRateLimiter();
    app.MapControllers().RequireRateLimiting("IpLimiter");
    if (app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire");
    }
    app.MapHub<TrackingHub>("/hubs/tracking");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}