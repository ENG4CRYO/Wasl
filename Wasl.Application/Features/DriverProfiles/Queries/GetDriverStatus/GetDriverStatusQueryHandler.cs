using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.DriverProfiles.Queries.GetDriverStatus
{
    public class GetDriverStatusQueryHandler : IRequestHandler<GetDriverStatusQuery, ApiResponse<DriverApprovalStatus>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly IApplicationDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetDriverStatusQueryHandler(
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            IApplicationDbContext context,
            IStringLocalizer<SharedResource> localizer)
        {
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _context = context;
            _localizer = localizer;
        }

        public async Task<ApiResponse<DriverApprovalStatus>> Handle(GetDriverStatusQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<DriverApprovalStatus>.Failure(_localizer["Auth.Unauthenticated"]);
            }

            var cacheKey = $"DriverStatus:{userId}";
            var cachedStatus = await _cacheService.GetAsync<DriverApprovalStatus?>(cacheKey, cancellationToken);

            if (cachedStatus != null)
            {
                return ApiResponse<DriverApprovalStatus>.Success(cachedStatus.Value, "Status retrieved succededed");
            }

            var driverStatus = await _context.DriverProfiles
                .AsNoTracking()
                .Where(dp => dp.UserId == userId)
                .Select(dp => (DriverApprovalStatus?)dp.ApprovalStatus)
                .FirstOrDefaultAsync(cancellationToken);

            if (driverStatus == null)
            {
                return ApiResponse<DriverApprovalStatus>.Failure(_localizer["DriverProfiles.NotFound"]);
            }

            await _cacheService.SetAsync(cacheKey, driverStatus.Value, TimeSpan.FromHours(24), cancellationToken);

            return ApiResponse<DriverApprovalStatus>.Success(driverStatus.Value, "Status retrieved succededed");
        }
    }
}