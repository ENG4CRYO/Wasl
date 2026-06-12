using AutoMapper;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Helpers;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Resources;
using Wasl.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Wasl.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthModel>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenHelper _tokenHelper;
        private readonly JWT _jwtOptions;
        private readonly IStringLocalizer<SharedResource> _localizer;
        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            ITokenHelper tokenHelper,
            IOptions<JWT> jwtOptions,
            IStringLocalizer<SharedResource> localizer)
        { 
            _userManager = userManager;
            _tokenHelper = tokenHelper;
            _jwtOptions = jwtOptions.Value;
            _localizer = localizer;
        }

        public async Task<ApiResponse<AuthModel>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.InvalidCredentials"]);
            }

            _tokenHelper.ManageUserSessions(user);


            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var jwtSecurityToken = _tokenHelper.CreateJwtToken(user, roles, claims);


            var refreshToken = _tokenHelper.GenerateRefreshToken();

            user.RefreshTokens.Add(refreshToken);

            await _userManager.UpdateAsync(user);

            var authModel = new AuthModel
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                ExpiresOn = jwtSecurityToken.ValidTo,
                RefreshToken = refreshToken.Token,
                IsAuthenticated = true,
                Email = user.Email,
                UserName = user.UserName,
                Roles = roles.ToList(),
                RefreshTokenExpiration = refreshToken.Expires
            };

            return ApiResponse<AuthModel>.Success(authModel, "Login Successful");
        }
    }
}