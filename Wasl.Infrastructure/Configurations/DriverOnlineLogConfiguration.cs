using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wasl.Core.Entities;

namespace Wasl.Infrastructure.Configurations
{
    public class DriverOnlineLogConfiguration : IEntityTypeConfiguration<DriverOnlineLog>
    {
        public void Configure(EntityTypeBuilder<DriverOnlineLog> builder)
        {
            builder.ToTable("DriverOnlineLogs");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.DriverId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(d => d.StartTime)
                .IsRequired();

            builder.Property(d => d.EndTime)
                .IsRequired();

            builder.Property(d => d.DurationMinutes)
                .IsRequired();

            builder.HasOne(d => d.Driver)
                .WithMany()
                .HasForeignKey(d => d.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
