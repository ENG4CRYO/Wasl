using FluentValidation;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Rides.Commands.ChangePaymentMethod
{
    public class ChangeRidePaymentMethodCommandValidator : AbstractValidator<ChangeRidePaymentMethodCommand>
    {
        public ChangeRidePaymentMethodCommandValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.RideId)
                .NotEmpty();

            RuleFor(x => x.NewPaymentMethod)
                .IsInEnum();
        }
    }
}
