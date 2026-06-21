using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.CancelRideByRider
{
    public class CancelRideByRiderCommandHandler : IRequestHandler<CancelRideByRiderCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDriverNotificationService _notificationService;
        private readonly IRedisCacheService _redisCache;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CancelRideByRiderCommandHandler(
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

        public async Task<ApiResponse<bool>> Handle(CancelRideByRiderCommand request, CancellationToken cancellationToken)
        {
            var riderId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(riderId))
                return ApiResponse<bool>.Failure(_localizer["Auth.Unauthenticated"]);

            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken);
            if (ride == null)
                return ApiResponse<bool>.Failure(_localizer["Rides.RideDoesNotExist"]);

        
            if (ride.RiderId != riderId)
                return ApiResponse<bool>.Failure(_localizer["Rides.RideNotYours"]);

            if (ride.Status == RideStatus.InProgress || ride.Status == RideStatus.Completed)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.CannotCancelRideAfterStart"]);
            }
            if (ride.Status == RideStatus.Cancelled)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.AlreadyCancelled"]);
            }

            var previousStatus = ride.Status;

            ride.Status = RideStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);

   

            if (previousStatus == RideStatus.Pending)
            {
               
                var notifiedDrivers = await _redisCache.GetExcludedDriversForRideAsync(ride.Id);

                if (notifiedDrivers != null && notifiedDrivers.Any())
                {
                    await _notificationService.HideRideRequestFromDriversAsync(notifiedDrivers, ride.Id);
                }
            }
            else if (previousStatus == RideStatus.Accepted || previousStatus == RideStatus.Arrived)
            {

                await _redisCache.ReleaseRideLockAsync(ride.Id);

                if (!string.IsNullOrEmpty(ride.DriverId))
                {
                    await _notificationService.NotifyUserRideCancelledAsync(
                        ride.DriverId,
                        _localizer["Rides.RideCancelledByCustomer"]
                    );
                }
            }

            return ApiResponse<bool>.Success(true, _localizer["Rides.CancelledSuccefully"]);
        }
    }
}