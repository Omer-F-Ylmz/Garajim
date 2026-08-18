using System.Net;
using System.Net.Mail;
using Garajim.Business.Abstract;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class SmtpEmailManager : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            var user = _configuration["Smtp:User"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
                return;
            using var client = new SmtpClient(host, int.Parse(_configuration["Smtp:Port"]))
            {
                EnableSsl = bool.Parse(_configuration["Smtp:EnableSsl"]),
                Credentials = new NetworkCredential(user, _configuration["Smtp:Password"])
            };
            using var message = new MailMessage(_configuration["Smtp:From"], to, subject, body);
            await client.SendMailAsync(message);
        }
    }
}
