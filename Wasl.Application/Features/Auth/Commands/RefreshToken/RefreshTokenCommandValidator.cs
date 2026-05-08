using Wasl.Application.Resources;
using Wasl.Application.Validators.Auth;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Wasl.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public RefreshTokenCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Token).NotNull().WithMessage(_localizer["Validation.Auth.RefreshTokenNull"])
            .NotEmpty().WithMessage(_localizer["Validation.Auth.RefreshTokenEmpty"]);
        }
    }
}