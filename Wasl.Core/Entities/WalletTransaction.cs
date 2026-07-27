using Wasl.Core.Entities.BaseEntity;
using Wasl.Core.Enums;

namespace Wasl.Core.Entities
{
    public class WalletTransaction : BaseAuditableEntity<Guid>
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = default!;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public Guid? RideId { get; set; }
        public Ride? Ride { get; set; }
    }
}
