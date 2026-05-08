namespace Wasl.Application.Interfaces.Infrastructure
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken);
    }
}
