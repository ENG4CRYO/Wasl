using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Core.Entities;

namespace Wasl.Infrastructure.Configurtions
{
    public class RideConfiguration : IEntityTypeConfiguration<Ride>
    {
        public void Configure(EntityTypeBuilder<Ride> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.CalculatedPrice)
                .HasColumnType("decimal(18,2)");

            builder.HasOne(r => r.Rider)
                .WithMany(u => u.RequestedRides)
                .HasForeignKey(r => r.RiderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Driver)
                .WithMany(u => u.DrivenRides)
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
