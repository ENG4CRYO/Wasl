using FluentValidation;
using Microsoft.Extensions.Localization;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Queries.GetRideHistory
{
    public class GetRideHistoryQueryValidator : AbstractValidator<GetRideHistoryQuery>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetRideHistoryQueryValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage(_localizer["Validation.Common.PageNumberGreaterThanZero"]);

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage(_localizer["Validation.Common.PageSizeGreaterThanZero"])
                .LessThanOrEqualTo(100).WithMessage(_localizer["Validation.Common.PageSizeMax100"]);
        }
    }
}
