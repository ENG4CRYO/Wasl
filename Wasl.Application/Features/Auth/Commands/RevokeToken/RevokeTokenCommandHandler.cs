using Wasl.Application.Common;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wasl.Application.Features.Auth.Commands.RevokeToken
{
    public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, ApiResponse<bool>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public RevokeTokenCommandHandler(UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<ApiResponse<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var tokenToRevoke = request.Token;

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == tokenToRevoke), cancellationToken);

            if (user == null)
            {
                return ApiResponse<bool>.Failure(_localizer["Auth.InvalidToken"]);
            }

            var refreshToken = user.RefreshTokens.Single(t => t.Token == tokenToRevoke);

            if (!refreshToken.IsActive)
            {
                return ApiResponse<bool>.Failure(_localizer["Auth.InactiveToken"]);
            }

            refreshToken.Revoked= DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return ApiResponse<bool>.Success(true, _localizer["Auth.TokenRevokedSuccessfully"]);
        }
    }
}