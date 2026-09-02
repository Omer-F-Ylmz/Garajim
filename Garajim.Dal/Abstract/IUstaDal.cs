using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IUstaSohbetDal : IEntityRepository<UstaSohbet>
    {
        Task<List<UstaSohbet>> GetListeAsync(int? vehicleId, int? userId);
        Task<List<int>> EskiSohbetIdleriAsync(DateTime sinir);
        Task SohbetleriSilAsync(List<int> sohbetIds);
    }

    public interface IUstaMesajDal : IEntityRepository<UstaMesaj>
    {
        Task<List<UstaMesaj>> GetSohbetMesajlariAsync(int sohbetId);
        Task<int> SohbetMesajSayisiAsync(int sohbetId);
        Task<int> KullaniciGunlukSayisiAsync(int sohbetSahibiUserId, DateTime gunBasi);
        Task<UstaIstatistikDto> IstatistikAsync();
        Task<List<UstaMesaj>> GetOzetlenmemisCozumluMesajlarAsync();
        Task OzetlendiIsaretleAsync(List<int> mesajIds);
        Task DeleteBySohbetAsync(int sohbetId);
        Task DeleteBySohbetlerAsync(List<int> sohbetIds);
    }

    public interface IUstaOnayDal : IEntityRepository<UstaOnay>
    {
    }

    public interface IUstaCozumOzetiDal : IEntityRepository<UstaCozumOzeti>
    {
        Task<List<UstaCozumOzeti>> GetTumuAsync();
        Task<UstaCozumOzeti> BulAsync(string marka, string model, string motor, string kategori, string parca);
    }
}
