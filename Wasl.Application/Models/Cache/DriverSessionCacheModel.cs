namespace Wasl.Application.Models.Cache
{
    public class DriverSessionCacheModel
    {
        public string DriverId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? DisconnectedAt { get; set; }
    }
}
