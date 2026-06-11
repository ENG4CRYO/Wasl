using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Commands.CompleteRide
{
    public class CompleteRideCommandValidator : AbstractValidator<CompleteRideCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CompleteRideCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.RideId)
                .NotNull().WithMessage(_localizer["Validation.Rides.RideIdNotNull"])
                .NotEmpty().WithMessage(_localizer["Validation.Rides.RideIdNotEmpty"])
                .Must(x => Guid.TryParse(x, out _)).WithMessage(_localizer["Validation.Rides.RideIdMustBeGuid"]);
        }
    }
}