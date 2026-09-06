using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Dal.Sorgular;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfEvrakDal : EfEntityRepositoryBase<EvrakKaydi, GarajimDbContext>, IEvrakDal
    {
        public EfEvrakDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<EvrakDueDto>> GetDueListAsync(DateTime dueLimit, DateTime notifyBefore)
        {
            var query = from e in Context.EvrakKayitlari
                        where e.Aktif
                              && e.BitisTarihi <= dueLimit
                              && (e.LastNotifiedAt == null || e.LastNotifiedAt <= notifyBefore)
                        join v in Context.Vehicles on e.VehicleId equals v.Id into araclar
                        from arac in araclar.DefaultIfEmpty()
                        where arac == null || !arac.Arsivli
                        select new EvrakDueDto
                        {
                            EvrakId = e.Id,
                            CompanyId = e.CompanyId,
                            VehicleId = e.VehicleId,
                            UserId = e.UserId,
                            Plate = arac == null ? null : arac.Plate,
                            EvrakTuru = e.EvrakTuru,
                            BitisTarihi = e.BitisTarihi
                        };

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<bool> TryClaimNotificationAsync(int evrakId, DateTime now, DateTime notifyBefore)
        {
            var affected = await Context.EvrakKayitlari
                .Where(e => e.Id == evrakId
                            && e.Aktif
                            && (e.LastNotifiedAt == null || e.LastNotifiedAt <= notifyBefore))
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.LastNotifiedAt, now));
            return affected > 0;
        }

        public async Task<(int Gecti, int Yaklasiyor)> DurumSayilariAsync(List<int> vehicleIds, int? userId, DateTime bugun, int yaklasiyorGun)
        {
            var esik = bugun.AddDays(yaklasiyorGun);

            var kapsam = Context.EvrakKayitlari
                .AsNoTracking()
                .Where(e => e.Aktif &&
                            ((e.VehicleId != null && vehicleIds.Contains(e.VehicleId.Value)) ||
                             (userId != null && e.UserId == userId)));

            var gecti = await kapsam.CountAsync(e => e.BitisTarihi < bugun);
            var yaklasiyor = await kapsam.CountAsync(e => e.BitisTarihi >= bugun && e.BitisTarihi <= esik);

            return (gecti, yaklasiyor);
        }

        public async Task PasiflestirAsync(int id)
        {
            await Context.EvrakKayitlari
                .Where(e => e.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.Aktif, false));
        }
        public Task<SayfaliSonuc<EvrakKaydi>> SayfaAsync(int? vehicleId, int? surucuKullaniciId, List<int> erisilebilirAracIdler, ListeSorgusu sorgu, SiralamaSonucu siralama)
        {
            var sorgulama = Context.EvrakKayitlari.AsNoTracking().AsQueryable();

            if (vehicleId != null)
            {
                sorgulama = sorgulama.Where(e => e.VehicleId == vehicleId);
            }

            if (surucuKullaniciId != null)
            {
                var idler = erisilebilirAracIdler ?? new List<int>();
                sorgulama = sorgulama.Where(e => e.UserId == surucuKullaniciId
                    || (e.UserId == null && e.VehicleId != null && idler.Contains(e.VehicleId.Value)));
            }

            var metin = TurkceArama.Iceren<EvrakKaydi>(sorgu.GecerliQ(), e => e.Saglayici, e => e.PoliceNo, e => e.Not);

            return Sayfalayici.UygulaAsync(sorgulama, sorgu, siralama, e => e.BitisTarihi, metin, Sirala);
        }

        private static IQueryable<EvrakKaydi> Sirala(IQueryable<EvrakKaydi> sorgulama, SiralamaSonucu siralama)
        {
            return siralama.Alan switch
            {
                "tur" => siralama.Artan
                    ? sorgulama.OrderBy(e => e.EvrakTuru).ThenBy(e => e.Id)
                    : sorgulama.OrderByDescending(e => e.EvrakTuru).ThenByDescending(e => e.Id),
                "durum" => siralama.Artan
                    ? sorgulama.OrderBy(e => e.Aktif).ThenBy(e => e.BitisTarihi).ThenBy(e => e.Id)
                    : sorgulama.OrderByDescending(e => e.Aktif).ThenBy(e => e.BitisTarihi).ThenBy(e => e.Id),
                "saglayici" => siralama.Artan
                    ? sorgulama.OrderBy(e => e.Saglayici).ThenBy(e => e.Id)
                    : sorgulama.OrderByDescending(e => e.Saglayici).ThenByDescending(e => e.Id),
                _ => siralama.Artan
                    ? sorgulama.OrderBy(e => e.BitisTarihi).ThenBy(e => e.Id)
                    : sorgulama.OrderByDescending(e => e.BitisTarihi).ThenByDescending(e => e.Id)
            };
        }
    }
}

