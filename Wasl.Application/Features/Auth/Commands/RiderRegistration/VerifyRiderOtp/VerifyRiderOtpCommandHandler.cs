using MediatR;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.RiderRegistration.VerifyRiderOtp
{
    public class VerifyRiderOtpCommandHandler : IRequestHandler<VerifyRiderOtpCommand, ApiResponse<string>>
    {
        private readonly IOtpService _otpService;
        private readonly ICacheService _caschService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public VerifyRiderOtpCommandHandler(
            IOtpService otpService,
            ICacheService cacheService,
            IStringLocalizer<SharedResource> localizer)
        {
            _otpService = otpService;
            _caschService = cacheService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<string>> Handle(VerifyRiderOtpCommand request, CancellationToken cancellationToken)
        {

            var verificationResult = await _otpService.VerifyOtpAsync(request.SessionToken, request.OtpCode, cancellationToken);

            if (!verificationResult.IsValid)
            {
                return ApiResponse<string>.Failure(_localizer[verificationResult.ErrorMessage!]);
            }
            string registerToken = Guid.NewGuid().ToString();

            await _caschService.SetAsync($"ValidatedSession:{registerToken}", verificationResult.Data!.Email, TimeSpan.FromMinutes(15), cancellationToken);

            return ApiResponse<string>.Success(registerToken, _localizer["Auth.OtpVerified"]);
        }
    }
}
