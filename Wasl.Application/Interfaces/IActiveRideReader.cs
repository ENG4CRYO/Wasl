using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Dtos.Rides;

namespace Wasl.Application.Interfaces
{
    /// <summary>
    /// Reads the authoritative ride state (DB + Redis driver location)
    /// used by REST recovery endpoints and SignalR reconnection sync.
    /// </summary>
    public interface IActiveRideReader
    {
        /// <summary>
        /// Returns the caller's current active ride (Pending/Accepted/Arrived/InProgress), or null if none.
        /// </summary>
        Task<ActiveRideDto?> GetActiveRideForUserAsync(string? userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the ride only if the user participates in it; otherwise null.
        /// </summary>
        Task<ActiveRideDto?> GetRideIfParticipantAsync(string? userId, Guid rideId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the id of the caller's active ride (Accepted/Arrived/InProgress) if any.
        /// </summary>
        Task<Guid?> GetActiveRideIdForDriverAsync(string? driverId, CancellationToken cancellationToken = default);
    }
}
