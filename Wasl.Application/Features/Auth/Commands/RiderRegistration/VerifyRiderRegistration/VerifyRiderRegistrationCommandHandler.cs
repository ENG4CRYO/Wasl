using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Features.Auth.Commands.RiderRegistration.VerifyRiderRegistration;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Interfaces.Services; 
using Wasl.Application.Resources;
using Wasl.Core.Constants;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Auth.Commands.RiderRegistration
{
    public class VerifyRiderRegistrationCommandHandler : IRequestHandler<VerifyRiderRegistrationCommand, ApiResponse<AuthModel>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOtpService _otpService; 
        private readonly ITokenHelper _tokenHelper;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public VerifyRiderRegistrationCommandHandler(
            UserManager<ApplicationUser> userManager,
            IOtpService otpService, 
            ITokenHelper tokenHelper,
            IMapper mapper,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _otpService = otpService;
            _tokenHelper = tokenHelper;
            _mapper = mapper;
            _localizer = localizer;
        }

        public async Task<ApiResponse<AuthModel>> Handle(VerifyRiderRegistrationCommand request, CancellationToken cancellationToken)
        {
            var verificationResult = await _otpService.VerifyOtpAsync(request.RegisterToken, request.OtpCode, cancellationToken);

            if (!verificationResult.IsValid)
            {
                return ApiResponse<AuthModel>.Failure(_localizer[verificationResult.ErrorMessage!]);
            }

            string userEmail = verificationResult.Data!.Email;

            var existingUser = await _userManager.FindByEmailAsync(userEmail);
            if (existingUser != null)
            {
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.EmailAlreadyRegistered"]);
            }

            var newUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = userEmail,
                UserName = userEmail,
                EmailConfirmed = true,
                Balance = 0
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
            {
                var errorsDictionary = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.Description).ToList()
                    );

                return ApiResponse<AuthModel>.Failure(
                    _localizer["Auth.CreateUserFeiled"],
                    errorsDictionary
                );
            }

            await _userManager.AddToRoleAsync(newUser, AspRoles.Rider);

            var newRefreshToken = _tokenHelper.GenerateRefreshToken();
            newUser.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(newUser);

            var roles = await _userManager.GetRolesAsync(newUser);
            var claims = await _userManager.GetClaimsAsync(newUser);
            var jwtToken = _tokenHelper.CreateJwtToken(newUser, roles, claims);

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