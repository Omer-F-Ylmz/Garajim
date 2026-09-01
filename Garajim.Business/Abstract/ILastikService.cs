using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface ILastikService
    {
        Task<IDataResult<LastikDurumDto>> GetDurumAsync(int userId, int vehicleId);
        Task<IDataResult<LastikDto>> TakAsync(int userId, LastikTakDto dto);
        Task<IResult> SokAsync(int userId, int id, LastikSokDto dto);
        Task<IResult> DeleteAsync(int userId, int id);
    }
}
