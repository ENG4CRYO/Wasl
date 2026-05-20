using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Interfaces.Common
{
    public interface IApplicationDbContext
    {
        DbSet<ApplicationUser> Users { get; }
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<DriverProfile> DriverProfiles { get; set; }
        DbSet<Ride> Rides { get; set; }
        DbSet<RideReview> RideReviews { get; set; }
        DbSet<WalletTransaction> WalletTransactions { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
