using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.DriverRegistration.VerifyDriverOtp
{
    public class VerifyDriverOtpValidator : AbstractValidator<VerifyDriverOtpCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public VerifyDriverOtpValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.SessionToken)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.RegisterTokenRequired"]);

            RuleFor(x => x.OtpCode).NotEmpty().Length(6).WithMessage(_localizer["Validation.Auth.OTPRequired"]);
        }
    }
}
