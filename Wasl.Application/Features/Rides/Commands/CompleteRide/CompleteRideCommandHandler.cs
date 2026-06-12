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

namespace Wasl.Application.Features.Rides.Commands.CompleteRide
{
    public class CompleteRideCommandHandler : IRequestHandler<CompleteRideCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUser;

        public CompleteRideCommandHandler(IApplicationDbContext dbContext,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUser)
        {
            _dbContext = dbContext;
            _localizer = localizer;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse<bool>> Handle(CompleteRideCommand request, CancellationToken cancellationToken)
        {
            var rideId = Guid.Parse(request.RideId);
            var ride = await _dbContext.Rides
                .FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken);

            var driverId = _currentUser.UserId();

            var apiResponse = new ApiResponse<bool>();
            apiResponse.Data = false;
            apiResponse.Succeeded = false;

            if (driverId == null)
            {
                apiResponse.Message = _localizer["Auth.Unauthenticated"];
                return apiResponse;
            }

            if (ride == null)
            {
                apiResponse.Message = _localizer["Rides.FlightDoesNotExist"];
                return apiResponse;
            }

            if (ride.DriverId != driverId)
            {
                apiResponse.Message = _localizer["Rides.FlightNotYours"];
                return apiResponse;
            }


            if (ride.Status == RideStatus.Completed || ride.Status == RideStatus.Cancelled)
            {
                apiResponse.Message = _localizer["Rides.StatusAlreadyCompleted"];
                return apiResponse;
            }

            ride.Status = RideStatus.Completed;

            await _dbContext.SaveChangesAsync(cancellationToken);

            apiResponse.Data = true;
            apiResponse.Succeeded = true;
            apiResponse.Message = _localizer["Rides.FlightCompletedSucceeded"];

            return apiResponse;
        }
    }
}