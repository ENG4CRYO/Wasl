using Wasl.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Wasl.API.Extensions
{
    public static class MigrationExtension
    {
        public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                var context = services.GetRequiredService<AppDbContext>();

                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    logger.LogInformation("⏳ Applying pending database migrations...");

                    await context.Database.MigrateAsync();

                    logger.LogInformation("✅ Database migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("✔️ Database is up to date. No pending migrations.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ An error occurred while applying the database migrations.");
                throw;
            }
        }
    }
}