using Wasl.Core.Entities.AuthEntites;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;    
        public decimal Balance { get; set; }
        public string ProfilePictureUrls { get; set; } = string.Empty;

        public string? City { get; set; }
        public bool? IsOnline { get; set; }
        public DriverProfile? DriverProfile { get; set; } 

        public ICollection<Ride> RequestedRides { get; set; } = new List<Ride>();

        public ICollection<Ride> DrivenRides { get; set; } = new List<Ride>();

        public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();


        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }
        public void ManageUserTokens(int refreshTokenValidityInDays)
        {
            RefreshTokens.RemoveAll(t => t.Expires <= DateTime.UtcNow);

            const int MaxActiveSessions = 5;

            var activeTokens = RefreshTokens
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
