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

namespace Wasl.Application.Features.Rides.Commands.DriverArrived
{
    public class DriverArrivedCommandHandler : IRequestHandler<DriverArrivedCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDriverNotificationService _driverNotification;

        public DriverArrivedCommandHandler(
            IApplicationDbContext dbContext,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUserService,
            IDriverNotificationService driverNotification)
        {
            _dbContext = dbContext;
            _localizer = localizer;
            _currentUserService = currentUserService;
            _driverNotification = driverNotification;
        }

        public async Task<ApiResponse<bool>> Handle(DriverArrivedCommand request, CancellationToken cancellationToken)
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

            var rideId = Guid.Parse(request.RideId);
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

            if (ride.Status != RideStatus.Accepted)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.StatusNotAccepted"]);
            }

            ride.Status = RideStatus.Arrived;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _driverNotification.NotifyRiderDriverArrivedAsync(ride.RiderId, ride.Id);


            return ApiResponse<bool>.Success(true, _localizer["Rides.DriverArrivedSuccessfully"]);
        }
    }
}