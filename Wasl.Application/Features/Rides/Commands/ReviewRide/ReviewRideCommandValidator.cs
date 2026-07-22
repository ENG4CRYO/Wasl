using FluentValidation;
using Microsoft.Extensions.Localization;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Commands.ReviewRide
{
    public class ReviewRideCommandValidator : AbstractValidator<ReviewRideCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ReviewRideCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.RideId)
                .NotEmpty().WithMessage(_localizer["Validation.Rides.RideIdNotEmpty"]);

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage(_localizer["Validation.Rides.RatingRange"]);

            RuleFor(x => x.Comment)
                .MaximumLength(500).WithMessage(_localizer["Validation.Rides.CommentMaxLength"])
                .When(x => !string.IsNullOrEmpty(x.Comment));
        }
    }
}
