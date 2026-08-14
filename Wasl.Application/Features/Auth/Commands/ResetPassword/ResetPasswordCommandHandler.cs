using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<bool>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ResetPasswordCommandHandler(
            UserManager<ApplicationUser> userManager,
            ICacheService cacheService,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _cacheService = cacheService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var userEmail = await _cacheService.GetAsync<string>($"ValidatedResetSession:{request.Token}", cancellationToken);

            if (string.IsNullOrEmpty(userEmail))
            {
                return ApiResponse<bool>.Failure(_localizer["Auth.PasswordResetSessionExpiredOrInvalid"]);
            }

            var user = await _userManager.FindByEmailAsync(userEmail);
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

                return ApiResponse<bool>.Failure(_localizer["Auth.ResetPasswordFailed"]);
            }

            await _cacheService.RemoveAsync($"ValidatedResetSession:{request.Token}", cancellationToken);

            return ApiResponse<bool>.Success(true, _localizer["Auth.PasswordResetSuccessfully"]);
        }
    }
}