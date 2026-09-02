using Garajim.Business.Abstract;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Garajim.Business.Concrete
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            var user = _configuration["Smtp:User"];
            var pass = _configuration["Smtp:Pass"] ?? _configuration["Smtp:Password"];
            var from = _configuration["Smtp:From"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(from))
            {
                _logger.LogWarning(
                    "E-posta yapılandırılmadı, gönderim atlandı. Alıcı: {Alici} · Konu: {Konu}{Satir}{Govde}",
                    to, subject, Environment.NewLine, body);
                return;
            }

            var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Garajım", from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
