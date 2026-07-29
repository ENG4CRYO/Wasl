using MediatR;
using Microsoft.EntityFrameworkCore;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.DriverEarnings.Queries.GetOverview
{
    public class GetDriverEarningsOverviewQueryHandler : IRequestHandler<GetDriverEarningsOverviewQuery, ApiResponse<DriverEarningsOverviewDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetDriverEarningsOverviewQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<DriverEarningsOverviewDto>> Handle(GetDriverEarningsOverviewQuery request, CancellationToken cancellationToken)
        {
            var startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
            var endDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

            var ridesQuery = _context.Rides
                .Where(r => r.DriverId == request.DriverId
                    && r.Status == RideStatus.Completed
                    && r.CompletedAt >= startDate
                    && r.CompletedAt <= endDate);

            var rideStats = await ridesQuery
                .GroupBy(r => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Sum = g.Sum(r => r.DriverNetEarnings)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var onlineMinutes = await _context.DriverOnlineLogs
                .Where(l => l.DriverId == request.DriverId
                    && l.StartTime >= startDate
                    && l.EndTime <= endDate)
                .SumAsync(l => l.DurationMinutes, cancellationToken);

            var dto = new DriverEarningsOverviewDto
            {
                CompletedRides = rideStats?.Count ?? 0,
                TotalEarnings = rideStats?.Sum ?? 0,
                OnlineMinutes = onlineMinutes,
                CanCashOut = (rideStats?.Sum ?? 0) > 0
            };

            return ApiResponse<DriverEarningsOverviewDto>.Success(dto);
        }
    }
}
