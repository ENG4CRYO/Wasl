using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Rides.Commands.StartRide
{
    public class StartRideCommand : IRequest<ApiResponse<bool>>
    {
        public string RideId { get; set; } = string.Empty;
    }
}