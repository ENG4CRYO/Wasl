using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Profile;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Profile.Commands.UpdateDriverProfile
{
    public class UpdateDriverProfileCommandHandler : IRequestHandler<UpdateDriverProfileCommand, ApiResponse<DriverProfileDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UpdateDriverProfileCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<DriverProfileDto>> Handle(UpdateDriverProfileCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<DriverProfileDto>.Failure(_localizer["Auth.Unauthenticated"]);

            var user = await _context.Users
                .Include(u => u.DriverProfile)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
                return ApiResponse<DriverProfileDto>.Failure(_localizer["Auth.UserNotFound"]);

            if (user.DriverProfile is null)
                return ApiResponse<DriverProfileDto>.Failure(_localizer["DriverProfiles.NotFound"]);

            var phoneTaken = await _context.Users
                .AnyAsync(u => u.Id != userId && u.PhoneNumber == request.PhoneNumber, cancellationToken);

            if (phoneTaken)
                return ApiResponse<DriverProfileDto>.Failure(_localizer["Profile.PhoneNumberAlreadyTaken"]);

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;

            await _context.SaveChangesAsync(cancellationToken);

            var profilePictureUrl = ParseProfilePicture(user.ProfilePictureUrls);

            var dto = new DriverProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                ProfilePictureUrl = profilePictureUrl,
                AverageRating = user.DriverProfile.AverageRating,
                TotalReviews = user.DriverProfile.TotalReviews,
                ApprovalStatus = user.DriverProfile.ApprovalStatus,
                City = user.City,
                Address = user.Address,
                Balance = user.Balance
            };

            return ApiResponse<DriverProfileDto>.Success(dto, _localizer["Profile.UpdatedSuccessfully"]);
        }

        private static string? ParseProfilePicture(string? profilePictureUrls)
        {
            if (string.IsNullOrWhiteSpace(profilePictureUrls))
                return null;

            var urls = profilePictureUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return urls.Length > 0 ? urls[0] : null;
        }
    }
}