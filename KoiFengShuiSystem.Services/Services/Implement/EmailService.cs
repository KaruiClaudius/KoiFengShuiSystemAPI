using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class EmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<EmailService> _logger;


        public EmailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings?.Value ?? throw new ArgumentNullException(nameof(mailSettings));
        }

        public EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger)
        {
            _mailSettings = mailSettings?.Value ?? throw new ArgumentNullException(nameof(mailSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Log the loaded configuration
            _logger.LogInformation($"MailSettings loaded: Server={_mailSettings.Server}, " +
                                   $"Port={_mailSettings.Port}, " +
                                   $"SenderName={_mailSettings.SenderName}, " +
                                   $"SenderEmail={_mailSettings.SenderEmail}, " +
                                   $"UserName={_mailSettings.UserName}, " +
                                   $"UseSSL={_mailSettings.UseSSL}, " +
                                   $"UseStartTls={_mailSettings.UseStartTls}");
        }

        public async Task<bool> SendEmailAsync(MailData mailData)
        {
            if (mailData == null)
            {
                throw new ArgumentNullException(nameof(mailData));
            }

            if (string.IsNullOrEmpty(mailData.EmailToId))
            {
                throw new ArgumentException("Recipient email address cannot be null or empty", nameof(mailData.EmailToId));
            }

            try
            {
                using var emailMessage = new MimeMessage();

                // Sender
                if (string.IsNullOrEmpty(_mailSettings.SenderEmail))
                {
                    throw new InvalidOperationException("Sender email is not configured");
                }
                var emailFrom = new MailboxAddress(_mailSettings.SenderName ?? "Sender", _mailSettings.SenderEmail);
                emailMessage.From.Add(emailFrom);

                // Recipient
                var emailTo = new MailboxAddress(mailData.EmailToName ?? "Recipient", mailData.EmailToId);
                emailMessage.To.Add(emailTo);

                emailMessage.Subject = mailData.EmailSubject ?? "No Subject";

                var bodyBuilder = new BodyBuilder { HtmlBody = mailData.EmailBody ?? "" };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_mailSettings.Server, _mailSettings.Port, _mailSettings.UseSSL);
                await client.AuthenticateAsync(_mailSettings.UserName, _mailSettings.Password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception with more details
                Console.WriteLine($"Error sending email: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }
}