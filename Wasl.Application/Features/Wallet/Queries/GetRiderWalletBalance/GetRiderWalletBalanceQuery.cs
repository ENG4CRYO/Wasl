using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Wallet;

namespace Wasl.Application.Features.Wallet.Queries.GetRiderWalletBalance
{
    public class GetRiderWalletBalanceQuery : IRequest<ApiResponse<WalletBalanceDto>>
    {
    }
}
