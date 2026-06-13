using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Dtos.AuthModel
{
    public class OtpCacheDto
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public int FailedAttempts { get; set; } = 0;
        public string Purpose { get; set; } = "Registration";
        public DateTime ExpiresAt { get; set; }
    }
}
