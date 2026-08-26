using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Tracking.Commands.UpdateDriverLocation
{
    public class UpdateDriverLocationCommand : IRequest<ApiResponse<bool>>
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
