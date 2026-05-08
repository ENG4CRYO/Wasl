 using Wasl.Application.Resources;
using Wasl.Application.Validators.Auth;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.InitiateRegistration
{
    public class InitiateRegistrationCommandValidator : AbstractValidator<InitiateRegistrationCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;   

        public InitiateRegistrationCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.FirstName)
             .NotEmpty().WithMessage(_localizer["Validation.Auth.FirstNameRequired"])
             .MaximumLength(50).WithMessage(_localizer["Validation.Auth.FirstNameMaxLength"]);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.LastNameRequired"])
                .MaximumLength(50).WithMessage(_localizer["Validation.Auth.LastNameMaxLength"]);

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.UserNameRequired"])
                .MinimumLength(3).WithMessage(_localizer["Validation.Auth.UserNameMinLength"])
                .MaximumLength(15).WithMessage(_localizer["Validation.Auth.UserNameMaxLength"]);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.EmailRequired"])
                .EmailAddress().WithMessage(_localizer["Validation.Auth.InvalidEmailFormat"]);

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Continue)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.PasswordRequired"])
                .MinimumLength(6).WithMessage(_localizer["Validation.Auth.MinPasswordLength"])
                .Matches("[A-Z]").WithMessage(_localizer["Validation.Auth.PasswordCapitalLetter"])
                .Matches("[a-z]").WithMessage(_localizer["Validation.Auth.PasswordSmallLetter"])
                .Matches("[0-9]").WithMessage(_localizer["Validation.Auth.PasswordConatainNumber"])
                .Matches(@"[\W_]").WithMessage(_localizer["Must Be Contain A Special Character"]);
        }
    }
}
