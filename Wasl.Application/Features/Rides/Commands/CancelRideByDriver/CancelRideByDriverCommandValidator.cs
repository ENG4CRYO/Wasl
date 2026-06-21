using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Commands.CancelRideByDriver
{
    public class CancelRideByDriverCommandValidator : AbstractValidator<CancelRideByDriverCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public CancelRideByDriverCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.RideId)
                .NotNull()
                .WithMessage(_localizer["Validation.Rides.RideIdNotNull"])
                .NotEmpty()
                .WithMessage(_localizer["Validation.Rides.RideIdNotEmpty"]);
        }
    }
}
