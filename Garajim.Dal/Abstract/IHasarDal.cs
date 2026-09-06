using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IHasarDosyasiDal : IEntityRepository<HasarDosyasi>
    {
        Task<List<HasarDosyasi>> GetListeAsync(List<int> vehicleIds, int limit);
        Task<int> AcikSayisiAsync(List<int> vehicleIds);
        Task<SayfaliSonuc<HasarDosyasi>> SayfaAsync(List<int> vehicleIds, ListeSorgusu sorgu, SiralamaSonucu siralama);
    }

    public interface IHasarFotoDal : IEntityRepository<HasarFoto>
    {
        Task<List<HasarFoto>> GetByDosyaAsync(int hasarDosyasiId);
        Task<int> SayiAsync(int hasarDosyasiId);
        Task<Dictionary<int, int>> SayilarAsync(List<int> hasarDosyasiIdleri);
        Task<int> SonSiraAsync(int hasarDosyasiId);
        Task<List<int>> AracinFotoBelgeIdleriAsync(int vehicleId);
    }
}
