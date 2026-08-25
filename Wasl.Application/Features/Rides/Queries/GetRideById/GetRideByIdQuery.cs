using MediatR;
using System;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;

namespace Wasl.Application.Features.Rides.Queries.GetRideById
{
    /// <summary>
    /// Returns full ride details for a participant (Rider or Driver).
    /// Used as a REST fallback to recover ride state after disconnection.
    /// </summary>
    public class GetRideByIdQuery : IRequest<ApiResponse<ActiveRideDto>>
    {
        public Guid RideId { get; set; }
    }
}
