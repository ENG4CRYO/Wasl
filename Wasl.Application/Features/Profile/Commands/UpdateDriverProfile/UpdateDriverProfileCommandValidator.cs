using FluentValidation;
using Microsoft.Extensions.Localization;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Profile.Commands.UpdateDriverProfile
{
    public class UpdateDriverProfileCommandValidator : AbstractValidator<UpdateDriverProfileCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UpdateDriverProfileCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.FirstNameRequired"])
                .MaximumLength(50).WithMessage(_localizer["Validation.Auth.FirstNameMaxLength"]);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.LastNameRequired"])
                .MaximumLength(50).WithMessage(_localizer["Validation.Auth.LastNameMaxLength"]);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.PhoneNumberRequired"])
                .Matches(@"^07[0-9]{9}$").WithMessage(_localizer["Validation.Auth.InvalidPhoneNumber"]);
        }
    }
}