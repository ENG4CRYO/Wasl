using MediatR;
using Wasl.Application.Common;

namespace Wasl.Application.Features.Admin.Commands.TopUpWallet
{
    public class TopUpWalletCommand : IRequest<ApiResponse<decimal>>
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
