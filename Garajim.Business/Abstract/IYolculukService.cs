using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IYolculukService
    {
        Task<IDataResult<List<YolculukDto>>> GetListAsync(int userId, int? vehicleId, DateTime? baslangic, DateTime? bitis);
        Task<IDataResult<YolculukOzetDto>> GetOzetAsync(int userId, int? vehicleId, DateTime? baslangic, DateTime? bitis);
        Task<IDataResult<YolculukDto>> AddAsync(int userId, YolculukCreateDto dto);
        Task<IResult> UpdateAsync(int userId, int id, YolculukUpdateDto dto);
        Task<IResult> DeleteAsync(int userId, int id);
    }
}
