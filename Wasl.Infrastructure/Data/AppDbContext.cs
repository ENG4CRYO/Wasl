using Wasl.Application.Interfaces.Common;
using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;
using Wasl.Core.Entities.BaseEntity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Wasl.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        private readonly ICurrentUserService _currentUser;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService currentUser)
            : base(options)
        {
            _currentUser = currentUser;
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? "System";
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is null)
                    continue;

                var baseType = entry.Entity.GetType().BaseType;

                if (baseType != null &&
                    baseType.IsGenericType &&
                    baseType.GetGenericTypeDefinition() == typeof(BaseAuditableEntity<>))
                {
                    if (entry.State == EntityState.Added)
                    {
                        entry.Property("CreatedAt").CurrentValue = now;
                        entry.Property("CreatedBy").CurrentValue = userId;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        entry.Property("CreatedAt").IsModified = false;
                        entry.Property("CreatedBy").IsModified = false;

                        entry.Property("UpdatedAt").CurrentValue = now;
                        entry.Property("UpdatedBy").CurrentValue = userId;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}