using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IFuelService
    {
        Task<IDataResult<List<FuelDto>>> GetListAsync(int userId, int vehicleId);
        Task<IDataResult<SayfaliSonuc<FuelDto>>> GetSayfaAsync(int userId, int vehicleId, ListeSorgusu sorgu);
        Task<IDataResult<FuelDto>> AddAsync(int userId, FuelCreateDto dto);
        Task<IResult> DeleteAsync(int userId, int id);
    }
}
