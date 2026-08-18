using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Tests.Integration
{
    public class ReminderNotificationClaimTests : IDisposable
    {
        private readonly SqliteTestDatabase _db = new SqliteTestDatabase();

        private readonly int _userId;
        private readonly Vehicle _arac;

        public ReminderNotificationClaimTests()
        {
            _userId = _db.KullaniciEkle("a@garajim.local").Id;
            _arac = _db.AracEkle(_userId, "34AAA111");
        }

        private Reminder HatirlatmaEkle(DateTime? lastNotifiedAt = null, bool isCompleted = false)
        {
            var reminder = new Reminder
            {
                VehicleId = _arac.Id,
                Type = ReminderType.Muayene,
                DueDate = DateTime.UtcNow.Date.AddDays(5),
                IsCompleted = isCompleted,
                LastNotifiedAt = lastNotifiedAt,
                CreatedAt = new DateTime(2026, 1, 1)
            };

            _db.Context.Reminders.Add(reminder);
            _db.Context.SaveChanges();
            return reminder;
        }

        [Fact]
        public async Task TryClaimNotificationAsync_AyniHatirlatmaIkinciKezClaimEdilemez()
        {
            var reminder = HatirlatmaEkle();
            var now = DateTime.UtcNow;
            var notifyBefore = now.AddDays(-1);

            var ilkClaim = await _db.ReminderDal.TryClaimNotificationAsync(reminder.Id, now, notifyBefore);
            var ikinciClaim = await _db.ReminderDal.TryClaimNotificationAsync(reminder.Id, now.AddMinutes(1), notifyBefore);

            Assert.True(ilkClaim);
            Assert.False(ikinciClaim);

            var veritabanindaki = await _db.Context.Reminders.AsNoTracking().SingleAsync(r => r.Id == reminder.Id);
            Assert.NotNull(veritabanindaki.LastNotifiedAt);
            Assert.Equal(now, veritabanindaki.LastNotifiedAt.Value, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task TryClaimNotificationAsync_TamamlanmisHatirlatmaClaimEdilemez()
        {
            var reminder = HatirlatmaEkle(isCompleted: true);
            var now = DateTime.UtcNow;

            var claim = await _db.ReminderDal.TryClaimNotificationAsync(reminder.Id, now, now.AddDays(-1));

            Assert.False(claim);
            var veritabanindaki = await _db.Context.Reminders.AsNoTracking().SingleAsync(r => r.Id == reminder.Id);
            Assert.Null(veritabanindaki.LastNotifiedAt);
        }

        [Fact]
        public async Task TryClaimNotificationAsync_EskiBildirimdenSonraTekrarClaimEdilebilir()
        {
            var now = DateTime.UtcNow;
            var reminder = HatirlatmaEkle(lastNotifiedAt: now.AddDays(-10));

            var claim = await _db.ReminderDal.TryClaimNotificationAsync(reminder.Id, now, now.AddDays(-1));

            Assert.True(claim);
        }

        [Fact]
        public async Task GetDueListAsync_ClaimEdilenHatirlatmaTekrarListelenmez()
        {
            var reminder = HatirlatmaEkle();
            var now = DateTime.UtcNow;
            var dueLimit = now.Date.AddDays(30);
            var notifyBefore = now.AddDays(-1);

            var claimOncesi = await _db.ReminderDal.GetDueListAsync(dueLimit, notifyBefore);
            await _db.ReminderDal.TryClaimNotificationAsync(reminder.Id, now, notifyBefore);
            var claimSonrasi = await _db.ReminderDal.GetDueListAsync(dueLimit, notifyBefore);

            Assert.Single(claimOncesi);
            Assert.Equal("a@garajim.local", claimOncesi[0].Email);
            Assert.Equal("34AAA111", claimOncesi[0].Plate);
            Assert.Empty(claimSonrasi);
        }

        [Fact]
        public async Task TryClaimNotificationAsync_OlmayanHatirlatmaIcinFalseDoner()
        {
            var now = DateTime.UtcNow;

            var claim = await _db.ReminderDal.TryClaimNotificationAsync(9999, now, now.AddDays(-1));

            Assert.False(claim);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
