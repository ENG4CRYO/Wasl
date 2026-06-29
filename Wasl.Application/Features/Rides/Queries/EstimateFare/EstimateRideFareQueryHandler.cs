using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Interfaces.Services;

namespace Wasl.Application.Features.Rides.Queries.EstimateFare
{
    public class EstimateRideFareQueryHandler : IRequestHandler<EstimateRideFareQuery, ApiResponse<RideEstimateDto>>
    {
        // استخدام الخدمة الرائعة التي كتبتها
        private readonly IRideFareCalculator _fareCalculator;

        public EstimateRideFareQueryHandler(IRideFareCalculator fareCalculator)
        {
            _fareCalculator = fareCalculator;
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
                Currency = "IQD" 
            };
            return await Task.FromResult(ApiResponse<RideEstimateDto>.Success(result, "تم حساب التسعيرة بنجاح."));
        }
    }
}