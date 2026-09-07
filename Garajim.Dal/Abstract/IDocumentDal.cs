using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IDocumentDal : IEntityRepository<Document>
    {
        Task<long> GetCompanyTotalSizeAsync();
        Task<SayfaliSonuc<Document>> SayfaAsync(int? vehicleId, int? maintenanceRecordId, ListeSorgusu sorgu, SiralamaSonucu siralama);
        Task<List<Document>> KarneBelgeleriAsync(int vehicleId, int limit);
    }
}
