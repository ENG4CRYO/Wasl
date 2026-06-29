using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;

namespace Wasl.Application.Features.Rides.Queries.EstimateFare
{
    public class EstimateRideFareQuery : IRequest<ApiResponse<RideEstimateDto>>
    {
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public double DropoffLat { get; set; }
        public double DropoffLng { get; set; }
    }
}