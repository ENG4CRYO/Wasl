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
        public string? Address { get; set; }
        public bool? IsOnline { get; set; }
        public DriverProfile? DriverProfile { get; set; } 

        public ICollection<Ride> RequestedRides { get; set; } = new List<Ride>();

        public ICollection<Ride> DrivenRides { get; set; } = new List<Ride>();

        public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    }
}
