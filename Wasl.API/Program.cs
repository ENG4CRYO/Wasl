using Wasl.api.Extensions;
using Wasl.api.Factories;
using Wasl.api.Middlewares;
using Wasl.API.Extensions;
using Wasl.API.Helper.CustomCssScalar;
using Wasl.Application.Common;
using Wasl.Application.Extensions;
using Wasl.Application.Helpers;
using Wasl.Infrastructure.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

SerilogExtension.SetupBootstrapLogger();

try
{
    Log.Information("Starting Web Application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.RegisterSerilog();
    builder.Services.AddGlobalRateLimiter();
    builder.Services.AddGlobalCors(builder.Configuration);
    builder.Services.AddGlobalHealthChecks(builder.Configuration);
    builder.Services.AddApiResponseCompression();



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


    var app = builder.Build();



    await app.ApplyDatabaseMigrationsAsync();

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
        options.Theme = ScalarTheme.BluePlanet;
        options.Layout = ScalarLayout.Modern;
        options.WithTitle("Wasl API")
               .WithCustomCss(CssScalar.GetCss());
    });
    app.UseCors("AllowAll");
   
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);

    app.UseHttpsRedirection();

    app.UseSecurityHeaders(PolicyCollection.policyCollection(app));
    app.UseGlobalHealthChecks();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers().RequireRateLimiting("IpLimiter");

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