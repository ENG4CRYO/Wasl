using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wasl.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthModel>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenHelper _tokenHelper;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public RefreshTokenCommandHandler(UserManager<ApplicationUser> userManager,
            ITokenHelper tokenHelper,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _tokenHelper = tokenHelper;
            _localizer = localizer;
        }

        public async Task<ApiResponse<AuthModel>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var tokenToRefresh = request.Token;

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == tokenToRefresh), cancellationToken);

            if (user == null)
            {
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.InvalidToken"]);
            }

            var existingToken = user.RefreshTokens.Single(t => t.Token == tokenToRefresh);

            if (!existingToken.IsActive)
            {
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.InactiveToken"]);
            }

            existingToken.Revoked = DateTime.UtcNow.AddMinutes(1);

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var newJwtToken = _tokenHelper.CreateJwtToken(user, roles, claims);
            var newRefreshToken = _tokenHelper.GenerateRefreshToken();

            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);


            var authModel = new AuthModel
            {
                Token = new JwtSecurityTokenHandler().WriteToken(newJwtToken),
                RefreshToken = newRefreshToken.Token,
                IsAuthenticated = true,
                Email = user.Email,
                UserName = user.UserName,
                Roles = roles.ToList(),
                ExpiresOn = newJwtToken.ValidTo,
                RefreshTokenExpiration = newRefreshToken.Expires
            };

            return ApiResponse<AuthModel>.Success(authModel, _localizer["Auth.TokenRefreshedSuccessfully."]);
        }
    }
}