using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.DriverEarnings.Queries.GetOverview
{
    public class GetDriverEarningsOverviewQuery : IRequest<ApiResponse<DriverEarningsOverviewDto>>
    {
        public string DriverId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
