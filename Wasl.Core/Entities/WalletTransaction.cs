using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Core.Entities.BaseEntity;
using Wasl.Core.Enums;

namespace Wasl.Core.Entities
{
    public class WalletTransaction : BaseAuditableEntity<int>  
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = default!;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public Guid? RideId { get; set; }
        public Ride? Ride { get; set; }
    }
}
