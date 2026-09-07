using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Dal.Abstract
{
    public interface IEvrakDal : IEntityRepository<EvrakKaydi>
    {
        Task<List<EvrakDueDto>> GetDueListAsync(DateTime dueLimit, DateTime notifyBefore);
        Task<bool> TryClaimNotificationAsync(int evrakId, DateTime now, DateTime notifyBefore);
        Task PasiflestirAsync(int id);
        Task<List<EvrakKaydi>> AktifListeAsync(int vehicleId, int limit);
        Task<SayfaliSonuc<EvrakKaydi>> SayfaAsync(int? vehicleId, int? surucuKullaniciId, List<int> erisilebilirAracIdler, ListeSorgusu sorgu, SiralamaSonucu siralama);
        Task<(int Gecti, int Yaklasiyor)> DurumSayilariAsync(List<int> vehicleIds, int? userId, DateTime bugun, int yaklasiyorGun);
    }

    public class EvrakDueDto
    {
        public int EvrakId { get; set; }
        public int CompanyId { get; set; }
        public int? VehicleId { get; set; }
        public int? UserId { get; set; }
        public string Plate { get; set; }
        public Garajim.Entity.Enums.EvrakTuru EvrakTuru { get; set; }
        public DateTime BitisTarihi { get; set; }
    }
}
