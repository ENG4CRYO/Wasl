using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Wallet;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Resources;

namespace Wasl.Application.Features.Wallet.Queries.GetRiderWalletBalance
{
    public class GetRiderWalletBalanceQueryHandler : IRequestHandler<GetRiderWalletBalanceQuery, ApiResponse<WalletBalanceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GetRiderWalletBalanceQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _currentUserService = currentUserService;
            _localizer = localizer;
        }

        public async Task<ApiResponse<WalletBalanceDto>> Handle(GetRiderWalletBalanceQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId();
            if (string.IsNullOrEmpty(userId))
                return ApiResponse<WalletBalanceDto>.Failure(_localizer["Auth.Unauthenticated"]);

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
                return ApiResponse<WalletBalanceDto>.Failure(_localizer["Auth.UserNotFound"]);

            var dto = new WalletBalanceDto
            {
                Balance = user.Balance
            };

            return ApiResponse<WalletBalanceDto>.Success(dto, _localizer["Wallet.BalanceRetrievedSuccessfully"]);
        }
    }
}
