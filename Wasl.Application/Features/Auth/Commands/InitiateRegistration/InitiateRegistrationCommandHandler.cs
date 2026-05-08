using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Wasl.Application.Features.Auth.Commands.InitiateRegistration
{
    public class InitiateRegistrationCommandHandler : IRequestHandler<InitiateRegistrationCommand, ApiResponse<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ITemplateService _templateService;

        public InitiateRegistrationCommandHandler(
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

        public async Task<ApiResponse<string>> Handle(InitiateRegistrationCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return ApiResponse<string>.Failure(_localizer["Auth.EmailAlreadyRegistered"]);
            }

            var existingUsername = await _userManager.FindByNameAsync(request.UserName);
            if (existingUsername != null)
            {
                return ApiResponse<string>.Failure(_localizer["Auth.UserNameAlreadyTaken"]);
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

            var passwordHash = _userManager.PasswordHasher.HashPassword(null!, request.Password);

            var pendingUser = new PendingRegistrationDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Username = request.UserName,
                PasswordHash = passwordHash,
                OtpCode = otpCode
            };

            await _cacheService.SetAsync(registerToken, pendingUser, TimeSpan.FromMinutes(10), cancellationToken);

            if (!isTestEmail)
            {
                var emailPlaceholders = new Dictionary<string, string>
                {
                     { "FirstName", request.FirstName },
                     { "OtpCode", otpCode }
                };

                var emailBody = await _templateService.GetTemplateAsync("OtpEmail", emailPlaceholders);

                await _emailService.SendEmailAsync(request.Email, "Your Secure OTP Code", emailBody, cancellationToken);
            }
         
            return ApiResponse<string>.Success(registerToken, _localizer["Auth.RegisterSendOtp"]);
        }
    }
}