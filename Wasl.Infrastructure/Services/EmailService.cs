using Wasl.Application.Interfaces.Infrastructure;
using Wasl.Infrastructure.Models;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Wasl.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly Channel<EmailMessage> _emailChannel;

        public EmailService(Channel<EmailMessage> emailChannel)
        {
            _emailChannel = emailChannel;
        }

        public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage { To = to, Subject = subject, Body = body };

            await _emailChannel.Writer.WriteAsync(message, cancellationToken);
        }
    }
}