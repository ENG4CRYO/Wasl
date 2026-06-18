using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.IdentityModel.Tokens.Jwt;
using Wasl.Application.Common;
using Wasl.Application.Dtos.AuthModel;
using Wasl.Application.Features.Auth.Commands.RiderRegistration.VerifyRiderRegistration;
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Constants;
using Wasl.Core.Entities;

namespace Wasl.Application.Features.Auth.Commands.RiderRegistration
{
    public class CompleteRiderRegistrationCommandHandler : IRequestHandler<CompleteRiderRegistrationCommand, ApiResponse<AuthModel>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly ITokenHelper _tokenHelper;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IApplicationDbContext _context;

        public CompleteRiderRegistrationCommandHandler(
            UserManager<ApplicationUser> userManager,
            ICacheService cacheService, 
            ITokenHelper tokenHelper,
            IStringLocalizer<SharedResource> localizer,
            IApplicationDbContext context)
        {
            _userManager = userManager;
            _cacheService = cacheService; 
            _tokenHelper = tokenHelper;
            _localizer = localizer;
            _context = context;
        }

        public async Task<ApiResponse<AuthModel>> Handle(CompleteRiderRegistrationCommand request, CancellationToken cancellationToken)
        {

            var userEmail = await _cacheService.GetAsync<string>($"ValidatedSession:{request.RegisterToken}", cancellationToken);

            if (string.IsNullOrEmpty(userEmail))
            {
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.RegistrationSessionExpiredOrInvalid"]);
            }

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
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true,
                Balance = 0
            };

            using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await _userManager.CreateAsync(newUser, request.Password);
                if (!result.Succeeded)
                {
                    var errorsDictionary = result.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToList());

                    return ApiResponse<AuthModel>.Failure(_localizer["Auth.CreateUserFailed"], errorsDictionary);
                }

                await _userManager.AddToRoleAsync(newUser, AspRoles.Rider);

                var newRefreshToken = _tokenHelper.GenerateRefreshToken();
                newUser.RefreshTokens.Add(newRefreshToken);

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);


                await _cacheService.RemoveAsync($"ValidatedSession:{request.RegisterToken}", cancellationToken);

                var roles = await _userManager.GetRolesAsync(newUser);
                var claims = await _userManager.GetClaimsAsync(newUser);
                var jwtToken = _tokenHelper.CreateJwtToken(newUser, roles, claims);

                var authModel = new AuthModel
                {
                    Email = newUser.Email,
                    UserName = newUser.UserName,

                    Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    RefreshToken = newRefreshToken.Token,
                    ExpiresOn = jwtToken.ValidTo,
                    RefreshTokenExpiration = newRefreshToken.Expires,
                    IsAuthenticated = true,
                    Roles = roles.ToList()
                };

                return ApiResponse<AuthModel>.Success(authModel, _localizer["Auth.UserRegisteredSuccessfully"]);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}