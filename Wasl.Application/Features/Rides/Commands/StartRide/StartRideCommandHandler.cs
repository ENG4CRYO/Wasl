using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.StartRide
{
    public class StartRideCommandHandler : IRequestHandler<StartRideCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUserService;

        public StartRideCommandHandler(
            IApplicationDbContext dbContext,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _localizer = localizer;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<bool>> Handle(StartRideCommand request, CancellationToken cancellationToken)
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
                return ApiResponse<bool>.Failure(_localizer["DriverProfiles.NotFound"]);
            }

            if (driverProfile.ApprovalStatus != DriverApprovalStatus.Approved)
            {
                return ApiResponse<bool>.Failure(_localizer["DriverProfile.AccountNotApproved"]);
            }

            if (!Guid.TryParse(request.RideId, out var rideId))
            {
                return ApiResponse<bool>.Failure("Invalid Ride ID format.");
            }

            var ride = await _dbContext.Rides
                .FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken);

            if (ride == null)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.RideDoesNotExist"]);
            }

            if (ride.DriverId != driverId)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.RideNotYours"]);
            }

            if (ride.Status != RideStatus.Arrived)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.StatusNotArrived"]);
            }

    
            ride.Status = RideStatus.InProgress;
            ride.StartedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);


            return ApiResponse<bool>.Success(true, _localizer["Rides.RideStartedSuccessfully"]);
        }
    }
}