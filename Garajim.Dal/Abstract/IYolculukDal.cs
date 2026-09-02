using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IYolculukDal : IEntityRepository<YolculukKaydi>
    {
        Task<List<YolculukKaydi>> GetListeAsync(List<int> vehicleIds, DateTime baslangic, DateTime bitis, int limit);
        Task<List<AmacToplamDto>> AmacToplamlariAsync(List<int> vehicleIds, DateTime baslangic, DateTime bitis);
    }
}
