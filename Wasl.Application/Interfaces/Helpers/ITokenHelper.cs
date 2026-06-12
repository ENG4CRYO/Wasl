using Wasl.Core.Entities;
using Wasl.Core.Entities.AuthEntites;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Wasl.Application.Interfaces.Helpers
{
    public interface ITokenHelper
    {
        JwtSecurityToken CreateJwtToken(ApplicationUser user, IList<string> roles, IList<Claim> userClaims);
        RefreshToken GenerateRefreshToken();
        void ManageUserSessions(ApplicationUser user);
    }
}
