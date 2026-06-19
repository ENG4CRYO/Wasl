using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Interfaces.Services
{
    public interface IRideFareCalculator
    {
        (decimal EstimatedFare, double DistanceKm)
            CalculateFare(double pickupLat, double pickupLng,
            double dropoffLat, double dropoffLng);
    }
}
