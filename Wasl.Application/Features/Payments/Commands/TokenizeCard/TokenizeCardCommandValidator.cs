using FluentValidation;

namespace Wasl.Application.Features.Payments.Commands.TokenizeCard
{
    public class TokenizeCardCommandValidator : AbstractValidator<TokenizeCardCommand>
    {
        public TokenizeCardCommandValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .Length(13, 19);

            RuleFor(x => x.ExpiryMonth)
                .NotEmpty()
                .Must(m => int.TryParse(m, out var v) && v >= 1 && v <= 12);

            RuleFor(x => x.ExpiryYear)
                .NotEmpty()
                .Must(y => int.TryParse(y, out var v) && v >= 2024);

            RuleFor(x => x.Cvv)
                .NotEmpty()
                .Length(3, 4);
        }
    }
}
