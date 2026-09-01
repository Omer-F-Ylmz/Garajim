using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IKarnePaylasimiDal : IEntityRepository<KarnePaylasimi>
    {
        Task<KarnePaylasimi> GetByTokenHashAsync(string tokenHash);
        Task PasiflestirAsync(int vehicleId);
        Task GoruntulenmeArtirAsync(int id);
        Task<(int AktifArac, int ToplamGoruntulenme)> IstatistikAsync();
    }
}
