using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wasl.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<bool>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager,
            ICacheService cacheService,
            IStringLocalizer<SharedResource> localizer)
        { 
            _userManager = userManager;
            _cacheService = cacheService;
            _localizer = localizer;

        }

        public async Task<ApiResponse<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var cacheData = await _cacheService.GetAsync<ResetPasswordCacheDto>(request.ResetToken, cancellationToken);

            if (cacheData == null)
            {
                return ApiResponse<bool>.Failure(_localizer["Auth.SessionExpiredOrInvalidToken"]);
            }

            if (cacheData.OtpCode != request.OtpCode)
            {
                return ApiResponse<bool>.Failure(_localizer["Auth.InvalidOTP"]);
            }
            var user = await _userManager.FindByEmailAsync(cacheData.Email);
            if (user == null)
            {
                return ApiResponse<bool>.Failure(_localizer["Auth.UserNotFound"]);
            }

            var isSameAsOldPassword = await _userManager.CheckPasswordAsync(user, request.NewPassword);
            if (isSameAsOldPassword)
            {
                return ApiResponse<bool>.Failure(_localizer["Auth.NewPasswordSameAsOld"]);
            }

            var identityResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, identityResetToken, request.NewPassword);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join(" | ", resetResult.Errors.Select(e => e.Description));
                return ApiResponse<bool>.Failure(_localizer["Aut.FailedResetPassword"]);
            }

            await _cacheService.RemoveAsync(request.ResetToken, cancellationToken);

            return ApiResponse<bool>.Success(true, _localizer["Auth.PasswordResetSuccessfully"]);
        }
    }
}