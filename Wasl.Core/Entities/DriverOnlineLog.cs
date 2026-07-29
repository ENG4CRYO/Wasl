using Wasl.Core.Entities.BaseEntity;

namespace Wasl.Core.Entities
{
    public class DriverOnlineLog : BaseAuditableEntity<Guid>
    {
        public string DriverId { get; set; } = string.Empty;
        public ApplicationUser Driver { get; set; } = default!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes { get; set; }
    }
}
