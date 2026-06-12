using MediatR;
using Microsoft.Extensions.Localization;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Services;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.RiderRegistration.InitiateRiderRegistration
{
    public class InitiateRiderRegistrationCommandHandler : IRequestHandler<InitiateRiderRegistrationCommand, ApiResponse<string>>
    {
        private readonly IOtpService _otpService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public InitiateRiderRegistrationCommandHandler(
            IOtpService otpService,
            IStringLocalizer<SharedResource> localizer)
        {
            _otpService = otpService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<string>> Handle(InitiateRiderRegistrationCommand request, CancellationToken cancellationToken)
        {
            var registerToken = await _otpService.InitiateRegistrationAsync(request.Email, cancellationToken);

            if (registerToken == null)
            {
                return ApiResponse<string>.Failure(_localizer["Auth.EmailAlreadyRegistered"]);
            }

            return ApiResponse<string>.Success(registerToken, _localizer["Auth.RegisterSendOtp"]);
        }
    }
}