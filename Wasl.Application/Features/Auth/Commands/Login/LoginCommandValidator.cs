using Wasl.Application.Resources;
using Wasl.Application.Validators.Auth;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public LoginCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.EmailRequired"])
                .EmailAddress().WithMessage(_localizer["Validation.Auth.InvalidEmailFormat"]);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.PasswordRequired"]);
        }
    }
}
