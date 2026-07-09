using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Commands.RequestRide
{
    public class CreateRideRequestCommandValidator : AbstractValidator<CreateRideRequestCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public CreateRideRequestCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.pickupLat)
                .NotNull().WithMessage(_localizer["Validation.Rides.LatitudeNotNull"])
                .NotEmpty().WithMessage(_localizer["Validation.Rides.LatitudeNotEmpty"]);

            RuleFor(x => x.pickupLng)
                .NotNull().WithMessage(_localizer["Validation.Rides.LongitudeNotNull"])
                .NotEmpty().WithMessage(_localizer["Validation.Rides.LongitudeNotEmpty"]);

        }
    }
}
