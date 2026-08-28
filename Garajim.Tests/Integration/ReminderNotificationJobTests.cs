using Garajim.Business.Abstract;
using Garajim.Business.Jobs;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Tests.Integration
{
    public class ReminderNotificationJobTests : IDisposable
    {
        private sealed class SahteEmailService : IEmailService
        {
            public List<(string To, string Subject, string Body)> Gonderilenler { get; } = new List<(string, string, string)>();

            public Task SendAsync(string to, string subject, string body)
            {
                Gonderilenler.Add((to, subject, body));
                return Task.CompletedTask;
            }
        }

        private sealed class PatlayanEmailService : IEmailService
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
            var job = new ReminderNotificationJob(_db.ReminderDal, _email);

            await job.RunAsync();

            Assert.Single(_email.Gonderilenler);
            Assert.Equal("surucu@garajim.local", _email.Gonderilenler[0].To);
            Assert.Contains("34AAA111", _email.Gonderilenler[0].Subject);
            Assert.Contains("araç muayenesi", _email.Gonderilenler[0].Subject);
            Assert.Contains("3 gün kaldı", _email.Gonderilenler[0].Body);
        }

        [Fact]
        public async Task RunAsync_IkinciKosudaAyniHatirlatmaIcinEpostaGonderilmez()
        {
            HatirlatmaEkle(3);
            var job = new ReminderNotificationJob(_db.ReminderDal, _email);

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
            var job = new ReminderNotificationJob(_db.ReminderDal, _email);

            await job.RunAsync();

            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task RunAsync_UzakTarihliHatirlatmaIcinEpostaGonderilmez()
        {
            HatirlatmaEkle(30);
            var job = new ReminderNotificationJob(_db.ReminderDal, _email);

            await job.RunAsync();

            Assert.Empty(_email.Gonderilenler);
        }

        [Fact]
        public async Task RunAsync_GecmisTarihliHatirlatmaIcinGectiMesajiGonderilir()
        {
            HatirlatmaEkle(-2);
            var job = new ReminderNotificationJob(_db.ReminderDal, _email);

            await job.RunAsync();

            Assert.Single(_email.Gonderilenler);
            Assert.Contains("2 gün önce geçti", _email.Gonderilenler[0].Body);
        }

        [Fact]
        public async Task RunAsync_EpostaHatasiJobuDurdurmaz()
        {
            HatirlatmaEkle(1);
            var patlayan = new PatlayanEmailService();
            var job = new ReminderNotificationJob(_db.ReminderDal, patlayan);

            await job.RunAsync();

            Assert.Equal(1, patlayan.Cagrildi);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
