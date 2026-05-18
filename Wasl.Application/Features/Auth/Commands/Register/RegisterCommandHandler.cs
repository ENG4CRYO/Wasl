using AutoMapper;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Resources;
using Wasl.Core.Constants;
using Wasl.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wasl.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthModel>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ITokenHelper _tokenHelper;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public RegisterCommandHandler(
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            ITokenHelper tokenHelper,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _mapper = mapper;
            _tokenHelper = tokenHelper;
            _localizer = localizer;
        }

        public async Task<ApiResponse<AuthModel>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var authModel = new AuthModel { IsAuthenticated = false };

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                var failedResponse = ApiResponse<AuthModel>.Failure(_localizer["Auth.EmailAlreadyRegistered"]);
                failedResponse.Data = authModel;
                return failedResponse;
            }

            var newUser = _mapper.Map<ApplicationUser>(request);

            var result = await _userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
            {
                var errorMessages = string.Join(" | ", result.Errors.Select(e => e.Description));
                var failedResponse = ApiResponse<AuthModel>.Failure(_localizer["Auth.CreateUserFeiled"]);
                failedResponse.Data = authModel;
                return failedResponse;
            }

 
            await _userManager.AddToRoleAsync(newUser, AspRoles.Rider);

            var newRefreshToken = _tokenHelper.GenerateRefreshToken();
            newUser.RefreshTokens.Add(newRefreshToken);

            await _userManager.UpdateAsync(newUser);

            var roles = await _userManager.GetRolesAsync(newUser);
            var claims = await _userManager.GetClaimsAsync(newUser);
            var token = _tokenHelper.CreateJwtToken(newUser, roles, claims);


            authModel = _mapper.Map<AuthModel>(newUser);
            authModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
            authModel.RefreshToken = newRefreshToken.Token;
            authModel.ExpiresOn = token.ValidTo;
            authModel.RefreshTokenExpiration = newRefreshToken.Expires;
            authModel.IsAuthenticated = true;
            authModel.Roles = roles.ToList(); 

            return ApiResponse<AuthModel>.Success(authModel, _localizer["Auth.UserRegisteredSuccessfully"]);
        }
    }
}