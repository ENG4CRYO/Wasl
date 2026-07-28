using FluentValidation;
using Microsoft.Extensions.Localization;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Admin.Commands.TopUpWallet
{
    public class TopUpWalletCommandValidator : AbstractValidator<TopUpWalletCommand>
    {
        public TopUpWalletCommandValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage(localizer["Validation.Admin.UserIdRequired"]);

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage(localizer["Validation.Admin.AmountGreaterThanZero"]);
        }
    }
}
