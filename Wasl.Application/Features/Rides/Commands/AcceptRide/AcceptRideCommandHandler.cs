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

namespace Wasl.Application.Features.Rides.Commands.AcceptRide;

public class AcceptRideCommandHandler : IRequestHandler<AcceptRideCommand, ApiResponse<bool>>
{
    private readonly IRedisCacheService _redisCache;
    private readonly IApplicationDbContext _dbContext;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICurrentUserService _currentUserService; 

    public AcceptRideCommandHandler(
        IRedisCacheService redisCache,
        IApplicationDbContext dbContext,
        IStringLocalizer<SharedResource> localizer,
        ICurrentUserService currentUserService)
    {
        _redisCache = redisCache;
        _dbContext = dbContext;
        _localizer = localizer;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<bool>> Handle(AcceptRideCommand request, CancellationToken cancellationToken)
    {
        var driverId = _currentUserService.UserId();
        if (string.IsNullOrEmpty(driverId))
        {
            return ApiResponse<bool>.Failure("Unauthorized access.");
        }

        var driverProfile = await _dbContext.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == driverId, cancellationToken);

        if (driverProfile == null)
        {
            return ApiResponse<bool>.Failure(_localizer["Driver.ProfileNotFound"]);
        }

        if (driverProfile.ApprovalStatus != DriverApprovalStatus.Approved)
        {
            return ApiResponse<bool>.Failure(_localizer["Driver.AccountNotApproved"]);
        }

        var rideId = Guid.Parse(request.RideId);

        bool isLockAcquired = await _redisCache.AcquireRideLockAsync(rideId, driverId);
        if (!isLockAcquired)
        {
            return ApiResponse<bool>.Failure(_localizer["Rides.FailedToAcceptRide"]);
        }

        var ride = await _dbContext.Rides.FindAsync(new object[] { rideId }, cancellationToken);

        if (ride == null)
        {
            await _redisCache.ReleaseRideLockAsync(rideId);
            return ApiResponse<bool>.Failure(_localizer["Rides.NotFound"]);
        }

        if (ride.Status != RideStatus.Pending)
        {
            await _redisCache.ReleaseRideLockAsync(rideId);
            return ApiResponse<bool>.Failure(_localizer["Rides.AlreadyAccepted"]);
        }

        ride.DriverId = driverId;
        ride.Status = RideStatus.Accepted;
        ride.AcceptedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true, _localizer["Rides.RideAcceptanceSucceeded"]);
    }
}