using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Dal.Sorgular;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfReminderDal : EfEntityRepositoryBase<Reminder, GarajimDbContext>, IReminderDal
    {
        public EfReminderDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<ReminderDueDto>> GetDueListAsync(DateTime dueLimit, DateTime notifyBefore)
        {
            var query = from r in Context.Reminders
                        join v in Context.Vehicles on r.VehicleId equals v.Id
                        join u in Context.Users on v.UserId equals u.Id
                        where !r.IsCompleted
                              && u.BildirimHatirlatma
                              && !v.Arsivli
                              && r.DueDate != null
                              && r.DueDate <= dueLimit
                              && (r.LastNotifiedAt == null || r.LastNotifiedAt <= notifyBefore)
                        select new ReminderDueDto
                        {
                            ReminderId = r.Id,
                            Email = u.Email,
                            FullName = u.FullName,
                            Plate = v.Plate,
                            Type = r.Type,
                            DueDate = r.DueDate.Value
                        };
            return await query.ToListAsync();
        }

        public async Task<bool> TryClaimNotificationAsync(int reminderId, DateTime now, DateTime notifyBefore)
        {
            var affected = await Context.Reminders
                .Where(r => r.Id == reminderId
                            && !r.IsCompleted
                            && (r.LastNotifiedAt == null || r.LastNotifiedAt <= notifyBefore))
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.LastNotifiedAt, now));
            return affected > 0;
        }

        public async Task<List<UpcomingReminderDto>> GetUpcomingForUserAsync(int userId, DateTime limit)
        {
            var query = from r in Context.Reminders
                        join v in Context.Vehicles on r.VehicleId equals v.Id
                        where v.UserId == userId
                              && !v.Arsivli
                              && !r.IsCompleted
                              && r.DueDate != null
                              && r.DueDate <= limit
                        orderby r.DueDate
                        select new UpcomingReminderDto
                        {
                            Id = r.Id,
                            VehicleId = v.Id,
                            Plate = v.Plate,
                            Type = r.Type,
                            DueDate = r.DueDate.Value,
                            Note = r.Note
                        };
            return await query.ToListAsync();
        }
        public async Task<int> YaklasanSayisiAsync(List<int> vehicleIds, DateTime limit)
        {
            return await Context.Reminders
                .AsNoTracking()
                .CountAsync(r => !r.IsCompleted &&
                                 vehicleIds.Contains(r.VehicleId) &&
                                 r.DueDate != null &&
                                 r.DueDate <= limit);
        }

        public async Task<List<Reminder>> GetListForVehicleAsync(int vehicleId, int limit)
        {
            return await Context.Reminders
                .AsNoTracking()
                .Where(r => r.VehicleId == vehicleId)
                .OrderBy(r => r.IsCompleted)
                .ThenBy(r => r.DueDate)
                .Take(limit)
                .ToListAsync();
        }
        public Task<SayfaliSonuc<Reminder>> SayfaAsync(int vehicleId, ListeSorgusu sorgu, SiralamaSonucu siralama)
        {
            var sorgulama = Context.Reminders.AsNoTracking().Where(r => r.VehicleId == vehicleId);
            var metin = TurkceArama.Iceren<Reminder>(sorgu.GecerliQ(), r => r.Note);

            return Sayfalayici.UygulaAsync(sorgulama, sorgu, siralama, r => r.DueDate, metin, Sirala);
        }

        private static IQueryable<Reminder> Sirala(IQueryable<Reminder> sorgulama, SiralamaSonucu siralama)
        {
            return siralama.Alan switch
            {
                "km" => siralama.Artan
                    ? sorgulama.OrderBy(r => r.DueKm).ThenBy(r => r.Id)
                    : sorgulama.OrderByDescending(r => r.DueKm).ThenByDescending(r => r.Id),
                "durum" => siralama.Artan
                    ? sorgulama.OrderBy(r => r.IsCompleted).ThenBy(r => r.Id)
                    : sorgulama.OrderByDescending(r => r.IsCompleted).ThenByDescending(r => r.Id),
                _ => siralama.Artan
                    ? sorgulama.OrderBy(r => r.DueDate).ThenBy(r => r.Id)
                    : sorgulama.OrderByDescending(r => r.DueDate).ThenByDescending(r => r.Id)
            };
        }
        public Task<bool> TekrardanUretilmisMiAsync(int kaynakId)
        {
            return Context.Reminders.AsNoTracking().AnyAsync(r => r.TekrardanUretenId == kaynakId);
        }
    }
}


