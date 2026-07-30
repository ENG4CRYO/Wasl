using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Profile;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Profile.Queries.GetRiderProfile
{
    public class GetRiderProfileQueryHandler : IRequestHandler<GetRiderProfileQuery, ApiResponse<RiderProfileDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetRiderProfileQueryHandler(
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<RiderProfileDto>> Handle(GetRiderProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<RiderProfileDto>.Failure(_localizer["Auth.Unauthenticated"]);

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return ApiResponse<RiderProfileDto>.Failure(_localizer["Auth.UserNotFound"]);

            var profilePictureUrl = ParseProfilePicture(user.ProfilePictureUrls);

            var dto = new RiderProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                ProfilePictureUrl = profilePictureUrl,
                Balance = user.Balance
            };

            return ApiResponse<RiderProfileDto>.Success(dto, "Profile retrieved successfully");
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
