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
        public DriverArrivedCommandHandler(IApplicationDbContext dbContext, IStringLocalizer<SharedResource> localizer)
        {
            _dbContext = dbContext;
            _localizer = localizer;
        }

        public async Task<ApiResponse<bool>> Handle(DriverArrivedCommand request, CancellationToken cancellationToken)
        {
            var rideId = Guid.Parse(request.RideId);
            var ride = await _dbContext.Rides
                .FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken);

            var apiResposne = new ApiResponse<bool>();
            apiResposne.Data = false;
            apiResposne.Succeeded = false;


            if (ride == null)
            {
                apiResposne.Message = _localizer["Rides.FlightDoesNotExist"];
                return apiResposne;
            }

            if (ride.DriverId != request.DriverId)
            {
                apiResposne.Message = _localizer["Rides.FlightNotYours"];
                return apiResposne;
            }

            if (ride.Status != RideStatus.Accepted)
            {
                apiResposne.Message = _localizer["Rides.StatusNotAccepted"];
                return apiResposne;
            }
            ride.Status = RideStatus.Arrived;

            await _dbContext.SaveChangesAsync(cancellationToken);
            apiResposne.Data = true;
            apiResposne.Succeeded = true;
            apiResposne.Message = _localizer["Rides.FlightAcceptanceSucceeded"];

            return apiResposne;
        }
    }
}
