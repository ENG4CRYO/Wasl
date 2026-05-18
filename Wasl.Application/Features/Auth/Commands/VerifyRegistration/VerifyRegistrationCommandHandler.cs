using AutoMapper;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Constants;
using Wasl.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wasl.Application.Features.Auth.Commands.VerifyRegistration
{
    public class VerifyRegistrationCommandHandler : IRequestHandler<VerifyRegistrationCommand, ApiResponse<AuthModel>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly ITokenHelper _tokenHelper;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public VerifyRegistrationCommandHandler(
            UserManager<ApplicationUser> userManager,
            ICacheService cacheService,
            ITokenHelper tokenHelper,
            IMapper mapper,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _cacheService = cacheService;
            _tokenHelper = tokenHelper;
            _mapper = mapper;
            _localizer = localizer;
        }

        public async Task<ApiResponse<AuthModel>> Handle(VerifyRegistrationCommand request, CancellationToken cancellationToken)
        {
            var pendingUser = await _cacheService.GetAsync<PendingRegistrationDto>(request.RegisterToken, cancellationToken);

          
            if (pendingUser == null)
            {
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.SessionExpiredOrInvalidToken"]);
            }

            if (pendingUser.OtpCode != request.OtpCode)
            {
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.InvalidOTP"]);
            }

            var newUser = new ApplicationUser
            {
                FirstName = pendingUser.FirstName,
                LastName = pendingUser.LastName,
                Email = pendingUser.Email,
                UserName = pendingUser.Username,
                EmailConfirmed = true, 
                PasswordHash = pendingUser.PasswordHash 
            };

           
            var result = await _userManager.CreateAsync(newUser);
            if (!result.Succeeded)
            {
                var errorMessages = string.Join(" | ", result.Errors.Select(e => e.Description));
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.CreateUserFeiled"]);
            }
            await _userManager.AddToRoleAsync(newUser, AspRoles.Rider);


            var newRefreshToken = _tokenHelper.GenerateRefreshToken();
            newUser.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(newUser);


            var roles = await _userManager.GetRolesAsync(newUser);
            var claims = await _userManager.GetClaimsAsync(newUser);
            var jwtToken = _tokenHelper.CreateJwtToken(newUser, roles, claims);

            
            await _cacheService.RemoveAsync(request.RegisterToken, cancellationToken);


            var authModel = _mapper.Map<AuthModel>(newUser);
            authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            authModel.RefreshToken = newRefreshToken.Token;
            authModel.ExpiresOn = jwtToken.ValidTo;
            authModel.RefreshTokenExpiration = newRefreshToken.Expires;
            authModel.IsAuthenticated = true;
            authModel.Roles = roles.ToList();

            return ApiResponse<AuthModel>.Success(authModel, _localizer["Auth.UserRegisteredSuccessfully"]);
        }
    }
}