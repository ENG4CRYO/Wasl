using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.AcceptRide;

public class AcceptRideCommandHandler : IRequestHandler<AcceptRideCommand, ApiResponse<bool>>
{
    private readonly IRedisCacheService _redisCache;
    private readonly IApplicationDbContext _dbContext;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDriverNotificationService _driverNotification;

    public AcceptRideCommandHandler(
        IRedisCacheService redisCache,
        IApplicationDbContext dbContext,
        IStringLocalizer<SharedResource> localizer,
        ICurrentUserService currentUserService,
        IDriverNotificationService driverNotification)
    {
        _redisCache = redisCache;
        _dbContext = dbContext;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _driverNotification = driverNotification;
    }

    public async Task<ApiResponse<bool>> Handle(AcceptRideCommand request, CancellationToken cancellationToken)
    {
        var driverId = _currentUserService.UserId();
        if (string.IsNullOrEmpty(driverId))
        {
            return ApiResponse<bool>.Failure("Unauthorized access.");
        }

       
        var driverStatus = await _dbContext.DriverProfiles
            .AsNoTracking()
            .Where(dp => dp.UserId == driverId)
            .Select(dp => (DriverApprovalStatus?)dp.ApprovalStatus)
            .FirstOrDefaultAsync(cancellationToken);

        if (driverStatus == null)
        {
            return ApiResponse<bool>.Failure(_localizer["Driver.ProfileNotFound"]);
        }

        if (driverStatus != DriverApprovalStatus.Approved)
        {
            return ApiResponse<bool>.Failure(_localizer["Driver.AccountNotApproved"]);
        }

        var rideId = Guid.Parse(request.RideId);

        bool isLockAcquired = await _redisCache.AcquireRideLockAsync(rideId, driverId);
        if (!isLockAcquired)
        {
            return ApiResponse<bool>.Failure(_localizer["Rides.FailedToAcceptRide"]);
        }

        try
        {
            var ride = await _dbContext.Rides.FindAsync(new object[] { rideId }, cancellationToken);

            if (ride == null)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.NotFound"]);
            }

            if (ride.Status != RideStatus.Pending)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.AlreadyAccepted"]);
            }

            ride.DriverId = driverId;
            ride.Status = RideStatus.Accepted;

            ride.AcceptedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var info = await BuildDriverRideAcceptedInfoAsync(ride, cancellationToken);
            await _driverNotification.NotifyRiderRideAcceptedAsync(ride.RiderId, info);

            return ApiResponse<bool>.Success(true, _localizer["Rides.RideAcceptanceSucceeded"]);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<bool>.Failure(_localizer["Rides.AlreadyAccepted"]);
        }
        finally
        {
            await _redisCache.ReleaseRideLockAsync(rideId);
        }
    }

    private async Task<DriverRideAcceptedInfoDto> BuildDriverRideAcceptedInfoAsync(Ride ride, CancellationToken cancellationToken)
    {
        var driver = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == ride.DriverId)
            .Select(u => new
            {
                u.FirstName,
                u.LastName,
                u.ProfilePictureUrls,
                u.PhoneNumber,
                VehicleModel = u.DriverProfile != null ? u.DriverProfile.VehicleModel : string.Empty,
                VehicleYear = u.DriverProfile != null ? u.DriverProfile.VehicleYear : 0,
                VinNumber = u.DriverProfile != null ? u.DriverProfile.VinNumber : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);

        var driverLocation = await _redisCache.GetDriverLocationAsync(ride.DriverId!);

        return new DriverRideAcceptedInfoDto
        {
            RideId = ride.Id,
            DriverId = ride.DriverId!,
            DriverName = driver != null ? $"{driver.FirstName} {driver.LastName}".Trim() : string.Empty,
            DriverProfilePictureUrl = driver?.ProfilePictureUrls ?? string.Empty,
            VehicleModel = driver?.VehicleModel ?? string.Empty,
            VehicleYear = driver?.VehicleYear ?? 0,
            VinNumber = driver?.VinNumber ?? string.Empty,
            PhoneNumber = driver?.PhoneNumber ?? string.Empty,
            DriverLatitude = driverLocation?.Latitude,
            DriverLongitude = driverLocation?.Longitude,
            Message = _localizer["Rides.RideAcceptedByDriver"]
        };
    }
}