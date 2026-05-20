using FluentValidation;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.DriverRegistration.InitiateDriverRegistration
{
    public class InitiateDriverRegistrationCommandValidator : AbstractValidator<InitiateDriverRegistrationCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public InitiateDriverRegistrationCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.EmailRequired"])
                .EmailAddress().WithMessage(_localizer["Validation.Auth.InvalidEmailFormat"]);
        }
    }
}
