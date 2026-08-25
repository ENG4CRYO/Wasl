using MediatR;
using Microsoft.Extensions.Localization;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Queries.GetMyActiveRide
{
    public class GetMyActiveRideQueryHandler : IRequestHandler<GetMyActiveRideQuery, ApiResponse<ActiveRideDto?>>
    {
        private readonly IActiveRideReader _activeRideReader;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetMyActiveRideQueryHandler(
            IActiveRideReader activeRideReader,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _activeRideReader = activeRideReader;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<ActiveRideDto?>> Handle(GetMyActiveRideQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();

            var ride = await _activeRideReader.GetActiveRideForUserAsync(userId, cancellationToken);

            return ApiResponse<ActiveRideDto?>.Success(
                ride,
                _localizer["Rides.ActiveRideRetrievedSuccessfully"]);
        }
    }
}
