using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.DriverProfiles.Queries.GetDriverStatus
{
    public class GetDriverStatusQueryHandler : IRequestHandler<GetDriverStatusQuery, ApiResponse<DriverApprovalStatus>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly IApplicationDbContext _context;

        public GetDriverStatusQueryHandler(
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            IApplicationDbContext context)
        {
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _context = context;
        }

        public async Task<ApiResponse<DriverApprovalStatus>> Handle(GetDriverStatusQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<DriverApprovalStatus>.Failure("Unauthorized");
            }

            var cacheKey = $"DriverStatus:{userId}";
            var cachedStatus = await _cacheService.GetAsync<DriverApprovalStatus?>(cacheKey, cancellationToken);

            if (cachedStatus != null)
            {
                return ApiResponse<DriverApprovalStatus>.Success(cachedStatus.Value, "Status retrieved from cache.");
            }

            var driverStatus = await _context.DriverProfiles
                .AsNoTracking()
                .Where(dp => dp.UserId == userId)
                .Select(dp => (DriverApprovalStatus?)dp.ApprovalStatus)
                .FirstOrDefaultAsync(cancellationToken);

            if (driverStatus == null)
            {
                return ApiResponse<DriverApprovalStatus>.Failure("Driver profile not found.");
            }

            await _cacheService.SetAsync(cacheKey, driverStatus.Value, TimeSpan.FromHours(24), cancellationToken);

            return ApiResponse<DriverApprovalStatus>.Success(driverStatus.Value, "Status retrieved from database.");
        }
    }
}