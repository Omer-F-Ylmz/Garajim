using Garajim.Business.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Garajim.Tests.Unit
{
    public class SmtpEmailSenderTests
    {
        private sealed class KayitliLogger : ILogger<SmtpEmailSender>
        {
            public List<string> Mesajlar { get; } = new List<string>();

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Mesajlar.Add(formatter(state, exception));
            }
        }

        private static IConfiguration Yapilandirma(Dictionary<string, string> degerler)
        {
            return new ConfigurationBuilder().AddInMemoryCollection(degerler).Build();
        }

        [Fact]
        public async Task YapilandirmaBosKenGondermeyiAtlarVeLoglar()
        {
            var logger = new KayitliLogger();
            var sender = new SmtpEmailSender(Yapilandirma(new Dictionary<string, string>()), logger);

            await sender.SendAsync("alici@garajim.local", "Konu", "Gövde");

            Assert.Contains(logger.Mesajlar, m => m.Contains("yapılandırılmadı", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task KullaniciEksikseGondermeyiAtlarVeLoglar()
        {
            var logger = new KayitliLogger();
            var sender = new SmtpEmailSender(Yapilandirma(new Dictionary<string, string>
            {
                ["Smtp:Host"] = "smtp.ornek.local",
                ["Smtp:From"] = "no-reply@garajim.app"
            }), logger);

            await sender.SendAsync("alici@garajim.local", "Konu", "Gövde");

            Assert.Contains(logger.Mesajlar, m => m.Contains("yapılandırılmadı", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task AtlananGonderimIstisnaFirlatmaz()
        {
            var sender = new SmtpEmailSender(Yapilandirma(new Dictionary<string, string>()), new KayitliLogger());

            var istisna = await Record.ExceptionAsync(() => sender.SendAsync("alici@garajim.local", "Konu", "Gövde"));

            Assert.Null(istisna);
        }
    }
}
