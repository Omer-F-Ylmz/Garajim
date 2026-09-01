using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Jobs;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class EvrakNotificationTests : IDisposable
    {
        private sealed class SahteEmailSender : IEmailSender
        {
            public List<(string To, string Subject, string Body)> Gonderilenler { get; } = new List<(string, string, string)>();

            public Task SendAsync(string to, string subject, string body)
            {
                Gonderilenler.Add((to, subject, body));
                return Task.CompletedTask;
            }
        }

        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();
        private readonly SahteEmailSender _email = new SahteEmailSender();

        private ReminderNotificationJob JobOlustur(Dictionary<string, string> ayarlar = null)
        {
            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(ayarlar ?? new Dictionary<string, string>())
                .Build();

            return new ReminderNotificationJob(
                _db.CompanyDal,
                _db.ReminderDal,
                _db.Tenant,
                _email,
                yapilandirma,
                _db.EvrakDal,
                _db.UserDal,
                _db.AssignmentDal,
                new EvrakKurallari(yapilandirma));
        }

        private EvrakKaydi AracEvrakiEkle(int gunSonra, Vehicle arac, bool aktif = true, DateTime? lastNotifiedAt = null)
        {
            var kayit = new EvrakKaydi
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                EvrakTuru = EvrakTuru.Muayene,
                BitisTarihi = DateTime.UtcNow.Date.AddDays(gunSonra),
                Aktif = aktif,
                LastNotifiedAt = lastNotifiedAt,
                OlusturmaTarihi = DateTime.UtcNow
            };

            _db.Context.EvrakKayitlari.Add(kayit);
            _db.Context.SaveChanges();
            return kayit;
        }

        [Fact]
        public async Task YaklasanAracEvrakiSahibeEpostaGonderir()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var arac = _db.AracEkle(sahip.Id, "34EVJ001");
            AracEvrakiEkle(10, arac);

            await JobOlustur().RunAsync();

            var gonderi = Assert.Single(_email.Gonderilenler);
            Assert.Equal("sahip@garajim.local", gonderi.To);
            Assert.Contains("34EVJ001", gonderi.Subject);
            Assert.Contains("araç muayenesi", gonderi.Subject);
        }

        [Fact]
        public async Task IkinciKosudaAyniEvrakIcinTekrarGonderilmez()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var arac = _db.AracEkle(sahip.Id, "34EVJ002");
            AracEvrakiEkle(10, arac);

            await JobOlustur().RunAsync();
            var ilkSayi = _email.Gonderilenler.Count;

            await JobOlustur().RunAsync();

            Assert.Equal(1, ilkSayi);
            Assert.Single(_email.Gonderilenler);
        }

        [Fact]
        public async Task PasifEvrakIcinGonderilmez()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var arac = _db.AracEkle(sahip.Id, "34EVJ003");
            AracEvrakiEkle(10, arac, aktif: false);

            await JobOlustur().RunAsync();

            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task UyariPenceresiDisindakiEvrakIcinGonderilmez()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var arac = _db.AracEkle(sahip.Id, "34EVJ004");
            AracEvrakiEkle(90, arac);

            await JobOlustur().RunAsync();

            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task SuresiGecenEvrakIcinGectiMesajiGonderilir()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var arac = _db.AracEkle(sahip.Id, "34EVJ005");
            AracEvrakiEkle(-3, arac);

            await JobOlustur().RunAsync();

            var gonderi = Assert.Single(_email.Gonderilenler);
            Assert.Contains("geçti", gonderi.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AktifZimmetliSurucuyeDeGonderilir()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var surucu = _db.KullaniciEkle("surucu@garajim.local", CompanyRole.Driver);
            var arac = _db.AracEkle(sahip.Id, "34EVJ006");

            _db.Context.VehicleAssignments.Add(new VehicleAssignment
            {
                CompanyId = arac.CompanyId,
                VehicleId = arac.Id,
                UserId = surucu.Id,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = null,
                AssignedByUserId = sahip.Id,
                CreatedAt = DateTime.UtcNow
            });
            _db.Context.SaveChanges();

            AracEvrakiEkle(10, arac);

            await JobOlustur().RunAsync();

            var alicilar = _email.Gonderilenler.Select(g => g.To).ToList();
            Assert.Contains("sahip@garajim.local", alicilar);
            Assert.Contains("surucu@garajim.local", alicilar);
        }

        [Fact]
        public async Task SurucuEvrakiSurucuyeVeSahibeGonderilir()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var surucu = _db.KullaniciEkle("surucu@garajim.local", CompanyRole.Driver);

            _db.Context.EvrakKayitlari.Add(new EvrakKaydi
            {
                CompanyId = surucu.CompanyId,
                UserId = surucu.Id,
                EvrakTuru = EvrakTuru.Ehliyet,
                BitisTarihi = DateTime.UtcNow.Date.AddDays(5),
                Aktif = true,
                OlusturmaTarihi = DateTime.UtcNow
            });
            _db.Context.SaveChanges();

            await JobOlustur().RunAsync();

            var alicilar = _email.Gonderilenler.Select(g => g.To).ToList();
            Assert.Contains("surucu@garajim.local", alicilar);
            Assert.Contains("sahip@garajim.local", alicilar);
        }

        [Fact]
        public async Task BaskaSirketinEvrakiIcinGonderilmez()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var arac = _db.AracEkle(sahip.Id, "34EVJ007");
            AracEvrakiEkle(10, arac);

            var yabanciSirket = _db.SirketEkle("Yabancı Filo");
            var yabanciKullanici = _db.KullaniciEkle("yabanci@garajim.local", yabanciSirket.Id);
            var yabanciArac = _db.AracEkleSirketle(yabanciKullanici.Id, "06EVJ008", yabanciSirket.Id);
            AracEvrakiEkle(10, yabanciArac);

            await JobOlustur().RunAsync();

            Assert.Equal(2, _email.Gonderilenler.Count);
            Assert.Contains(_email.Gonderilenler, g => g.To == "sahip@garajim.local");
            Assert.Contains(_email.Gonderilenler, g => g.To == "yabanci@garajim.local");
            Assert.DoesNotContain(_email.Gonderilenler, g => g.To == "sahip@garajim.local" && g.Subject.Contains("06EVJ008"));
        }

        [Fact]
        public async Task UyariGunleriConfigdenDaraltilabilir()
        {
            var sahip = _db.KullaniciEkle("sahip@garajim.local");
            var arac = _db.AracEkle(sahip.Id, "34EVJ009");
            AracEvrakiEkle(10, arac);

            await JobOlustur(new Dictionary<string, string> { ["Evrak:UyariGunleri"] = "3" }).RunAsync();

            Assert.Empty(_email.Gonderilenler);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
