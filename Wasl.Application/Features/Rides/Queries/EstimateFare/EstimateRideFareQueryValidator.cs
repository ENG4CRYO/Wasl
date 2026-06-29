using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Queries.EstimateFare
{
    public class EstimateRideFareQueryValidator : AbstractValidator<EstimateRideFareQuery>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public EstimateRideFareQueryValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.PickupLat)
                .NotNull().WithMessage(_localizer["Validation.Rides.LatitudeNotNull"])
                .NotEmpty().WithMessage(_localizer["Validation.Rides.LatitudeNotEmpty"]);

            RuleFor(x => x.PickupLng)
                .NotNull().WithMessage(_localizer["Validation.Rides.LongitudeNotNull"])
                .NotEmpty().WithMessage(_localizer["Validation.Rides.LongitudeNotEmpty"]);


            RuleFor(x => x.DropoffLat)
                .NotNull().WithMessage(_localizer["Validation.Rides.LatitudeNotNull"])
                .NotEmpty().WithMessage(_localizer["Validation.Rides.LatitudeNotEmpty"]);

            RuleFor(x => x.DropoffLng)
                .NotNull().WithMessage(_localizer["Validation.Rides.LongitudeNotNull"])
                .NotEmpty().WithMessage(_localizer["Validation.Rides.LongitudeNotEmpty"]);



        }
    }
}
