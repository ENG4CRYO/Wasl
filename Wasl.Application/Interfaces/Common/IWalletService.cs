using System.Threading;
using System.Threading.Tasks;
using Wasl.Core.Enums;

namespace Wasl.Application.Interfaces.Common
{
    public record WalletOperationResult(bool IsSuccess, decimal NewBalance, string? ErrorMessage = null);

    public interface IWalletService
    {
        Task<WalletOperationResult> AddFundsAsync(string userId, decimal amount, TransactionType type, Guid? referenceId, CancellationToken cancellationToken);
        Task<WalletOperationResult> DeductFundsAsync(string userId, decimal amount, TransactionType type, Guid? referenceId, bool allowNegativeBalance, CancellationToken cancellationToken);
        Task<WalletOperationResult> TransferFundsAsync(string senderId, string receiverId, decimal amount, TransactionType type, Guid? referenceId, CancellationToken cancellationToken);
    }
}
