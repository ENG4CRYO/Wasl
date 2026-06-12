using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Enums;
using Wasl.Infrastructure.Data;

namespace Wasl.Infrastructure.Services;

public class RideDispatchService : IRideDispatchService
{
    private readonly IRedisCacheService _redisCache;
    private readonly IDriverNotificationService _notificationService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly AppDbContext _dbContext;

    public RideDispatchService(
        IRedisCacheService redisCache,
        IDriverNotificationService notificationService,
        IBackgroundJobClient backgroundJobClient,
        AppDbContext dbContext
        )
    {
        _redisCache = redisCache;
        _notificationService = notificationService;
        _backgroundJobClient = backgroundJobClient;
        _dbContext = dbContext;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task DispatchRideAsync(Guid rideId,
        double latitude, double longitude,
        double currentRadiusKm, List<string> excludedDriverIds)
    {
        var ride = await _dbContext.Rides.FindAsync(rideId);

        if (ride == null || ride.Status != RideStatus.Pending)
        {
            return;
        }

        if (ride.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
        {
            ride.Status = RideStatus.Cancelled; 

            await _dbContext.SaveChangesAsync();

            return;
        }

        var nearbyDrivers = await _redisCache.GetNearbyDriversAsync(longitude, latitude, currentRadiusKm);
        var driversToNotify = nearbyDrivers.Except(excludedDriverIds ?? new List<string>()).ToList();

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
        }

        var updatedExcludedList = new List<string>(excludedDriverIds ?? new List<string>());
        updatedExcludedList.AddRange(driversToNotify);

        if (currentRadiusKm < 10)
        {
            var nextRadius = currentRadiusKm + 2;
            _backgroundJobClient.Schedule(
                () => DispatchRideAsync(rideId, latitude, longitude, nextRadius, updatedExcludedList),
                TimeSpan.FromSeconds(60));
        }
    }
}