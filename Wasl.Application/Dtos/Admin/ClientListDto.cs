namespace Wasl.Application.Dtos.Admin
{
    public class ClientListDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}
