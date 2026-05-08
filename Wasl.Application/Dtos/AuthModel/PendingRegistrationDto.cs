namespace Wasl.Application.Dtos.AuthModel
{
    public class PendingRegistrationDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}