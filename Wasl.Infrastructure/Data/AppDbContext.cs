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
        public DbSet<DriverProfile> DriverProfiles { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<RideReview> RideReviews { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId() ?? "System";
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(p => p.CreatedAt).IsModified = false;
                    entry.Property(p => p.CreatedBy).IsModified = false;

                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}