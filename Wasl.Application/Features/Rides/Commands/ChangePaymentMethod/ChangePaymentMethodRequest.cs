using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.ChangePaymentMethod
{
    public class ChangePaymentMethodRequest
    {
        public PaymentMethod NewPaymentMethod { get; set; }
    }
}
