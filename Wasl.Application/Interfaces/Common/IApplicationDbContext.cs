using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;

namespace Wasl.Application.Interfaces.Common
{
    public interface IApplicationDbContext
    {
        DbSet<ApplicationUser> Users { get; }
        DbSet<IdentityRole> Roles { get; }
        DbSet<IdentityUserRole<string>> UserRoles { get; }
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<DriverProfile> DriverProfiles { get; set; }
        DbSet<Ride> Rides { get; set; }
        DbSet<RideReview> RideReviews { get; set; }
        DbSet<WalletTransaction> WalletTransactions { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
