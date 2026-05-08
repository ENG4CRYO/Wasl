using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Helpers
{
    public class JWT
    {
        public string Key { get; set; } = default!;
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public int AccessTokenValidityInMinutes { get; set; }
        public int RefreshTokenValidityInDays {get; set;}
    }
}
