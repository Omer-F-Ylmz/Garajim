using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IEvrakService
    {
        Task<IDataResult<List<EvrakDto>>> GetListAsync(int userId, int? vehicleId);
        Task<IDataResult<List<EvrakDto>>> GetTakvimAsync(int userId, string ay);
        Task<IDataResult<EvrakDto>> GetByIdAsync(int userId, int id);
        Task<IDataResult<EvrakDto>> AddAsync(int userId, EvrakCreateDto dto);
        Task<IDataResult<EvrakDto>> UpdateAsync(int userId, int id, EvrakUpdateDto dto);
        Task<IDataResult<EvrakDto>> YenileAsync(int userId, int id);
        Task<IResult> DeleteAsync(int userId, int id);
    }
}
