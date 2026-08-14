using FluentValidation;
using Microsoft.Extensions.Localization;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Auth.Commands.ResetPassword
{
    public class VerifyResetOtpCommandValidator : AbstractValidator<VerifyResetOtpCommand>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public VerifyResetOtpCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ResetToken).NotEmpty().WithMessage(_localizer["Validation.Auth.ResetTokenRequired"]);
            RuleFor(x => x.OtpCode).NotEmpty().Length(6).WithMessage(_localizer["Validation.Auth.OTPRequired"]);
        }
    }
}