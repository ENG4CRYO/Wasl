using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.ChangePaymentMethod
{
    public class ChangeRidePaymentMethodCommandHandler : IRequestHandler<ChangeRidePaymentMethodCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ChangeRidePaymentMethodCommandHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<bool>> Handle(ChangeRidePaymentMethodCommand request, CancellationToken cancellationToken)
        {
            var driverId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(driverId))
                return ApiResponse<bool>.Failure("Unauthorized access.");

            var ride = await _dbContext.Rides
                .FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken);

            if (ride == null)
                return ApiResponse<bool>.Failure(_localizer["Rides.RideDoesNotExist"]);

            if (ride.DriverId != driverId)
                return ApiResponse<bool>.Failure(_localizer["Rides.RideNotYours"]);

            if (ride.Status != RideStatus.InProgress)
                return ApiResponse<bool>.Failure(_localizer["Rides.StatusNotInProgress"]);

            ride.PaymentMethod = request.NewPaymentMethod;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Success(true, _localizer["Rides.PaymentMethodChanged"]);
        }
    }
}
