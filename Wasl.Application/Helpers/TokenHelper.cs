using Wasl.Application.Common; // مسار الـ JWT
using Wasl.Application.Interfaces.Helpers;
using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Wasl.Application.Helpers
{
    public class TokenHelper : ITokenHelper
    {
        private readonly JWT _jwt;
        public TokenHelper(IOptions<JWT> jwt)
        {
            _jwt = jwt.Value;
        }

        public JwtSecurityToken CreateJwtToken(ApplicationUser user, IList<string> roles, IList<Claim> userClaims)
        {
            var roleClaims = new List<Claim>();

            foreach (var role in roles)
            {
                roleClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("uid", user.Id),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            }.Union(userClaims).Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenValidityInMinutes),
                signingCredentials: signingCredentials
                );

            return jwtSecurityToken;
        }

        public RefreshToken GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(_jwt.RefreshTokenValidityInDays),
                Created = DateTime.UtcNow
            };
        }

        public void ManageUserSessions(ApplicationUser user)
        {
            user.RefreshTokens.RemoveAll(t => t.Expires <= DateTime.UtcNow);

 
            const int MaxActiveSessions = 5;

            var activeTokens = user.RefreshTokens
                .Where(t => t.Revoked == null && t.Expires > DateTime.UtcNow)
                .OrderBy(t => t.Created)
                .ToList();

            if (activeTokens.Count >= MaxActiveSessions)
            {
                var tokensToRevokeCount = activeTokens.Count - MaxActiveSessions + 1;
                var tokensToRevoke = activeTokens.Take(tokensToRevokeCount);

                foreach (var token in tokensToRevoke)
                {
                    token.Revoked = DateTime.UtcNow;
                    token.ReasonRevoked = "Exceeded max active sessions";
                }
            }
        }
    }
}