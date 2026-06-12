using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<bool>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOtpService _otpService; 
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ResetPasswordCommandHandler(
            UserManager<ApplicationUser> userManager,
            IOtpService otpService, 
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _otpService = otpService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {

            var verificationResult = await _otpService.VerifyOtpAsync(request.ResetToken, request.OtpCode, cancellationToken);

            if (!verificationResult.IsValid)
            {

                return ApiResponse<bool>.Failure(_localizer[verificationResult.ErrorMessage!]);
            }

            string userEmail = verificationResult.Data!.Email;

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
            
                return ApiResponse<bool>.Failure(_localizer["Aut.FailedResetPassword"]);
            }
            return ApiResponse<bool>.Success(true, _localizer["Auth.PasswordResetSuccessfully"]);
        }
    }
}