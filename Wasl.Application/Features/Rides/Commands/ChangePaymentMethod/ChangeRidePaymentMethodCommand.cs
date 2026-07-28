using MediatR;
using Wasl.Application.Common;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Rides.Commands.ChangePaymentMethod
{
    public class ChangeRidePaymentMethodCommand : IRequest<ApiResponse<bool>>
    {
        public Guid RideId { get; set; }
        public PaymentMethod NewPaymentMethod { get; set; }
    }
}
