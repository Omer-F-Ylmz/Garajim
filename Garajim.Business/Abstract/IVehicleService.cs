using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Abstract
{
    public interface IVehicleService
    {
        Task<IDataResult<List<VehicleDto>>> GetAllAsync(int userId, bool arsiv = false);
        Task<IDataResult<VehicleDto>> GetByIdAsync(int userId, int id);
        Task<IDataResult<VehicleDto>> AddAsync(int userId, VehicleCreateDto dto);
        Task<IResult> UpdateAsync(int userId, int id, VehicleUpdateDto dto);
        Task<IResult> KasaTipiSecAsync(int userId, int id, KasaTipi kasaTipi);
        Task<IResult> DeleteAsync(int userId, int id);
        Task<IResult> ArsivleAsync(int userId, int id, ArsivNedeni neden);
        Task<IResult> ArsivdenAlAsync(int userId, int id);
    }
}
