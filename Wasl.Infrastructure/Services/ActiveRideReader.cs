using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Core.Entities;
using Wasl.Core.Enums;

namespace Wasl.Infrastructure.Services
{
    /// <summary>
    /// Single source of truth for recovering authoritative ride state.
    /// Reads from the database and enriches with the driver's live location from Redis.
    /// </summary>
    public class ActiveRideReader : IActiveRideReader
    {
        private static readonly RideStatus[] RecoverableStatuses =
        {
            RideStatus.Pending, RideStatus.Accepted, RideStatus.Arrived, RideStatus.InProgress
        };

        private readonly IApplicationDbContext _context;
        private readonly IRedisCacheService _redisCache;

        public ActiveRideReader(IApplicationDbContext context, IRedisCacheService redisCache)
        {
            _context = context;
            _redisCache = redisCache;
        }

        public async Task<ActiveRideDto?> GetActiveRideForUserAsync(string? userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            var query = _context.Rides
                .AsNoTracking()
                .Where(r => (r.RiderId == userId || r.DriverId == userId) && RecoverableStatuses.Contains(r.Status))
                .OrderByDescending(r => r.RequestedAt);

            var row = await ProjectAsync(query, cancellationToken);

            if (row == null) return null;

            return await MapToDtoAsync(row, cancellationToken);
        }

        public async Task<ActiveRideDto?> GetRideIfParticipantAsync(string? userId, Guid rideId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            var query = _context.Rides
                .AsNoTracking()
                .Where(r => r.Id == rideId &&
                            (r.RiderId == userId || r.DriverId == userId));

            var row = await ProjectAsync(query, cancellationToken);

            if (row == null) return null;

            return await MapToDtoAsync(row, cancellationToken);
        }

        public async Task<Guid?> GetActiveRideIdForDriverAsync(string? driverId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(driverId)) return null;

            return await _context.Rides
                .AsNoTracking()
                .Where(r => r.DriverId == driverId &&
                            (r.Status == RideStatus.Accepted ||
                             r.Status == RideStatus.Arrived ||
                             r.Status == RideStatus.InProgress))
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static async Task<RawRideRow?> ProjectAsync(IQueryable<Ride> query, CancellationToken cancellationToken)
        {
            return await query
                .Select(r => new RawRideRow
                {
                    Id = r.Id,
                    Status = r.Status,
                    PaymentMethod = r.PaymentMethod,
                    PickupLatitude = r.PickupLatitude,
                    PickupLongitude = r.PickupLongitude,
                    DropoffLatitude = r.DropoffLatitude,
                    DropoffLongitude = r.DropoffLongitude,
                    CalculatedPrice = r.CalculatedPrice,
                    RequestedAt = r.RequestedAt,
                    AcceptedAt = r.AcceptedAt,
                    StartedAt = r.StartedAt,
                    RiderId = r.RiderId,
                    RiderName = (r.Rider.FirstName ?? "") + " " + (r.Rider.LastName ?? ""),
                    RiderPhone = r.Rider.PhoneNumber ?? "",
                    DriverId = r.DriverId,
                    DriverName = r.Driver != null
                        ? (r.Driver.FirstName ?? "") + " " + (r.Driver.LastName ?? "")
                        : "",
                    DriverPhone = r.Driver != null ? (r.Driver.PhoneNumber ?? "") : "",
                    VehicleModel = r.Driver != null && r.Driver.DriverProfile != null ? r.Driver.DriverProfile.VehicleModel : "",
                    VehicleYear = r.Driver != null && r.Driver.DriverProfile != null ? r.Driver.DriverProfile.VehicleYear : 0,
                    VinNumber = r.Driver != null && r.Driver.DriverProfile != null ? r.Driver.DriverProfile.VinNumber : ""
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<ActiveRideDto> MapToDtoAsync(RawRideRow row, CancellationToken cancellationToken)
        {
            var dto = new ActiveRideDto
            {
                RideId = row.Id,
                Status = (int)row.Status,
                StatusName = row.Status.ToString(),
                PaymentMethod = row.PaymentMethod.ToString(),
                PickupLatitude = row.PickupLatitude,
                PickupLongitude = row.PickupLongitude,
                DropoffLatitude = row.DropoffLatitude,
                DropoffLongitude = row.DropoffLongitude,
                CalculatedPrice = row.CalculatedPrice,
                RequestedAt = row.RequestedAt,
                AcceptedAt = row.AcceptedAt,
                StartedAt = row.StartedAt,
                RiderId = row.RiderId,
                RiderName = row.RiderName.Trim(),
                RiderPhone = row.RiderPhone,
                DriverId = row.DriverId,
                DriverName = row.DriverName.Trim(),
                DriverPhone = row.DriverPhone,
                VehicleModel = row.VehicleModel,
                VehicleYear = row.VehicleYear,
                VinNumber = row.VinNumber
            };

            if (!string.IsNullOrEmpty(dto.DriverId))
            {
                var location = await _redisCache.GetDriverLocationAsync(dto.DriverId);
                dto.DriverLatitude = location?.Latitude;
                dto.DriverLongitude = location?.Longitude;
            }

            return dto;
        }

        private sealed class RawRideRow
        {
            public Guid Id { get; set; }
            public RideStatus Status { get; set; }
            public PaymentMethod PaymentMethod { get; set; }
            public double PickupLatitude { get; set; }
            public double PickupLongitude { get; set; }
            public double DropoffLatitude { get; set; }
            public double DropoffLongitude { get; set; }
            public decimal CalculatedPrice { get; set; }
            public DateTime RequestedAt { get; set; }
            public DateTime? AcceptedAt { get; set; }
            public DateTime? StartedAt { get; set; }
            public string RiderId { get; set; } = string.Empty;
            public string RiderName { get; set; } = string.Empty;
            public string RiderPhone { get; set; } = string.Empty;
            public string? DriverId { get; set; }
            public string DriverName { get; set; } = string.Empty;
            public string DriverPhone { get; set; } = string.Empty;
            public string VehicleModel { get; set; } = string.Empty;
            public int VehicleYear { get; set; }
            public string VinNumber { get; set; } = string.Empty;
        }
    }
}
