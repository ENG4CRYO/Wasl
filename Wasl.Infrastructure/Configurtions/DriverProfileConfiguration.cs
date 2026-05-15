using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Core.Entities;

namespace Wasl.Infrastructure.Configurtions
{
    public class DriverProfileConfiguration : IEntityTypeConfiguration<DriverProfile>
    {
        public void Configure(EntityTypeBuilder<DriverProfile> builder)
        {
            builder.HasOne(dp => dp.User)
                .WithOne(u => u.DriverProfile)
                .HasForeignKey<DriverProfile>(dp => dp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
