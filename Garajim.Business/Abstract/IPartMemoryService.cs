using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Abstract
{
    public interface IPartMemoryService
    {
        Task<IDataResult<List<ParcaHafizasiDto>>> GetAsync(int userId, int vehicleId);
        Task<IResult> CreateReminderAsync(int userId, int vehicleId, ParcaTuru parcaTuru);
    }
}
