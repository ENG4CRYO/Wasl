using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOtpService _otpService; 
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ForgotPasswordCommandHandler(
            UserManager<ApplicationUser> userManager,
            IOtpService otpService,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _otpService = otpService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

        
            var resetToken = Guid.NewGuid().ToString();

            if (user != null)
            {
                resetToken = await _otpService.InitiatePasswordResetAsync(
                    user.Email!,
                    user.FirstName,
                    cancellationToken);
            }

            return ApiResponse<string>.Success(resetToken, _localizer["Auth.ForgotPasswordSendOtp"]);
        }
    }
}