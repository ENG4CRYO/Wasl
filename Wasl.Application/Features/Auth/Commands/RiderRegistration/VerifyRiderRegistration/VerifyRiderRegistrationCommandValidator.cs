using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.RiderRegistration.VerifyRiderRegistration
{
    public class VerifyRiderRegistrationCommandValidator : AbstractValidator<VerifyRiderRegistrationCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public VerifyRiderRegistrationCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.RegisterToken)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.RegisterTokenRequired"]);

            RuleFor(x => x.OtpCode).NotEmpty().Length(6).WithMessage(_localizer["Validation.Auth.OTPRequired"]);

            RuleFor(x => x.FirstName)
             .NotEmpty().WithMessage(_localizer["Validation.Auth.FirstNameRequired"])
             .MaximumLength(50).WithMessage(_localizer["Validation.Auth.FirstNameMaxLength"]);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.LastNameRequired"])
                .MaximumLength(50).WithMessage(_localizer["Validation.Auth.LastNameMaxLength"]);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.PhoneNumberRequired"])
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage(_localizer["Validation.Auth.InvalidPhoneNumber"]);

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Continue)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.PasswordRequired"])
                .MinimumLength(6).WithMessage(_localizer["Validation.Auth.MinPasswordLength"])
                .Matches("[A-Z]").WithMessage(_localizer["Validation.Auth.PasswordCapitalLetter"])
                .Matches("[a-z]").WithMessage(_localizer["Validation.Auth.PasswordSmallLetter"])
                .Matches("[0-9]").WithMessage(_localizer["Validation.Auth.PasswordConatainNumber"])
                .Matches(@"[\W_]").WithMessage(_localizer["Validation.Auth.PasswordSpecialCharacter"]);

        }
    }
}
