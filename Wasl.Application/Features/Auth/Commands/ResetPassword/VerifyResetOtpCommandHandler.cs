using MediatR;
using Microsoft.Extensions.Localization;
using System;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.ResetPassword
{
    public class VerifyResetOtpCommandHandler : IRequestHandler<VerifyResetOtpCommand, ApiResponse<string>>
    {
        private readonly IOtpService _otpService;
        private readonly ICacheService _cacheService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public VerifyResetOtpCommandHandler(
            IOtpService otpService,
            ICacheService cacheService,
            IStringLocalizer<SharedResource> localizer)
        {
            _otpService = otpService;
            _cacheService = cacheService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<string>> Handle(VerifyResetOtpCommand request, CancellationToken cancellationToken)
        {
            var verificationResult = await _otpService.VerifyOtpAsync(request.ResetToken, request.OtpCode, cancellationToken);

            if (!verificationResult.IsValid)
            {
                return ApiResponse<string>.Failure(_localizer[verificationResult.ErrorMessage!]);
            }

            var resetToken = Guid.NewGuid().ToString();

            await _cacheService.SetAsync($"ValidatedResetSession:{resetToken}", verificationResult.Data!.Email, TimeSpan.FromMinutes(15), cancellationToken);

            return ApiResponse<string>.Success(resetToken, _localizer["Auth.OtpVerified"]);
        }
    }
}