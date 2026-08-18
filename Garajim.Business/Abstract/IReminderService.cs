using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IReminderService
    {
        Task<IDataResult<List<ReminderDto>>> GetListAsync(int userId, int vehicleId);
        Task<IDataResult<List<UpcomingReminderDto>>> GetUpcomingAsync(int userId, int days);
        Task<IDataResult<ReminderDto>> AddAsync(int userId, ReminderCreateDto dto);
        Task<IResult> CompleteAsync(int userId, int id);
        Task<IResult> DeleteAsync(int userId, int id);
    }
}
