using MediatR;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Features.Auth.Commands.DriverRegistration.VerifyDriverOtp;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.DriverRegistration.VerifyOtp
{
    public class VerifyDriverOtpCommandHandler : IRequestHandler<VerifyDriverOtpCommand, ApiResponse<string>>
    {
        private readonly IOtpService _otpService;
        private readonly ICacheService _cacheService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public VerifyDriverOtpCommandHandler(
            IOtpService otpService,
            ICacheService cacheService,
            IStringLocalizer<SharedResource> localizer)
        {
            _otpService = otpService;
            _cacheService = cacheService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<string>> Handle(VerifyDriverOtpCommand request, CancellationToken cancellationToken)
        {
            var verificationResult = await _otpService.VerifyOtpAsync(request.SessionToken, request.OtpCode, cancellationToken);

            if (!verificationResult.IsValid)
            {
                return ApiResponse<string>.Failure(_localizer[verificationResult.ErrorMessage!]);
            }

            string registerToken = Guid.NewGuid().ToString();

            await _cacheService.SetAsync($"ValidatedDriverSession:{registerToken}", verificationResult.Data!.Email, TimeSpan.FromMinutes(15), cancellationToken);

            return ApiResponse<string>.Success(registerToken, _localizer["Auth.OtpVerified"]);
        }
    }
}