using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IRideDispatchService
    {
        Task DispatchRideAsync(Guid rideId, double latitude, double longitude
            , double currentRadiusKm, List<string> excludedDriverIds,
            CancellationToken cancellationToken);
    }
}
