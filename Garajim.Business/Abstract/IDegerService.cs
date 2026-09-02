using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IDegerService
    {
        Task<IDataResult<DegerSerisiDto>> GetSeriAsync(int userId, int vehicleId);
        Task<IDataResult<AracDegerDto>> GirAsync(int userId, int vehicleId, DegerGirDto dto);
        Task<IDataResult<DegerTahminSonucuDto>> TahminAsync(int userId, int vehicleId);
        Task<decimal> FiloToplamDegerAsync(List<int> vehicleIds);
        Task<AracDegerDto> KarneDegeriAsync(int vehicleId);
        Task<decimal?> DonemDegerKaybiAsync(int vehicleId, DateTime baslangic, DateTime bitis);
    }
}
