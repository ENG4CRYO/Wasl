using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.DriverProfiles.Commands.SubmitProfile
{
    public class SubmitDriverProfileCommandHandler : IRequestHandler<SubmitDriverProfileCommand, ApiResponse<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileService _fileService;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICacheService _cacheService;

        public SubmitDriverProfileCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IFileService fileService,
            IStringLocalizer<SharedResource> localizer,
             ICacheService cacheService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _fileService = fileService;
            _localizer = localizer;
            _cacheService = cacheService;
        }

        public async Task<ApiResponse<string>> Handle(SubmitDriverProfileCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse<string>.Failure(_localizer["DriverProfiles.Unauthorized"]);
            }

            var driverProfile = await _context.DriverProfiles
                .Include(dp => dp.User)
                .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

            if (driverProfile == null)
            {
                return ApiResponse<string>.Failure(_localizer["DriverProfiles.NotFound"]);
            }

            if (driverProfile.ApprovalStatus == DriverApprovalStatus.UnderReview)
            {
                return ApiResponse<string>.Failure(_localizer["DriverProfiles.AlreadyUnderReview"]);
            }

            if (driverProfile.ApprovalStatus == DriverApprovalStatus.Approved)
            {
                return ApiResponse<string>.Failure(_localizer["DriverProfiles.AlreadyApproved"]);
            }


            if (request.VehicleImage == null ||
                request.LicenseFrontImage == null ||
                request.LicenseBackImage == null ||
                request.SelfieImage == null)
            {
               
                return ApiResponse<string>.Failure(_localizer["DriverProfiles.MissingRequiredFiles"]);
            }


            var vehicleUrl = await _fileService.SaveFileAsync(request.VehicleImage!, "drivers/vehicles", cancellationToken);
            var licenseFrontUrl = await _fileService.SaveFileAsync(request.LicenseFrontImage!, "drivers/licenses", cancellationToken);
            var licenseBackUrl = await _fileService.SaveFileAsync(request.LicenseBackImage!, "drivers/licenses", cancellationToken);
            var selfieUrl = await _fileService.SaveFileAsync(request.SelfieImage!, "drivers/selfies", cancellationToken);

            driverProfile.VehicleModel = request.VehicleModel;
            driverProfile.VehicleYear = request.VehicleYear;
            driverProfile.VinNumber = request.VinNumber;

            driverProfile.VehicleImagesUrl = vehicleUrl;
            driverProfile.LicenseFrontUrl = licenseFrontUrl;
            driverProfile.LicenseBackUrl = licenseBackUrl;
            driverProfile.SelfieUrl = selfieUrl;
            driverProfile.User.ProfilePictureUrls = selfieUrl;

            driverProfile.ApprovalStatus = DriverApprovalStatus.UnderReview;
            await _cacheService.RemoveAsync($"DriverStatus:{userId}", cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<string>.Success(_localizer["DriverProfiles.SubmitSuccess"]);
        }
    }
}