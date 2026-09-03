using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Parts;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class PartMemoryManager : IPartMemoryService
    {
        private const int YaklasiyorKmPayi = 10;
        private const int YaklasiyorGun = 30;

        private readonly IMaintenancePartDal _partDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IReminderDal _reminderDal;
        private readonly IVehicleAccessService _vehicleAccess;

        public PartMemoryManager(
            IMaintenancePartDal partDal,
            IMaintenanceDal maintenanceDal,
            IReminderDal reminderDal,
            IVehicleAccessService vehicleAccess)
        {
            _partDal = partDal;
            _maintenanceDal = maintenanceDal;
            _reminderDal = reminderDal;
            _vehicleAccess = vehicleAccess;
        }

        public async Task<IDataResult<List<ParcaHafizasiDto>>> GetAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<List<ParcaHafizasiDto>>(Messages.VehicleNotFound);

            var liste = await HesaplaAsync(vehicle);
            return new SuccessDataResult<List<ParcaHafizasiDto>>(liste);
        }

        public async Task<IDataResult<int>> CreateReminderAsync(int userId, int vehicleId, ParcaTuru parcaTuru)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<int>(Messages.VehicleNotFound);

            if (!Enum.IsDefined(parcaTuru))
                return new ErrorDataResult<int>(Messages.InvalidValue);

            var hafiza = (await HesaplaAsync(vehicle)).FirstOrDefault(h => h.ParcaTuru == parcaTuru);
            if (hafiza == null)
                return new ErrorDataResult<int>(Messages.PartNeverReplaced);

            if (hafiza.SonrakiTahminiKm == null && hafiza.SonrakiTahminiTarih == null)
                return new ErrorDataResult<int>(Messages.PartHasNoInterval);

            var not = hafiza.ParcaAdi + " değişimi yaklaşıyor";

            var acikOlan = (await _reminderDal.GetListAsync(r =>
                r.VehicleId == vehicle.Id && !r.IsCompleted && r.Note == not)).FirstOrDefault();

            if (acikOlan != null)
                return new ErrorDataResult<int>(acikOlan.Id, Messages.ParcaHatirlatmasiZatenVar);

            var kayit = new Reminder
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                Type = ReminderType.PeriyodikBakim,
                DueDate = hafiza.SonrakiTahminiTarih,
                DueKm = hafiza.SonrakiTahminiKm,
                Note = not,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _reminderDal.AddAsync(kayit);

            return new SuccessDataResult<int>(kayit.Id, Messages.ReminderAdded);
        }

        private async Task<List<ParcaHafizasiDto>> HesaplaAsync(Vehicle vehicle)
        {
            var parcalar = await _partDal.GetByVehicleAsync(vehicle.Id);
            if (parcalar.Count == 0)
            {
                return new List<ParcaHafizasiDto>();
            }

            var kayitlar = await _maintenanceDal.GetListAsync(m => m.VehicleId == vehicle.Id);
            var kayitSozlugu = kayitlar.ToDictionary(k => k.Id);
            var bugun = Saat.BugunTr();

            var liste = new List<ParcaHafizasiDto>();

            foreach (var grup in parcalar.GroupBy(p => p.ParcaTuru))
            {
                var kayitliOlanlar = grup
                    .Where(p => kayitSozlugu.ContainsKey(p.MaintenanceRecordId))
                    .Select(p => new { Parca = p, Kayit = kayitSozlugu[p.MaintenanceRecordId] })
                    .OrderByDescending(x => x.Kayit.Date)
                    .ThenByDescending(x => x.Kayit.Km)
                    .ToList();

                if (kayitliOlanlar.Count == 0)
                {
                    continue;
                }

                var son = kayitliOlanlar[0];
                var aralik = ParcaAraliklari.Al(grup.Key);

                var dto = new ParcaHafizasiDto
                {
                    ParcaTuru = grup.Key,
                    ParcaAdi = aralik.Ad,
                    SonDegisimTarihi = son.Kayit.Date,
                    SonDegisimKm = son.Kayit.Km,
                    DegisimSayisi = kayitliOlanlar.Count,
                    ToplamTutar = grup.Sum(p => p.Tutar ?? 0m),
                    SonrakiTahminiKm = aralik.Km == null ? null : son.Kayit.Km + aralik.Km.Value,
                    SonrakiTahminiTarih = aralik.Ay == null ? null : son.Kayit.Date.AddMonths(aralik.Ay.Value)
                };

                dto.Durum = DurumBelirle(dto, aralik, vehicle.CurrentKm, bugun);
                liste.Add(dto);
            }

            return liste.OrderBy(l => l.ParcaAdi).ToList();
        }

        private static string DurumBelirle(ParcaHafizasiDto dto, ParcaAraligi aralik, int guncelKm, DateTime bugun)
        {
            if (dto.SonrakiTahminiKm == null && dto.SonrakiTahminiTarih == null)
            {
                return "Iyi";
            }

            if (dto.SonrakiTahminiKm != null && guncelKm >= dto.SonrakiTahminiKm.Value)
            {
                return "Gecti";
            }

            if (dto.SonrakiTahminiTarih != null && bugun >= dto.SonrakiTahminiTarih.Value.Date)
            {
                return "Gecti";
            }

            if (dto.SonrakiTahminiKm != null && aralik.Km != null)
            {
                var kalan = dto.SonrakiTahminiKm.Value - guncelKm;
                if (kalan <= aralik.Km.Value * YaklasiyorKmPayi / 100)
                {
                    return "Yaklasiyor";
                }
            }

            if (dto.SonrakiTahminiTarih != null)
            {
                var kalanGun = (dto.SonrakiTahminiTarih.Value.Date - bugun).TotalDays;
                if (kalanGun <= YaklasiyorGun)
                {
                    return "Yaklasiyor";
                }
            }

            return "Iyi";
        }
    }
}
