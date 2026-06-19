using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.RequestRide;

public class CreateRideRequestCommandHandler : IRequestHandler<CreateRideRequestCommand, ApiResponse<Guid>>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRideDispatchService _dispatchService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRideFareCalculator _priceCalculator;

    public CreateRideRequestCommandHandler(
        IBackgroundJobClient backgroundJobClient,
        IRideDispatchService dispatchService,
        IApplicationDbContext dbContext,
        IStringLocalizer<SharedResource> localizer,
        ICurrentUserService currentUserService,
        IRideFareCalculator priceCalculator)
    {
        _backgroundJobClient = backgroundJobClient;
        _dispatchService = dispatchService;
        _dbContext = dbContext;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _priceCalculator = priceCalculator;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateRideRequestCommand request, CancellationToken cancellationToken)
    {
        var newRideId = Guid.CreateVersion7();

        var riderId = _currentUserService.UserId();
        if (string.IsNullOrEmpty(riderId))
        {
            return ApiResponse<Guid>.Failure(_localizer["Auth.Unauthenticated"]);
        }
        var (fare, distance) = _priceCalculator.CalculateFare(
        request.PickupLatitude, request.PickupLongitude,
        request.DropoffLatitude, request.DropoffLongitude);
        var ride = new Ride
        {
            Id = newRideId,
            PickupLatitude = request.PickupLatitude,
            PickupLongitude = request.PickupLongitude,
            DropoffLatitude = request.DropoffLatitude,
            DropoffLongitude = request.DropoffLongitude,
            Status = RideStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RequestedAt = DateTime.UtcNow,
            CalculatedPrice = fare,
            RiderId = riderId
        };

        await _dbContext.Rides.AddAsync(ride, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _backgroundJobClient.Enqueue(() =>
            _dispatchService.DispatchRideAsync(newRideId, request.PickupLatitude, request.PickupLongitude, 3.0,CancellationToken.None)
        );

        return ApiResponse<Guid>.Success(newRideId, _localizer["Rides.RequestRideReceivedSuccessfully"]);
    }
}