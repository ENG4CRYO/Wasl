using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.DriverRegistration.VerifyDriverRegistration
{
    public class VerifyDriverRegistrationCommandValidator : AbstractValidator<VerifyDriverRegistrationCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public VerifyDriverRegistrationCommandValidator(IStringLocalizer<SharedResource> localizer)
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
                .Matches(@"^07[0-9]{9}$").WithMessage(_localizer["Validation.Auth.InvalidPhoneNumber"]);

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Continue)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.PasswordRequired"])
                .MinimumLength(6).WithMessage(_localizer["Validation.Auth.MinPasswordLength"])
                .Matches("[A-Z]").WithMessage(_localizer["Validation.Auth.PasswordCapitalLetter"])
                .Matches("[a-z]").WithMessage(_localizer["Validation.Auth.PasswordSmallLetter"])
                .Matches("[0-9]").WithMessage(_localizer["Validation.Auth.PasswordConatainNumber"])
                .Matches(@"[\W_]").WithMessage(_localizer["Validation.Auth.PasswordSpecialCharacter"]);

            RuleFor(x => x.City)
                .NotNull().WithMessage(_localizer["Validation.Auth.CityNotNull"])
                .NotEmpty().WithMessage(_localizer["Validation.Auth.CityNotEmpty"]);

            RuleFor(x => x.Address)
                 .NotNull().WithMessage(_localizer["Validation.Auth.AddressNotNull"])
                 .NotEmpty().WithMessage(_localizer["Validation.Auth.AddressNotEmpty"]);



        }
    }
}
