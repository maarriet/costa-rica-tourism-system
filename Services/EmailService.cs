// Services/EmailService.cs
using MailKit.Net.Smtp;
using MimeKit;

namespace Sistema_GuiaLocal_Turismo.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlContent);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["Email:DisplayName"],
                    _configuration["Email:From"]));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                message.Body = new TextPart("html") { Text = htmlContent };

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _configuration["Email:Host"],
                    int.Parse(_configuration["Email:Port"]),
                    true);
                await client.AuthenticateAsync(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email enviado exitosamente a {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error enviando email a {toEmail}: {ex.Message}");
                throw;
            }
        }
    }
}
