using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Dtos.AuthModel;

namespace Wasl.Application.Interfaces.Services
{
    public interface IOtpService
    {
        Task<string?> InitiateRegistrationAsync(string email, CancellationToken cancellationToken = default);
        Task<(bool IsValid, string? ErrorMessage, OtpCacheDto? Data)> VerifyOtpAsync(string token, string providedOtp, CancellationToken cancellationToken = default);
    }
}
