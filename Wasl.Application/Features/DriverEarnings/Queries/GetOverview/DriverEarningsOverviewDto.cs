namespace Wasl.Application.Features.DriverEarnings.Queries.GetOverview
{
    public class DriverEarningsOverviewDto
    {
        public int CompletedRides { get; set; }
        public decimal TotalEarnings { get; set; }
        public int OnlineMinutes { get; set; }
        public bool CanCashOut { get; set; }
    }
}
