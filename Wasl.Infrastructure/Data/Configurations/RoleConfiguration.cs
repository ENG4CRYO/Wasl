using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wasl.Core.Constants;

namespace Wasl.Infrastructure.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
    {
        public void Configure(EntityTypeBuilder<IdentityRole> builder)
        {
            builder.HasData(
                new IdentityRole
                {
                    Id = "1",
                    Name = AspRoles.Admin,
                    NormalizedName = AspRoles.Admin.ToUpper(),
                    ConcurrencyStamp = "1"
                },
                new IdentityRole
                {
                    Id = "2",
                    Name = AspRoles.Driver,
                    NormalizedName = AspRoles.Driver.ToUpper(),
                    ConcurrencyStamp = "2" 
                },
                new IdentityRole
                {
                    Id = "3",
                    Name = AspRoles.Rider,
                    NormalizedName = AspRoles.Rider.ToUpper(),
                    ConcurrencyStamp = "3"
                }
            );
        }
    }
}