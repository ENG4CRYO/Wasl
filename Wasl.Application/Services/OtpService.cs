using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Interfaces.Services;
using Wasl.Core.Entities;

namespace Wasl.Application.Services
{
    public class OtpService : IOtpService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly IEmailService _emailService;
        private readonly ITemplateService _templateService;
        private readonly IConfiguration _configuration;

        public OtpService(
            UserManager<ApplicationUser> userManager,
            ICacheService cacheService,
            IEmailService emailService,
            ITemplateService templateService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _cacheService = cacheService;
            _emailService = emailService;
            _templateService = templateService;
            _configuration = configuration;
        }

        public async Task<string?> InitiateRegistrationAsync(string email, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return null; 
            }

            string otpCode;
            var registerToken = Guid.NewGuid().ToString();

           
            bool bypassEnabled = _configuration.GetValue<bool>("Testing:BypassOtp");
            string bypassDomain = _configuration.GetValue<string>("Testing:BypassDomain") ?? "@test.com";
            string fixedOtp = _configuration.GetValue<string>("Testing:FixedOtpCode") ?? "123456";

            bool isBypassEmail = bypassEnabled && email.EndsWith(bypassDomain, StringComparison.OrdinalIgnoreCase);

            if (isBypassEmail)
            {
                otpCode = fixedOtp;
            }
            else
            {
                otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            }

            var cacheDto = new OtpCacheDto
            {
                Email = email,
                OtpCode = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            await _cacheService.SetAsync(registerToken, cacheDto, TimeSpan.FromMinutes(10), cancellationToken);

            if (!isBypassEmail)
            {
                var emailPlaceholders = new Dictionary<string, string>
                {
                     { "OtpCode", otpCode }
                };

                var emailBody = await _templateService.GetTemplateAsync("OtpEmail", emailPlaceholders);

                await _emailService.SendEmailAsync(email, "Your Secure OTP Code", emailBody, cancellationToken);
            }

            return registerToken;
        }
        public async Task<(bool IsValid, string? ErrorMessage, OtpCacheDto? Data)> VerifyOtpAsync(string token, string providedOtp, CancellationToken cancellationToken = default)
        {
            var cachedData = await _cacheService.GetAsync<OtpCacheDto>(token, cancellationToken);

            if (cachedData == null)
            {
                return (false, "Auth.SessionExpiredOrInvalidToken", null);
            }

            var remainingTime = cachedData.ExpiresAt - DateTime.UtcNow;

            if (remainingTime <= TimeSpan.Zero)
            {
                await _cacheService.RemoveAsync(token, cancellationToken);
                return (false, "Auth.SessionExpiredOrInvalidToken", null);
            }

            if (cachedData.FailedAttempts >= 5)
            {
                await _cacheService.RemoveAsync(token, cancellationToken);
                return (false, "Auth.MaxOtpAttemptsReached", null);
            }

            if (cachedData.OtpCode != providedOtp)
            {
                cachedData.FailedAttempts++;

                
                await _cacheService.SetAsync(token, cachedData, remainingTime, cancellationToken);

                return (false, "Auth.InvalidOTP", null);
            }

            await _cacheService.RemoveAsync(token, cancellationToken);

            return (true, null, cachedData);
        }

        public async Task<string> InitiatePasswordResetAsync(string email, string firstName, CancellationToken cancellationToken = default)
        {
            string otpCode;
            var resetToken = Guid.NewGuid().ToString();

            bool bypassEnabled = _configuration.GetValue<bool>("Testing:BypassOtp");
            string bypassDomain = _configuration.GetValue<string>("Testing:BypassDomain") ?? "@test.com";
            string fixedOtp = _configuration.GetValue<string>("Testing:FixedOtpCode") ?? "123456";

            bool isBypassEmail = bypassEnabled && email.EndsWith(bypassDomain, StringComparison.OrdinalIgnoreCase);

            if (isBypassEmail)
            {
                otpCode = fixedOtp;
            }
            else
            {
                otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            }

            var cacheDto = new OtpCacheDto
            {
                Email = email,
                OtpCode = otpCode,
                Purpose = "PasswordReset"
            };

            await _cacheService.SetAsync(resetToken, cacheDto, TimeSpan.FromMinutes(10), cancellationToken);

            if (!isBypassEmail)
            {
                var emailPlaceholders = new Dictionary<string, string>
        {
             { "FirstName", firstName ?? "User" },
             { "OtpCode", otpCode }
        };

                var emailBody = await _templateService.GetTemplateAsync("OtpEmail", emailPlaceholders);
                await _emailService.SendEmailAsync(email, "Your Secure OTP Code", emailBody, cancellationToken);
            }

            return resetToken;
        }
    }
}