using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.DriverArrived
{
    public class DriverArrivedCommandHandler : IRequestHandler<DriverArrivedCommand,ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUser;
        public DriverArrivedCommandHandler(IApplicationDbContext dbContext,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUser)
        {
            _dbContext = dbContext;
            _localizer = localizer;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse<bool>> Handle(DriverArrivedCommand request, CancellationToken cancellationToken)
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

            if (ride.Status != RideStatus.Accepted)
            {
                apiResponse.Message = _localizer["Rides.StatusNotAccepted"];
                return apiResponse;
            }
            ride.Status = RideStatus.Arrived;

            await _dbContext.SaveChangesAsync(cancellationToken);
            apiResponse.Data = true;
            apiResponse.Succeeded = true;
            apiResponse.Message = _localizer["Rides.FlightAcceptanceSucceeded"];

            return apiResponse;
        }
    }
}
