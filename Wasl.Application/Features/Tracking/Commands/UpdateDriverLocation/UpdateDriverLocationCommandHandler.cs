using MediatR;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Tracking.Commands.UpdateDriverLocation
{
    public class UpdateDriverLocationCommandHandler : IRequestHandler<UpdateDriverLocationCommand, ApiResponse<bool>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IActiveRideReader _activeRideReader;
        private readonly IRedisCacheService _redisCache;
        private readonly ICacheService _cacheService;
        private readonly IDriverNotificationService _notificationService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UpdateDriverLocationCommandHandler(
            ICurrentUserService currentUserService,
            IActiveRideReader activeRideReader,
            IRedisCacheService redisCache,
            ICacheService cacheService,
            IDriverNotificationService notificationService,
            IStringLocalizer<SharedResource> localizer)
        {
            _currentUserService = currentUserService;
            _activeRideReader = activeRideReader;
            _redisCache = redisCache;
            _cacheService = cacheService;
            _notificationService = notificationService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<bool>> Handle(UpdateDriverLocationCommand request, CancellationToken cancellationToken)
        {
            var driverId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(driverId))
            {
                return ApiResponse<bool>.Failure("Unauthorized access.");
            }

            var activeRideId = await _activeRideReader.GetActiveRideIdForDriverAsync(driverId, cancellationToken);
            if (!activeRideId.HasValue)
            {
                return ApiResponse<bool>.Failure(_localizer["Tracking.NoActiveRide"]);
            }

            await _redisCache.UpdateDriverLocationAsync(driverId, request.Longitude, request.Latitude);

            await _cacheService.SetAsync(
                $"DriverLocationUpdateAt:{driverId}",
                DateTime.UtcNow,
                TimeSpan.FromMinutes(30),
                cancellationToken);

            await _notificationService.NotifyRideGroupLocationUpdateAsync(
                activeRideId.Value, driverId, request.Latitude, request.Longitude);

            return ApiResponse<bool>.Success(true, _localizer["Tracking.LocationUpdated"]);
        }
    }
}
