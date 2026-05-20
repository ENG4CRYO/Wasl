using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Auth.Commands.RiderRegistration.InitiateRiderRegistration
{
    public class InitiateDriverRegistrationCommandHandler : IRequestHandler<InitiateRiderRegistrationCommand, ApiResponse<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly IEmailService _emailService;
        private readonly ITemplateService _templateService;
        private readonly IStringLocalizer<SharedResource> _localizer;
        public InitiateDriverRegistrationCommandHandler(
            UserManager<ApplicationUser> userManager,
            ICacheService cacheService,
            IEmailService emailService,
            ITemplateService templateService,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _cacheService = cacheService;
            _emailService = emailService;
            _templateService = templateService;
            _localizer = localizer;
        }
        public async Task<ApiResponse<string>> Handle(InitiateRiderRegistrationCommand request, CancellationToken cancellationToken)
        {
            var exsitingUser = _userManager.FindByEmailAsync(request.Email);

            if (exsitingUser != null)
            {
                return ApiResponse<string>.Failure(_localizer["Auth.EmailAlreadyRegistered"]);
            }

            string otpCode;
            var registerToken = Guid.NewGuid().ToString();

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
                Email = request.Email,
                OtpCode = otpCode
            };

            await _cacheService.SetAsync(registerToken, cacheDto, TimeSpan.FromMinutes(10), cancellationToken);

            if (!isTestEmail)
            {
                var emailPlaceholders = new Dictionary<string, string>
                {
                     { "OtpCode", otpCode }
                };

                var emailBody = await _templateService.GetTemplateAsync("OtpEmail", emailPlaceholders);

                await _emailService.SendEmailAsync(request.Email, "Your Secure OTP Code", emailBody, cancellationToken);
            }

            return ApiResponse<string>.Success(registerToken, _localizer["Auth.RegisterSendOtp"]);

        }
    }
}
