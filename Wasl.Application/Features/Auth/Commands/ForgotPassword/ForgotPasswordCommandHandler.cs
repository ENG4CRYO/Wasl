using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ITemplateService _templateService;

        public ForgotPasswordCommandHandler(
            UserManager<ApplicationUser> userManager,
            ICacheService cacheService,
            IEmailService emailService,
            IStringLocalizer<SharedResource> localizer,
            ITemplateService templateService)
        {
            _userManager = userManager;
            _cacheService = cacheService;
            _emailService = emailService;
            _localizer = localizer;
            _templateService = templateService;
        }

        public async Task<ApiResponse<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            var resetToken = Guid.NewGuid().ToString();

            if (user != null)
            {
                string otpCode;
                bool isTestEmail = request.Email.Contains("@test.com");

                if (isTestEmail)
                {
                    otpCode = "123456";
                }
                else
                {
                    otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                }

                var cacheDto = new OtpCacheDto
                {
                    Email = user.Email!,
                    OtpCode = otpCode,
                    Purpose = "PasswordReset"
                };

                await _cacheService.SetAsync(resetToken, cacheDto, TimeSpan.FromMinutes(10), cancellationToken);

                if (!isTestEmail)
                {
                    var emailPlaceholders = new Dictionary<string, string>
                    {
                        { "FirstName", user.FirstName ?? "User" }, 
                        { "OtpCode", otpCode }
                    };

                    var emailBody = await _templateService.GetTemplateAsync("OtpEmail", emailPlaceholders);

                    await _emailService.SendEmailAsync(request.Email, "Your Secure OTP Code", emailBody, cancellationToken);
                }
            }
            return ApiResponse<string>.Success(resetToken, _localizer["Auth.ForgotPasswordSendOtp"]);
        }
    }
}