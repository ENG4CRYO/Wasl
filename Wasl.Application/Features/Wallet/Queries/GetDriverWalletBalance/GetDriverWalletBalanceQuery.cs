using MediatR;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Wallet;

namespace Wasl.Application.Features.Wallet.Queries.GetDriverWalletBalance
{
    public class GetDriverWalletBalanceQuery : IRequest<ApiResponse<WalletBalanceDto>>
    {
    }
}
