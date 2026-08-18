using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
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
    }
}
