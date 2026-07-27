using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Common.Models;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.CompleteRide
{
    public class CompleteRideCommandHandler : IRequestHandler<CompleteRideCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDriverNotificationService _driverNotification;
        private readonly IWalletService _walletService;
        private readonly RidePricingSettings _pricingSettings;

        public CompleteRideCommandHandler(
            IApplicationDbContext dbContext,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUserService,
            IDriverNotificationService driverNotification,
            IWalletService walletService,
            IOptions<RidePricingSettings> pricingSettings)
        {
            _dbContext = dbContext;
            _localizer = localizer;
            _currentUserService = currentUserService;
            _driverNotification = driverNotification;
            _walletService = walletService;
            _pricingSettings = pricingSettings.Value;
        }

        public async Task<ApiResponse<bool>> Handle(CompleteRideCommand request, CancellationToken cancellationToken)
        {

            var driverId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(driverId))
            {
                return ApiResponse<bool>.Failure("Unauthorized access.");
            }

            var driverProfile = await _dbContext.DriverProfiles
                .FirstOrDefaultAsync(dp => dp.UserId == driverId, cancellationToken);

            if (driverProfile == null)
            {
                return ApiResponse<bool>.Failure(_localizer["DriverProfiles.NotFound"]);
            }

            if (driverProfile.ApprovalStatus != DriverApprovalStatus.Approved)
            {
                return ApiResponse<bool>.Failure(_localizer["DriverProfile.AccountNotApproved"]);
            }

            var rideId = Guid.Parse(request.RideId);
            var ride = await _dbContext.Rides
                .FirstOrDefaultAsync(r => r.Id == rideId, cancellationToken);


            if (ride == null)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.RideDoesNotExist"]);
            }

            if (ride.DriverId != driverId)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.RideNotYours"]);
            }

            if (ride.Status != RideStatus.InProgress)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.StatusNotInProgress"]);
            }
            if (ride.Status == RideStatus.Completed || ride.Status == RideStatus.Cancelled)
            {
                return ApiResponse<bool>.Failure(_localizer["Rides.StatusAlreadyCompleted"]);
            }

            using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);
            try
            {
                ride.Status = RideStatus.Completed;
                ride.CompletedAt = DateTime.UtcNow;
                ride.TotalFare = ride.CalculatedPrice;
                ride.CompanyCommission = Math.Round(ride.TotalFare * _pricingSettings.CompanyCommissionRate, 2);
                ride.DriverNetEarnings = ride.TotalFare - ride.CompanyCommission;

                if (ride.PaymentMethod == PaymentMethod.Wallet)
                {
                    var transfer = await _walletService.TransferFundsAsync(
                        ride.RiderId, ride.DriverId, ride.TotalFare,
                        TransactionType.RidePayment, ride.Id, cancellationToken);

                    if (!transfer.IsSuccess)
                        return ApiResponse<bool>.Failure(transfer.ErrorMessage!);

                    await _walletService.DeductFundsAsync(
                        ride.DriverId, ride.CompanyCommission,
                        TransactionType.CompanyCommission, ride.Id,
                        allowNegativeBalance: true, cancellationToken);
                }
                else if (ride.PaymentMethod == PaymentMethod.Cash)
                {
                    await _walletService.DeductFundsAsync(
                        ride.DriverId, ride.CompanyCommission,
                        TransactionType.CompanyCommission, ride.Id,
                        allowNegativeBalance: true, cancellationToken);
                }
                else if (ride.PaymentMethod == PaymentMethod.Card)
                {
                    await _walletService.AddFundsAsync(
                        ride.DriverId, ride.DriverNetEarnings,
                        TransactionType.RidePayment, ride.Id, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                await _driverNotification.NotifyRiderRideCompletedAsync(ride.RiderId, ride.Id);

                return ApiResponse<bool>.Success(true, _localizer["Rides.RideCompletedSuccessfully"]);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}