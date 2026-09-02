using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IHasarDosyasiDal : IEntityRepository<HasarDosyasi>
    {
        Task<List<HasarDosyasi>> GetListeAsync(List<int> vehicleIds, int limit);
        Task<int> AcikSayisiAsync(List<int> vehicleIds);
    }

    public interface IHasarFotoDal : IEntityRepository<HasarFoto>
    {
        Task<List<HasarFoto>> GetByDosyaAsync(int hasarDosyasiId);
        Task<int> SayiAsync(int hasarDosyasiId);
        Task<int> SonSiraAsync(int hasarDosyasiId);
    }
}
