using Microsoft.Extensions.Options;
using System;
using Wasl.Application.Common;
using Wasl.Application.Common.Models;
using Wasl.Application.Interfaces.Services;

namespace Wasl.Application.Services
{
    public class RideFareCalculator : IRideFareCalculator
    {
        private readonly RidePricingSettings _settings;
        private const double EarthRadiusKm = 6371.0; 

        public RideFareCalculator(IOptions<RidePricingSettings> options)
        {
            _settings = options.Value;
        }

        public (decimal EstimatedFare, double DistanceKm) CalculateFare(double pickupLat, double pickupLng, double dropoffLat, double dropoffLng)
        {
            double distanceKm = CalculateHaversineDistance(pickupLat, pickupLng, dropoffLat, dropoffLng);

     
            double estimatedMinutes = (distanceKm / _settings.AverageCitySpeedKmh) * 60.0;

            decimal distanceCost = (decimal)distanceKm * _settings.PerKmRate;
            decimal timeCost = (decimal)estimatedMinutes * _settings.PerMinuteRate;

            decimal totalFare = _settings.BaseFare + distanceCost + timeCost;

            decimal finalFare = totalFare < _settings.MinimumFare ? _settings.MinimumFare : totalFare;


            return (MoneyHelper.RoundToIncrement(finalFare, _settings.PriceRoundingIncrement), Math.Round(distanceKm, 2));
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}