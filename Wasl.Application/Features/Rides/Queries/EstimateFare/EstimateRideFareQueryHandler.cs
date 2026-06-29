using MediatR;
using Microsoft.Extensions.Localization;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Queries.EstimateFare
{
    public class EstimateRideFareQueryHandler : IRequestHandler<EstimateRideFareQuery, ApiResponse<RideEstimateDto>>
    {
        private readonly IRideFareCalculator _fareCalculator;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public EstimateRideFareQueryHandler(IRideFareCalculator fareCalculator,
            IStringLocalizer<SharedResource> localizer)
        {
            _fareCalculator = fareCalculator;
            _localizer = localizer; 
        }

        public async Task<ApiResponse<RideEstimateDto>> Handle(EstimateRideFareQuery request, CancellationToken cancellationToken)
        {

            var fareResult = _fareCalculator.CalculateFare(
                request.PickupLat, request.PickupLng,
                request.DropoffLat, request.DropoffLng);

            var result = new RideEstimateDto
            {
                DistanceInKm = fareResult.DistanceKm,
                EstimatedPrice = fareResult.EstimatedFare,
                Currency = _localizer["Currency"]
            };
            return await Task.FromResult(ApiResponse<RideEstimateDto>.Success(result, _localizer["Validation.Rides.PriceCalculated"]));
        }
    }
}