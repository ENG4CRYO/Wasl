using FluentValidation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.DriverProfiles.Commands.SubmitProfile
{
    public class SubmitDriverProfileCommandValidator : AbstractValidator<SubmitDriverProfileCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public SubmitDriverProfileCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;

            RuleFor(x => x.VehicleModel)
                .NotEmpty()
                .WithMessage(_localizer["Validation.DriverProfiles.VehicleModelRequired"]);

            RuleFor(x => x.VehicleYear)
                .NotEmpty()
                .WithMessage(_localizer["Validation.DriverProfiles.VehicleYearRequired"]);

            RuleFor(x => x.VinNumber)
                .NotEmpty()
                .WithMessage(_localizer["Validation.DriverProfiles.VINRequired"]);

            RuleFor(x => x.VehicleImage)
                .NotNull()
                .WithMessage(_localizer["Validation.DriverProfiles.VehicleImageRequired"]);

            RuleFor(x => x.LicenseFrontImage)
                .NotNull()
                .WithMessage(_localizer["Validation.DriverProfiles.LicenseFrontImageRequired"]);

            RuleFor(x => x.LicenseBackImage)
                .NotNull()
                .WithMessage(_localizer["Validation.DriverProfiles.LicenseBackImageRequired"]);

            RuleFor(x => x.SelfieImage)
                .NotNull()
                .WithMessage(_localizer["Validation.DriverProfiles.SelfieImageRequired"]);

        }
    }
}
