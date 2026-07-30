using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Profile;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Profile.Queries.GetDriverProfile
{
    public class GetDriverProfileQueryHandler : IRequestHandler<GetDriverProfileQuery, ApiResponse<DriverProfileDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetDriverProfileQueryHandler(
            UserManager<ApplicationUser> userManager,
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _context = context;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<DriverProfileDto>> Handle(GetDriverProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<DriverProfileDto>.Failure(_localizer["Auth.Unauthenticated"]);

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.DriverProfile)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
                return ApiResponse<DriverProfileDto>.Failure(_localizer["Auth.UserNotFound"]);

            if (user.DriverProfile is null)
                return ApiResponse<DriverProfileDto>.Failure(_localizer["DriverProfiles.NotFound"]);

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

            return ApiResponse<DriverProfileDto>.Success(dto, "Profile retrieved successfully");
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
