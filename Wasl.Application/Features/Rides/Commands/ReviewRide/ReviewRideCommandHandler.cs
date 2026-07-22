using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common; 
using Wasl.Application.Interfaces.Infrastructure; 
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.ReviewRide
{
    public class ReviewRideCommandHandler : IRequestHandler<ReviewRideCommand, ApiResponse<bool>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUserService;

        public ReviewRideCommandHandler(IApplicationDbContext dbContext, IStringLocalizer<SharedResource> localizer, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _localizer = localizer;
            _currentUserService = currentUserService;
        }

        public async Task<ApiResponse<bool>> Handle(ReviewRideCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId();

            if (string.IsNullOrEmpty(currentUserId))
                return ApiResponse<bool>.Failure(_localizer["DriverProfiles.Unauthorized"]);


            var ride = await _dbContext.Rides
                .FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken);

            if (ride == null)
                return ApiResponse<bool>.Failure(_localizer["Rides.RideDoesNotExist"]);

            if (ride.RiderId != currentUserId)
                return ApiResponse<bool>.Failure(_localizer["Rides.ReviewNotOwnRide"]);

            if (ride.Status != RideStatus.Completed)
                return ApiResponse<bool>.Failure(_localizer["Rides.ReviewOnlyCompleted"]);

            var alreadyReviewed = await _dbContext.RideReviews
                .AnyAsync(r => r.RideId == request.RideId, cancellationToken);

            if (alreadyReviewed)
                return ApiResponse<bool>.Failure(_localizer["Rides.AlreadyReviewed"]);

            var review = new RideReview
            {
                RideId = request.RideId,
                RiderId = currentUserId,
                DriverId = ride.DriverId!, 
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.RideReviews.Add(review);

            await UpdateDriverAverageRating(ride.DriverId, request.Rating, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.Success(true, _localizer["Rides.ReviewSubmittedSuccessfully"]);
        }

        private async Task UpdateDriverAverageRating(string driverId, int newRating, CancellationToken cancellationToken)
        {
            var driverProfile = await _dbContext.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == driverId, cancellationToken);

            if (driverProfile != null)
            {
                var currentTotal = driverProfile.TotalReviews;
                var currentAvg = driverProfile.AverageRating;

                driverProfile.AverageRating = ((currentAvg * currentTotal) + newRating) / (currentTotal + 1);
                driverProfile.TotalReviews += 1;
            }
        }
    }
}