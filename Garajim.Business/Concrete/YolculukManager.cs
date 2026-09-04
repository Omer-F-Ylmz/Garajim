using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class YolculukManager : IYolculukService
    {
        private readonly IYolculukDal _yolculukDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IVehicleAccessService _vehicleAccess;

        public YolculukManager(IYolculukDal yolculukDal, IUserDal userDal, IVehicleDal vehicleDal, IVehicleAccessService vehicleAccess)
        {
            _yolculukDal = yolculukDal;
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _vehicleAccess = vehicleAccess;
        }

        public async Task<IDataResult<List<YolculukDto>>> GetListAsync(int userId, int? vehicleId, DateTime? baslangic, DateTime? bitis)
        {
            var kapsam = await KapsamAsync(userId, vehicleId);
            if (kapsam.Hata != null)
                return new ErrorDataResult<List<YolculukDto>>(kapsam.Hata);

            var bas = baslangic?.Date ?? DateTime.MinValue;
            var son = GunSonu(bitis);

            if (kapsam.Araclar.Count == 0)
                return new SuccessDataResult<List<YolculukDto>>(new List<YolculukDto>());

            var kayitlar = await _yolculukDal.GetListeAsync(kapsam.Araclar.Select(a => a.Id).ToList(), bas, son, QueryLimits.MaxListSize);
            var kullanicilar = await KullanicilarAsync(kayitlar.Select(k => k.UserId));
            var plakalar = kapsam.Araclar.ToDictionary(a => a.Id, a => a.Plate);

            var liste = kayitlar.Select(k => MapToDto(k, plakalar, kullanicilar)).ToList();
            return new SuccessDataResult<List<YolculukDto>>(liste);
        }

        public async Task<IDataResult<YolculukOzetDto>> GetOzetAsync(int userId, int? vehicleId, DateTime? baslangic, DateTime? bitis)
        {
            var kapsam = await KapsamAsync(userId, vehicleId);
            if (kapsam.Hata != null)
                return new ErrorDataResult<YolculukOzetDto>(kapsam.Hata);

            var bas = baslangic?.Date ?? DateTime.MinValue;
            var son = GunSonu(bitis);

            var ozet = new YolculukOzetDto { Baslangic = bas, Bitis = bitis?.Date ?? DateTime.MaxValue.Date };
            if (kapsam.Araclar.Count == 0)
                return new SuccessDataResult<YolculukOzetDto>(ozet);

            var toplamlar = await _yolculukDal.AmacToplamlariAsync(kapsam.Araclar.Select(a => a.Id).ToList(), bas, son);

            ozet.IsKm = toplamlar.Where(t => t.Amac == YolculukAmaci.Is).Sum(t => t.ToplamKm);
            ozet.OzelKm = toplamlar.Where(t => t.Amac == YolculukAmaci.Ozel).Sum(t => t.ToplamKm);
            ozet.ToplamKm = ozet.IsKm + ozet.OzelKm;
            ozet.YolculukSayisi = toplamlar.Sum(t => t.Adet);

            if (ozet.ToplamKm > 0)
            {
                ozet.IsOrani = Math.Round((decimal)ozet.IsKm / ozet.ToplamKm * 100, 1);
            }

            return new SuccessDataResult<YolculukOzetDto>(ozet);
        }

        public async Task<IDataResult<YolculukDto>> AddAsync(int userId, YolculukCreateDto dto)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<YolculukDto>(Messages.UserNotFound);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<YolculukDto>(Messages.VehicleNotFound);

            var hata = Dogrula(dto.Tarih, dto.BaslangicKm, dto.BitisKm, dto.Amac);
            if (hata != null)
                return new ErrorDataResult<YolculukDto>(hata);

            if (await CakisiyorMuAsync(vehicle.Id, dto.BaslangicKm, dto.BitisKm, null))
                return new ErrorDataResult<YolculukDto>(Messages.YolculukKmCakisiyor);

            var kayit = new YolculukKaydi
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                UserId = userId,
                Tarih = dto.Tarih.Date,
                BaslangicKm = dto.BaslangicKm,
                BitisKm = dto.BitisKm,
                MesafeKm = dto.BitisKm - dto.BaslangicKm,
                Amac = dto.Amac,
                Nereden = Kirp(dto.Nereden, 150),
                Nereye = Kirp(dto.Nereye, 150),
                Not = Kirp(dto.Not, 500),
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _yolculukDal.AddAsync(kayit);

            if (kayit.BitisKm > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = kayit.BitisKm;
                await _vehicleDal.UpdateAsync(vehicle);
            }

            var kullanicilar = await KullanicilarAsync(new[] { userId });
            var plakalar = new Dictionary<int, string> { [vehicle.Id] = vehicle.Plate };

            return new SuccessDataResult<YolculukDto>(MapToDto(kayit, plakalar, kullanicilar), Messages.YolculukEklendi);
        }

        public async Task<IResult> UpdateAsync(int userId, int id, YolculukUpdateDto dto)
        {
            var erisim = await KayitErisimiAsync(userId, id);
            if (erisim.Hata != null)
                return new ErrorResult(erisim.Hata);

            var hata = Dogrula(dto.Tarih, dto.BaslangicKm, dto.BitisKm, dto.Amac);
            if (hata != null)
                return new ErrorResult(hata);

            if (await CakisiyorMuAsync(erisim.Kayit.VehicleId, dto.BaslangicKm, dto.BitisKm, erisim.Kayit.Id))
                return new ErrorResult(Messages.YolculukKmCakisiyor);

            var kayit = erisim.Kayit;
            kayit.Tarih = dto.Tarih.Date;
            kayit.BaslangicKm = dto.BaslangicKm;
            kayit.BitisKm = dto.BitisKm;
            kayit.MesafeKm = dto.BitisKm - dto.BaslangicKm;
            kayit.Amac = dto.Amac;
            kayit.Nereden = Kirp(dto.Nereden, 150);
            kayit.Nereye = Kirp(dto.Nereye, 150);
            kayit.Not = Kirp(dto.Not, 500);

            await _yolculukDal.UpdateAsync(kayit);
            return new SuccessResult(Messages.YolculukGuncellendi);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var erisim = await KayitErisimiAsync(userId, id);
            if (erisim.Hata != null)
                return new ErrorResult(erisim.Hata);

            await _yolculukDal.DeleteAsync(erisim.Kayit);
            return new SuccessResult(Messages.YolculukSilindi);
        }

        private async Task<(YolculukKaydi Kayit, string Hata)> KayitErisimiAsync(int userId, int id)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return (null, Messages.UserNotFound);

            var kayit = await _yolculukDal.GetAsync(y => y.Id == id);
            if (kayit == null)
                return (null, Messages.YolculukBulunamadi);

            if (await _vehicleAccess.GetAccessibleAsync(userId, kayit.VehicleId) == null)
                return (null, Messages.YolculukBulunamadi);

            if (user.Role == CompanyRole.Driver && kayit.UserId != userId)
                return (null, Messages.AuthorizationDenied);

            return (kayit, null);
        }

        private async Task<(List<Vehicle> Araclar, string Hata)> KapsamAsync(int userId, int? vehicleId)
        {
            if (vehicleId == null)
            {
                return (await _vehicleAccess.GetAccessibleListAsync(userId), null);
            }

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId.Value);
            if (vehicle == null)
                return (null, Messages.VehicleNotFound);

            return (new List<Vehicle> { vehicle }, null);
        }

        private async Task<Dictionary<int, string>> KullanicilarAsync(IEnumerable<int> idler)
        {
            var benzersiz = idler.Distinct().ToList();
            if (benzersiz.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            var kullanicilar = await _userDal.GetListAsync(u => benzersiz.Contains(u.Id));
            return kullanicilar.ToDictionary(u => u.Id, u => u.FullName);
        }

        private async Task<bool> CakisiyorMuAsync(int vehicleId, int baslangicKm, int bitisKm, int? haricId)
        {
            var kayitlar = await _yolculukDal.GetListAsync(y => y.VehicleId == vehicleId
                                                              && y.BaslangicKm < bitisKm
                                                              && y.BitisKm > baslangicKm);

            return kayitlar.Any(y => haricId == null || y.Id != haricId.Value);
        }

        private static string Dogrula(DateTime tarih, int baslangicKm, int bitisKm, YolculukAmaci amac)
        {
            if (!Enum.IsDefined(amac))
                return Messages.InvalidValue;
            if (!DegerSinirlari.GecmisTarih(tarih))
                return Messages.InvalidValue;
            if (!DegerSinirlari.KmGecerli(baslangicKm) || !DegerSinirlari.KmGecerli(bitisKm) || bitisKm <= baslangicKm)
                return Messages.YolculukKmHatali;

            return null;
        }

        private static DateTime GunSonu(DateTime? bitis)
        {
            if (bitis == null)
            {
                return DateTime.MaxValue;
            }

            return bitis.Value.Date >= DateTime.MaxValue.Date ? DateTime.MaxValue : bitis.Value.Date.AddDays(1).AddTicks(-1);
        }

        private static string Kirp(string metin, int uzunluk)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return null;
            }

            var kirpik = metin.Trim();
            return kirpik.Length > uzunluk ? kirpik.Substring(0, uzunluk) : kirpik;
        }

        private static YolculukDto MapToDto(YolculukKaydi kayit, Dictionary<int, string> plakalar, Dictionary<int, string> kullanicilar)
        {
            return new YolculukDto
            {
                Id = kayit.Id,
                VehicleId = kayit.VehicleId,
                Plaka = plakalar.TryGetValue(kayit.VehicleId, out var plaka) ? plaka : null,
                UserId = kayit.UserId,
                SurucuAdi = kullanicilar.TryGetValue(kayit.UserId, out var ad) ? ad : null,
                Tarih = kayit.Tarih,
                BaslangicKm = kayit.BaslangicKm,
                BitisKm = kayit.BitisKm,
                MesafeKm = kayit.MesafeKm,
                Amac = kayit.Amac.ToString(),
                Nereden = kayit.Nereden,
                Nereye = kayit.Nereye,
                Not = kayit.Not
            };
        }
    }
}
