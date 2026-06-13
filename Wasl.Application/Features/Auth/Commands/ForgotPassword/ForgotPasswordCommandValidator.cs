using Wasl.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Wasl.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public ForgotPasswordCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.EmailRequired"])
                .EmailAddress().WithMessage(_localizer["Validation.Auth.InvalidEmailFormat"]);
        }
    }
}
