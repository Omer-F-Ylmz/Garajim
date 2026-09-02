using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IAracDegerDal : IEntityRepository<AracDeger>
    {
        Task<List<AracDeger>> GetSeriAsync(int vehicleId, int limit);
        Task<AracDeger> SonDegerAsync(int vehicleId);
        Task<int> GunlukTahminSayisiAsync(int vehicleId, DateTime gun);
        Task<decimal> FiloToplamSonDegerAsync(List<int> vehicleIds);
        Task<AracDeger> AraliktakiIlkAsync(int vehicleId, DateTime baslangic, DateTime bitis);
        Task<AracDeger> AraliktakiSonAsync(int vehicleId, DateTime baslangic, DateTime bitis);
    }
}
