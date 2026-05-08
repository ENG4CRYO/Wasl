using Wasl.Application.Resources;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.RevokeToken
{
    public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>    
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public RevokeTokenCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;


            RuleFor(x => x.Token).NotNull().WithMessage(_localizer["Validation.Auth.TokenNull"])
            .NotEmpty().WithMessage(_localizer["Validation.Auth.TokenEmpty"]);
        }
    }
}
