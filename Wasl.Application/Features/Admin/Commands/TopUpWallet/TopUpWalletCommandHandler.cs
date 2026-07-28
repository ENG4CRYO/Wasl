using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Admin.Commands.TopUpWallet
{
    public class TopUpWalletCommandHandler : IRequestHandler<TopUpWalletCommand, ApiResponse<decimal>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IWalletService _walletService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public TopUpWalletCommandHandler(
            IApplicationDbContext context,
            IWalletService walletService,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _walletService = walletService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<decimal>> Handle(TopUpWalletCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user is null)
                return ApiResponse<decimal>.Failure(_localizer["Auth.UserNotFound"]);

            var result = await _walletService.AddFundsAsync(
                request.UserId,
                request.Amount,
                TransactionType.WalletTopUp,
                referenceId: null,
                cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<decimal>.Failure(result.ErrorMessage ?? _localizer["Admin.TopUpFailed"]);

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<decimal>.Success(result.NewBalance, string.Format(_localizer["Admin.TopUpSuccess"], result.NewBalance));
        }
    }
}
