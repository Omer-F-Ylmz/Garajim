using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IReminderDal : IEntityRepository<Reminder>
    {
        Task<List<ReminderDueDto>> GetDueListAsync(DateTime dueLimit, DateTime notifyBefore);
        Task<bool> TryClaimNotificationAsync(int reminderId, DateTime now, DateTime notifyBefore);
        Task<List<UpcomingReminderDto>> GetUpcomingForUserAsync(int userId, DateTime limit);
        Task<List<Reminder>> GetListForVehicleAsync(int vehicleId, int limit);
        Task<int> YaklasanSayisiAsync(List<int> vehicleIds, DateTime limit);
    }
}
