using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Interfaces.Common;
using Wasl.Core.Entities;
using Wasl.Core.Enums;

namespace Wasl.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IApplicationDbContext _dbContext;

        public WalletService(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<WalletOperationResult> AddFundsAsync(string userId, decimal amount, TransactionType type, Guid? referenceId, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null)
                return new WalletOperationResult(false, 0, "User not found");

            user.Balance += amount;

            _dbContext.WalletTransactions.Add(new WalletTransaction
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Amount = amount,
                Type = type,
                RideId = referenceId
            });

            return new WalletOperationResult(true, user.Balance);
        }

        public async Task<WalletOperationResult> DeductFundsAsync(string userId, decimal amount, TransactionType type, Guid? referenceId, bool allowNegativeBalance, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null)
                return new WalletOperationResult(false, 0, "User not found");

            if (!allowNegativeBalance && user.Balance < amount)
                return new WalletOperationResult(false, user.Balance, "Insufficient balance");

            user.Balance -= amount;

            _dbContext.WalletTransactions.Add(new WalletTransaction
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Amount = -amount,
                Type = type,
                RideId = referenceId
            });

            return new WalletOperationResult(true, user.Balance);
        }

        public async Task<WalletOperationResult> TransferFundsAsync(string senderId, string receiverId, decimal amount, TransactionType type, Guid? referenceId, CancellationToken cancellationToken)
        {
            var deductResult = await DeductFundsAsync(senderId, amount, type, referenceId, allowNegativeBalance: false, cancellationToken);
            if (!deductResult.IsSuccess)
                return deductResult;

            await AddFundsAsync(receiverId, amount, type, referenceId, cancellationToken);

            var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == receiverId, cancellationToken);

            return new WalletOperationResult(true, receiver?.Balance ?? 0);
        }
    }
}
