using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Rides;
using Wasl.Application.Interfaces;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Queries.GetRideById
{
    public class GetRideByIdQueryHandler : IRequestHandler<GetRideByIdQuery, ApiResponse<ActiveRideDto>>
    {
        private readonly IActiveRideReader _activeRideReader;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetRideByIdQueryHandler(
            IActiveRideReader activeRideReader,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _activeRideReader = activeRideReader;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<ActiveRideDto>> Handle(GetRideByIdQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();

            var ride = await _activeRideReader.GetRideIfParticipantAsync(userId, request.RideId, cancellationToken);

            if (ride == null)
            {
                return ApiResponse<ActiveRideDto>.Failure(_localizer["Rides.NotFound"]);
            }

            return ApiResponse<ActiveRideDto>.Success(
                ride,
                _localizer["Rides.ActiveRideRetrievedSuccessfully"]);
        }
    }
}
