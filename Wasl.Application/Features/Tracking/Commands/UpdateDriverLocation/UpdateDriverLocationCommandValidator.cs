using FluentValidation;
using Microsoft.Extensions.Localization;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Tracking.Commands.UpdateDriverLocation
{
    public class UpdateDriverLocationCommandValidator : AbstractValidator<UpdateDriverLocationCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UpdateDriverLocationCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Latitude)
                .NotNull().WithMessage(_localizer["Validation.Rides.LatitudeNotNull"])
                .InclusiveBetween(-90, 90).WithMessage(_localizer["Validation.Tracking.InvalidLatitude"]);

            RuleFor(x => x.Longitude)
                .NotNull().WithMessage(_localizer["Validation.Rides.LongitudeNotNull"])
                .InclusiveBetween(-180, 180).WithMessage(_localizer["Validation.Tracking.InvalidLongitude"]);
        }
    }
}
