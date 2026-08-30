using Garajim.Business.Abstract;
using Garajim.Business.Jobs;
using Microsoft.Extensions.Configuration;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class ReminderNotificationJobTests : IDisposable
    {
        private sealed class SahteEmailService : IEmailSender
        {
            public List<(string To, string Subject, string Body)> Gonderilenler { get; } = new List<(string, string, string)>();

            public Task SendAsync(string to, string subject, string body)
            {
                Gonderilenler.Add((to, subject, body));
                return Task.CompletedTask;
            }
        }

        private sealed class PatlayanEmailService : IEmailSender
        {
            public int Cagrildi { get; private set; }

            public Task SendAsync(string to, string subject, string body)
            {
                Cagrildi++;
                throw new InvalidOperationException("SMTP kapalı.");
            }
        }

        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();
        private readonly SahteEmailService _email = new SahteEmailService();

        private ReminderNotificationJob JobOlustur(IEmailSender emailSender, string baseUrl = null)
        {
            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["App:BaseUrl"] = baseUrl })
                .Build();
            return new ReminderNotificationJob(_db.CompanyDal, _db.ReminderDal, _db.Tenant, emailSender, yapilandirma);
        }

        private Reminder HatirlatmaEkle(int gunSonra, bool isCompleted = false, DateTime? lastNotifiedAt = null)
        {
            var userId = _db.KullaniciEkle("surucu@garajim.local").Id;
            var arac = _db.AracEkle(userId, "34AAA111");

            var reminder = new Reminder
            {
                CompanyId = arac.CompanyId, VehicleId = arac.Id,
                Type = ReminderType.Muayene,
                DueDate = DateTime.UtcNow.Date.AddDays(gunSonra),
                IsCompleted = isCompleted,
                LastNotifiedAt = lastNotifiedAt,
                CreatedAt = new DateTime(2026, 1, 1)
            };

            _db.Context.Reminders.Add(reminder);
            _db.Context.SaveChanges();
            return reminder;
        }

        [Fact]
        public async Task RunAsync_ClaimEdilenHatirlatmayaTamBirEpostaGonderilir()
        {
            HatirlatmaEkle(3);
            var job = JobOlustur(_email);

            await job.RunAsync();

            Assert.Single(_email.Gonderilenler);
            Assert.Equal("surucu@garajim.local", _email.Gonderilenler[0].To);
            Assert.Contains("34AAA111", _email.Gonderilenler[0].Subject);
            Assert.Contains("araç muayenesi", _email.Gonderilenler[0].Subject);
            Assert.Contains("3 gün kaldı", _email.Gonderilenler[0].Body);
        }

        [Fact]
        public async Task RunAsync_UygulamaLinkiYapilandirilmissaGovdeyeEklenir()
        {
            HatirlatmaEkle(3);
            var job = JobOlustur(_email, "https://ornek.garajim.app");

            await job.RunAsync();

            Assert.Single(_email.Gonderilenler);
            Assert.Contains("https://ornek.garajim.app", _email.Gonderilenler[0].Body);
        }

        [Fact]
        public async Task RunAsync_UygulamaLinkiYoksaGovdeLinksizKalir()
        {
            HatirlatmaEkle(3);
            var job = JobOlustur(_email);

            await job.RunAsync();

            Assert.Single(_email.Gonderilenler);
            Assert.DoesNotContain("http", _email.Gonderilenler[0].Body);
        }

        [Fact]
        public async Task RunAsync_IkinciKosudaAyniHatirlatmaIcinEpostaGonderilmez()
        {
            HatirlatmaEkle(3);
            var job = JobOlustur(_email);

            await job.RunAsync();
            var ilkKosuSayisi = _email.Gonderilenler.Count;

            await job.RunAsync();

            Assert.Equal(1, ilkKosuSayisi);
            Assert.Single(_email.Gonderilenler);
        }

        [Fact]
        public async Task RunAsync_TamamlanmisHatirlatmaIcinEpostaGonderilmez()
        {
            HatirlatmaEkle(3, isCompleted: true);
            var job = JobOlustur(_email);

            await job.RunAsync();

            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task RunAsync_UzakTarihliHatirlatmaIcinEpostaGonderilmez()
        {
            HatirlatmaEkle(30);
            var job = JobOlustur(_email);

            await job.RunAsync();

            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task RunAsync_GecmisTarihliHatirlatmaIcinGectiMesajiGonderilir()
        {
            HatirlatmaEkle(-2);
            var job = JobOlustur(_email);

            await job.RunAsync();

            Assert.Single(_email.Gonderilenler);
            Assert.Contains("2 gün önce geçti", _email.Gonderilenler[0].Body);
        }

        [Fact]
        public async Task RunAsync_EpostaHatasiJobuDurdurmaz()
        {
            HatirlatmaEkle(1);
            var patlayan = new PatlayanEmailService();
            var job = JobOlustur(patlayan);

            await job.RunAsync();

            Assert.Equal(1, patlayan.Cagrildi);
        }

        [Fact]
        public async Task RunAsync_HerSirketYalnizKendiHatirlatmasiniIsler()
        {
            var sirketA = _db.SirketEkle("A Filo");
            var sirketB = _db.SirketEkle("B Filo");
            HatirlatmaEkleSirkete(sirketA, "a@garajim.local", "34AAA111");
            HatirlatmaEkleSirkete(sirketB, "b@garajim.local", "06BBB222");

            await JobOlustur(_email).RunAsync();

            Assert.Equal(2, _email.Gonderilenler.Count);

            var aMesaji = _email.Gonderilenler.Single(g => g.To == "a@garajim.local");
            var bMesaji = _email.Gonderilenler.Single(g => g.To == "b@garajim.local");

            Assert.Contains("34AAA111", aMesaji.Subject);
            Assert.DoesNotContain("06BBB222", aMesaji.Subject);
            Assert.Contains("06BBB222", bMesaji.Subject);
            Assert.DoesNotContain("34AAA111", bMesaji.Subject);
        }

        [Fact]
        public async Task RunAsync_BirSirketinHatirlatmasiDigerinePostaGondermez()
        {
            var sirketA = _db.SirketEkle("A Filo");
            var sirketB = _db.SirketEkle("B Filo");
            HatirlatmaEkleSirkete(sirketA, "a@garajim.local", "34AAA111");
            _db.KullaniciEkle("b@garajim.local", sirketB.Id);

            await JobOlustur(_email).RunAsync();

            Assert.Single(_email.Gonderilenler);
            Assert.Equal("a@garajim.local", _email.Gonderilenler[0].To);
        }

        private void HatirlatmaEkleSirkete(Company sirket, string eposta, string plaka)
        {
            var kullanici = _db.KullaniciEkle(eposta, sirket.Id);
            var arac = _db.AracEkleSirketle(kullanici.Id, plaka, sirket.Id);

            _db.Context.Reminders.Add(new Reminder
            {
                CompanyId = sirket.Id,
                VehicleId = arac.Id,
                Type = ReminderType.Muayene,
                DueDate = DateTime.UtcNow.Date.AddDays(3),
                IsCompleted = false,
                CreatedAt = new DateTime(2026, 1, 1)
            });
            _db.Context.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
