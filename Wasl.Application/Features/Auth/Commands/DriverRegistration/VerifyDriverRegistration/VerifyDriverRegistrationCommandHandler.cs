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
using Wasl.Application.Interfaces.Common;
using Wasl.Application.Interfaces.Helpers;
using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Application.Resources;
using Wasl.Core.Constants;
using Wasl.Core.Entities;
using Wasl.Core.Enums; 

namespace Wasl.Application.Features.Auth.Commands.DriverRegistration
{
    public class VerifyDriverRegistrationCommandHandler : IRequestHandler<VerifyDriverRegistrationCommand, ApiResponse<AuthModel>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICacheService _cacheService;
        private readonly ITokenHelper _tokenHelper;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IApplicationDbContext _context;

        public VerifyDriverRegistrationCommandHandler(
            UserManager<ApplicationUser> userManager,
            ICacheService cacheService,
            ITokenHelper tokenHelper,
            IMapper mapper,
            IStringLocalizer<SharedResource> localizer,
            IApplicationDbContext context)
        {
            _userManager = userManager;
            _cacheService = cacheService;
            _tokenHelper = tokenHelper;
            _mapper = mapper;
            _localizer = localizer;
            _context = context;
        }

        public async Task<ApiResponse<AuthModel>> Handle(VerifyDriverRegistrationCommand request, CancellationToken cancellationToken)
        {
            var cachedData = await _cacheService.GetAsync<OtpCacheDto>(request.RegisterToken, cancellationToken);

            if (cachedData == null)
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.SessionExpiredOrInvalidToken"]);

            if (cachedData.OtpCode != request.OtpCode)
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.InvalidOTP"]);

            string userEmail = cachedData.Email;

            var existingUser = await _userManager.FindByEmailAsync(userEmail);
            if (existingUser != null)
                return ApiResponse<AuthModel>.Failure(_localizer["Auth.EmailAlreadyRegistered"]);

            var newUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = userEmail,
                UserName = userEmail,
                PhoneNumber = request.PhoneNumber,
                City = request.City, 
                Address = request.Address,
                IsOnline = false,
                EmailConfirmed = true,
                Balance = 0
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
            {
                var errorsDictionary = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToList());

                return ApiResponse<AuthModel>.Failure(_localizer["Auth.CreateUserFeiled"], errorsDictionary);
            }

            await _userManager.AddToRoleAsync(newUser, AspRoles.Driver);

            var driverProfile = new DriverProfile
            {
                UserId = newUser.Id,
                ApprovalStatus = DriverApprovalStatus.Pending
            };

            await _context.DriverProfiles.AddAsync(driverProfile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

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