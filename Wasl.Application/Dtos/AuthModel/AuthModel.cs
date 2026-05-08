using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Wasl.Application.Dtos.AuthModel
{
    public class AuthModel
    {
        public bool IsAuthenticated { get; set; }
        public string? UserName { get; set; }
        public string Email { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
        public string Token { get; set; } = string.Empty;   
        public DateTime ExpiresOn { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiration { get; set; }

    }
}
