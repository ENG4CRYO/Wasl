namespace Wasl.Application.Dtos.Rides
{
    public class RideHistoryDto
    {
        public DateTime RequestedDate { get; set; }
        public TimeSpan RequestedTime { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
