using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Queries.GetRideHistory
{
    public class GetRideHistoryQueryHandler : IRequestHandler<GetRideHistoryQuery, ApiResponse<PagedList<RideHistoryDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetRideHistoryQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<PagedList<RideHistoryDto>>> Handle(GetRideHistoryQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();

            var query = _context.Rides
                .Where(r => (r.RiderId == userId || r.DriverId == userId) &&
                            (r.Status == RideStatus.Completed || r.Status == RideStatus.Cancelled))
                .AsNoTracking()
                .OrderByDescending(r => r.RequestedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var rawRides = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new
                {
                    r.RequestedAt,
                    r.CalculatedPrice,
                    r.Status
                })
                .ToListAsync(cancellationToken);

            var items = rawRides.Select(r => new RideHistoryDto
            {
                RequestedDate = r.RequestedAt.Date,
                RequestedTime = r.RequestedAt.TimeOfDay,
                Price = r.CalculatedPrice,
                Status = r.Status == RideStatus.Completed
                    ? _localizer["Rides.Completed"]
                    : _localizer["Rides.Cancelled"]
            }).ToList();

            var pagedList = new PagedList<RideHistoryDto>(items, totalCount, request.PageNumber, request.PageSize);

            return ApiResponse<PagedList<RideHistoryDto>>.Success(
                pagedList,
                _localizer["Rides.HistoryRetrievedSuccessfully"]);
        }
    }
}
