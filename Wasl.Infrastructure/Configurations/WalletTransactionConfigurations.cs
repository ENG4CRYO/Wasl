using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Core.Entities;

namespace Wasl.Infrastructure.Configurations
{
    public class WalletTransactionConfigurations : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.Property(x => x.Id)
                    .ValueGeneratedOnAdd();
        }
    }
}
