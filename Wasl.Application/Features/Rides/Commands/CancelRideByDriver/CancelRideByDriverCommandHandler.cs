using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.CancelRideByDriver
{
    public class CancelRideByDriverCommandHandler : IRequestHandler<CancelRideByDriverCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDriverNotificationService _notificationService;
        private readonly IRedisCacheService _redisCache;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CancelRideByDriverCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IDriverNotificationService notificationService,
            IRedisCacheService redisCache,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _redisCache = redisCache;
            _localizer = localizer;
        }

        public async Task<ApiResponse<bool>> Handle(CancelRideByDriverCommand request, CancellationToken cancellationToken)
        {
            var driverId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(driverId))
                return ApiResponse<bool>.Failure(_localizer["Auth.Unauthenticated"]);

            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken);
            if (ride == null)
                return ApiResponse<bool>.Failure(_localizer["Rides.RideDoesNotExist"]);

            if (ride.DriverId != driverId)
                return ApiResponse<bool>.Failure(_localizer["Rides.RideNotYours"]);

            if (ride.Status == RideStatus.InProgress || ride.Status == RideStatus.Completed)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.CannotCancelRideAfterStart"]);
            }
            if (ride.Status == RideStatus.Cancelled)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.AlreadyCancelled"]);
            }


            ride.Status = RideStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);


            await _redisCache.ReleaseRideLockAsync(ride.Id);

            if (!string.IsNullOrEmpty(ride.RiderId))
            {
                await _notificationService.NotifyUserRideCancelledAsync(
                    ride.RiderId,
                    _localizer["Rides.RideCancelledByDriver"]
                );
            }

            return ApiResponse<bool>.Success(true, _localizer["Rides.CancelledSuccefully"]);
        }
    }
}