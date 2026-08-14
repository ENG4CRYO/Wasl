using Wasl.Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;


namespace Wasl.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;   
        public ResetPasswordCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Token).NotEmpty().WithMessage(_localizer["Validation.Auth.ResetTokenRequired"]);
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(_localizer["Validation.Auth.PasswordRequired"])
                .MinimumLength(6).WithMessage(_localizer["Validation.Auth.MinPasswordLength"])
                .Matches("[A-Z]").WithMessage(_localizer["Validation.Auth.PasswordCapitalLetter"])
                .Matches("[a-z]").WithMessage(_localizer["Validation.Auth.PasswordSmallLetter"])
                .Matches("[0-9]").WithMessage(_localizer["Validation.Auth.PasswordConatainNumber"])
                .Matches(@"[\W_]").WithMessage(_localizer["Must Be Contain A Special Character"]);
        }
    }
}
