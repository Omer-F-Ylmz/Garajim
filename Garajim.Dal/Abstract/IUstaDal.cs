using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface IUstaSohbetDal : IEntityRepository<UstaSohbet>
    {
        Task<List<UstaSohbet>> GetListeAsync(int? vehicleId, int? userId);
    }

    public interface IUstaMesajDal : IEntityRepository<UstaMesaj>
    {
        Task<List<UstaMesaj>> GetSohbetMesajlariAsync(int sohbetId);
        Task<int> SohbetMesajSayisiAsync(int sohbetId);
        Task<int> KullaniciGunlukSayisiAsync(int sohbetSahibiUserId, DateTime gunBasi);
        Task<List<UstaMesaj>> GetCozumluMesajlarAsync();
        Task DeleteBySohbetAsync(int sohbetId);
    }

    public interface IUstaOnayDal : IEntityRepository<UstaOnay>
    {
    }

    public interface IUstaCozumOzetiDal : IEntityRepository<UstaCozumOzeti>
    {
        Task<List<UstaCozumOzeti>> GetTumuAsync();
        Task TemizleAsync();
    }
}
