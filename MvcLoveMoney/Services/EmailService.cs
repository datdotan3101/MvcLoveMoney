using System.Net;
using System.Net.Mail;

namespace MvcLoveMoney.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }

    // Values from the "Smtp" section of appsettings.json / user secrets
    public class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "MvcLoveMoney";
    }

    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpOptions _options;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<SmtpEmailService> logger)
        {
            _options = configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions();
            _environment = environment;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            bool isConfigured = !string.IsNullOrWhiteSpace(_options.Host)
                             && !string.IsNullOrWhiteSpace(_options.Username)
                             && !string.IsNullOrWhiteSpace(_options.Password);

            // No SMTP account yet: while developing, print the email to the console instead
            if (!isConfigured)
            {
                if (!_environment.IsDevelopment())
                    throw new InvalidOperationException("SMTP is not configured (Smtp:Host / Smtp:Username / Smtp:Password).");

                _logger.LogWarning("SMTP is not configured. Email to {To} | {Subject}\n{Body}", toEmail, subject, htmlBody);
                return;
            }

            var fromEmail = string.IsNullOrWhiteSpace(_options.FromEmail) ? _options.Username : _options.FromEmail;

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = new NetworkCredential(_options.Username, _options.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}
