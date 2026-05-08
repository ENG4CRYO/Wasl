using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Resources;
using Wasl.Application.Validators.Auth;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.VerifyRegistration
{
    public class VerifyRegistrationCommandValidator : AbstractValidator<VerifyRegistrationCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;   
        public VerifyRegistrationCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.RegisterToken)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.RegisterTokenRequired"]);

            RuleFor(x => x.OtpCode).NotEmpty().Length(6).WithMessage(_localizer["Validation.Auth.OTPRequired"]);
        }
    }
}
