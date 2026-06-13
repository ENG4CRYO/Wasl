using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Enums;

namespace Wasl.Infrastructure.Services;

public class RideDispatchService : IRideDispatchService
{
    private readonly IRedisCacheService _redisCache;
    private readonly IDriverNotificationService _notificationService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IApplicationDbContext _dbContext;

    public RideDispatchService(
        IRedisCacheService redisCache,
        IDriverNotificationService notificationService,
        IBackgroundJobClient backgroundJobClient,
        IApplicationDbContext dbContext)
    {
        _redisCache = redisCache;
        _notificationService = notificationService;
        _backgroundJobClient = backgroundJobClient;
        _dbContext = dbContext;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task DispatchRideAsync(Guid rideId,
        double latitude, double longitude,
        double currentRadiusKm,
        CancellationToken cancellationToken = default)
    {
        var ride = await _dbContext.Rides.FindAsync(new object[] { rideId }, cancellationToken);

        if (ride == null || ride.Status != RideStatus.Pending)
        {
            return;
        }

        if (ride.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
        {
            ride.Status = RideStatus.Cancelled;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var nearbyDrivers = await _redisCache.GetNearbyDriversAsync(longitude, latitude, currentRadiusKm);


        var excludedDriverIds = await _redisCache.GetExcludedDriversForRideAsync(rideId);

        var driversToNotify = nearbyDrivers.Except(excludedDriverIds).ToList();

        if (driversToNotify.Any())
        {
            await _notificationService.NotifyDriversWithRideRequestAsync(
                driversToNotify,
                rideId,
                ride.PickupLatitude,
                ride.PickupLongitude,
                ride.DropoffLatitude,
                ride.DropoffLongitude
            );

            await _redisCache.AddExcludedDriversToRideAsync(rideId, driversToNotify);
        }

        if (currentRadiusKm < 10)
        {
            var nextRadius = currentRadiusKm + 2;

            _backgroundJobClient.Schedule(
                () => DispatchRideAsync(rideId, latitude, longitude, nextRadius, CancellationToken.None),
                TimeSpan.FromSeconds(60));
        }
    }
}