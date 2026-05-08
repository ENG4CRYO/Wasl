using Wasl.Infrastructure.Models;
using Wasl.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Wasl.Infrastructure.BackgroundJobs
{
    public class EmailBackgroundSender : BackgroundService
    {
        private readonly Channel<EmailMessage> _emailChannel;
        private readonly ILogger<EmailBackgroundSender> _logger;
        private readonly MailSettings _mailSettings;

        public EmailBackgroundSender(Channel<EmailMessage> emailChannel,
            ILogger<EmailBackgroundSender> logger,
            IOptions<MailSettings> mailSettings)
        {
            _emailChannel = emailChannel;
            _logger = logger;
            _mailSettings = mailSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Background Service is starting.");

            await foreach (var message in _emailChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation($"Sending email to {message.To}...");

                    var email = new MimeMessage();
                    email.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.EmailFrom));
                    email.To.Add(MailboxAddress.Parse(message.To));
                    email.Subject = message.Subject;

                    var builder = new BodyBuilder { HtmlBody = message.Body };
                    email.Body = builder.ToMessageBody();

                    using var smtp = new SmtpClient();

                    await smtp.ConnectAsync(_mailSettings.SmtpHost, _mailSettings.SmtpPort, SecureSocketOptions.StartTls, stoppingToken);

                    await smtp.AuthenticateAsync(_mailSettings.SmtpUser, _mailSettings.SmtpPass, stoppingToken);
                    await smtp.SendAsync(email, stoppingToken);
                    await smtp.DisconnectAsync(true, stoppingToken);

                    _logger.LogInformation($"Successfully sent email to {message.To}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email to {message.To}");
                }
            }
        }
    }
}