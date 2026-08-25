using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;

namespace Wasl.Application.Features.Rides.Queries.GetMyActiveRide
{
    /// <summary>
    /// Returns the current user's active ride (Pending/Accepted/Arrived/InProgress) or null if none.
    /// Primary recovery source for cold start / app restart.
    /// </summary>
    public class GetMyActiveRideQuery : IRequest<ApiResponse<ActiveRideDto?>>
    {
    }
}
