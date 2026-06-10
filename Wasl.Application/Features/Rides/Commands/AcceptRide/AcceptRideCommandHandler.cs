using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands;

public class AcceptRideCommandHandler : IRequestHandler<AcceptRideCommand, bool>
{
    private readonly IRedisCacheService _redisCache;
    private readonly IApplicationDbContext _dbContext;

    public AcceptRideCommandHandler(IRedisCacheService redisCache,
        IApplicationDbContext dbContext
        )
    {
        _redisCache = redisCache;
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(AcceptRideCommand request, CancellationToken cancellationToken)
    {
        var rideId = Guid.Parse(request.RideId);
        bool isLockAcquired = await _redisCache.AcquireRideLockAsync(rideId, request.DriverId);
        if (!isLockAcquired)
        {
            return false; 
        }

        var ride = await _dbContext.Rides.FindAsync(Guid.Parse(request.RideId));

        if (ride != null && ride.Status == RideStatus.Pending)
        {
            ride.DriverId = request.DriverId;
            ride.Status = RideStatus.Accepted;
            ride.AcceptedAt = DateTime.UtcNow;  

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}