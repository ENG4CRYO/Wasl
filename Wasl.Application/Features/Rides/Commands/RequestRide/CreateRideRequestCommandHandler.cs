using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Entities;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.RequestRide;

public class CreateRideRequestCommandHandler : IRequestHandler<CreateRideRequestCommand, Guid>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRideDispatchService _dispatchService;
    private readonly IApplicationDbContext _dbContext;

    public CreateRideRequestCommandHandler(
        IBackgroundJobClient backgroundJobClient,
        IRideDispatchService dispatchService,
        IApplicationDbContext dbContext)
    {
        _backgroundJobClient = backgroundJobClient;
        _dispatchService = dispatchService;
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateRideRequestCommand request, CancellationToken cancellationToken)
    {
        var newRideId = Guid.NewGuid();

        var ride = new Ride
        {
            Id = newRideId,
            PickupLatitude = request.PickupLatitude,
            PickupLongitude = request.PickupLongitude,
            DropoffLatitude = request.DropoffLatitude,
            DropoffLongitude = request.DropoffLongitude,
            Status = RideStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RiderId = request.RiderId 
        };

        await _dbContext.Rides.AddAsync(ride, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _backgroundJobClient.Enqueue(() =>
            _dispatchService.DispatchRideAsync(newRideId, request.PickupLatitude, request.PickupLongitude, 3.0, new List<string>())
        );

        return newRideId;
    }
}