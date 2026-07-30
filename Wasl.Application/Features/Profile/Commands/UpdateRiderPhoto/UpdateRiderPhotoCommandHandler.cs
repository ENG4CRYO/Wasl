using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Profile.Commands.UpdateRiderPhoto
{
    public class UpdateRiderPhotoCommandHandler : IRequestHandler<UpdateRiderPhotoCommand, ApiResponse<string>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UpdateRiderPhotoCommandHandler(
            ICurrentUserService currentUserService,
            IApplicationDbContext context,
            IFileService fileService,
            IStringLocalizer<SharedResource> localizer)
        {
            _currentUserService = currentUserService;
            _context = context;
            _fileService = fileService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<string>> Handle(UpdateRiderPhotoCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<string>.Failure(_localizer["Auth.Unauthenticated"]);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
                return ApiResponse<string>.Failure(_localizer["Auth.UserNotFound"]);

            if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrls))
            {
                var oldUrls = user.ProfilePictureUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var url in oldUrls)
                {
                    _fileService.DeleteFile(url);
                }
            }

            var newUrl = await _fileService.SaveFileAsync(request.Photo, "riders/profiles", cancellationToken);

            user.ProfilePictureUrls = newUrl;
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<string>.Success(newUrl, "Profile photo updated successfully");
        }
    }
}
