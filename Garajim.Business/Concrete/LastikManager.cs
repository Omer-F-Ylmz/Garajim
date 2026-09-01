using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class LastikManager : ILastikService
    {
        private readonly ILastikDal _lastikDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly EvrakKurallari _evrakKurallari;

        public LastikManager(ILastikDal lastikDal, IUserDal userDal, IVehicleAccessService vehicleAccess, EvrakKurallari evrakKurallari)
        {
            _lastikDal = lastikDal;
            _userDal = userDal;
            _vehicleAccess = vehicleAccess;
            _evrakKurallari = evrakKurallari;
        }

        public async Task<IDataResult<LastikDurumDto>> GetDurumAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<LastikDurumDto>(Messages.VehicleNotFound);

            var setler = await _lastikDal.GetListeAsync(vehicleId);
            var takili = setler.FirstOrDefault(s => s.Takili);
            var kisDonemi = _evrakKurallari.KisLastigiDonemindeMi(DateTime.UtcNow.Date);

            var durum = new LastikDurumDto
            {
                KisLastigiDonemi = kisDonemi,
                TakiliSet = takili == null ? null : MapToDto(takili),
                Setler = setler.Select(MapToDto).ToList(),
                Uyari = Uyari(takili, kisDonemi)
            };

            return new SuccessDataResult<LastikDurumDto>(durum);
        }

        public async Task<IDataResult<LastikDto>> TakAsync(int userId, LastikTakDto dto)
        {
            var yetki = await YetkiHatasiAsync(userId);
            if (yetki != null)
                return new ErrorDataResult<LastikDto>(yetki);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<LastikDto>(Messages.VehicleNotFound);

            if (string.IsNullOrWhiteSpace(dto.Ad) || !Enum.IsDefined(dto.Mevsim) ||
                dto.TakilmaKm < 0 || dto.TakilmaTarihi == default ||
                dto.TakilmaTarihi.Date > DateTime.UtcNow.Date.AddDays(1) ||
                (dto.DisDerinligiMm != null && (dto.DisDerinligiMm < 0 || dto.DisDerinligiMm > 30)))
                return new ErrorDataResult<LastikDto>(Messages.InvalidValue);

            var mevcut = await _lastikDal.GetTakiliAsync(dto.VehicleId);
            if (mevcut != null)
            {
                if (dto.TakilmaKm < mevcut.TakilmaKm)
                    return new ErrorDataResult<LastikDto>(Messages.LastikKmHatali);

                await SokAsync(mevcut, dto.TakilmaTarihi.Date, dto.TakilmaKm, null);
            }

            var set = new LastikSeti
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                Ad = dto.Ad.Trim(),
                Mevsim = dto.Mevsim,
                Marka = Kirp(dto.Marka, 100),
                Ebat = Kirp(dto.Ebat, 50),
                DisDerinligiMm = dto.DisDerinligiMm,
                TakilmaTarihi = dto.TakilmaTarihi.Date,
                TakilmaKm = dto.TakilmaKm,
                ToplamKm = 0,
                Takili = true,
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _lastikDal.AddAsync(set);
            return new SuccessDataResult<LastikDto>(MapToDto(set), Messages.LastikTakildi);
        }

        public async Task<IResult> SokAsync(int userId, int id, LastikSokDto dto)
        {
            var yetki = await YetkiHatasiAsync(userId);
            if (yetki != null)
                return new ErrorResult(yetki);

            var set = await _lastikDal.GetAsync(l => l.Id == id);
            if (set == null)
                return new ErrorResult(Messages.LastikBulunamadi);

            if (await _vehicleAccess.GetAccessibleAsync(userId, set.VehicleId) == null)
                return new ErrorResult(Messages.LastikBulunamadi);

            if (!set.Takili)
                return new ErrorResult(Messages.LastikZatenSokulmus);

            if (dto.SokulmeKm < set.TakilmaKm || dto.SokulmeTarihi == default ||
                dto.SokulmeTarihi.Date < set.TakilmaTarihi.Date ||
                dto.SokulmeTarihi.Date > DateTime.UtcNow.Date.AddDays(1))
                return new ErrorResult(Messages.LastikKmHatali);

            if (dto.DisDerinligiMm != null && (dto.DisDerinligiMm < 0 || dto.DisDerinligiMm > 30))
                return new ErrorResult(Messages.InvalidValue);

            await SokAsync(set, dto.SokulmeTarihi.Date, dto.SokulmeKm, dto.DisDerinligiMm);
            return new SuccessResult(Messages.LastikSokuldu);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var yetki = await YetkiHatasiAsync(userId);
            if (yetki != null)
                return new ErrorResult(yetki);

            var set = await _lastikDal.GetAsync(l => l.Id == id);
            if (set == null)
                return new ErrorResult(Messages.LastikBulunamadi);

            if (await _vehicleAccess.GetAccessibleAsync(userId, set.VehicleId) == null)
                return new ErrorResult(Messages.LastikBulunamadi);

            await _lastikDal.DeleteAsync(set);
            return new SuccessResult(Messages.LastikSilindi);
        }

        private async Task SokAsync(LastikSeti set, DateTime tarih, int km, decimal? disDerinligi)
        {
            set.SokulmeTarihi = tarih;
            set.SokulmeKm = km;
            set.ToplamKm = km - set.TakilmaKm;
            set.Takili = false;

            if (disDerinligi != null)
            {
                set.DisDerinligiMm = disDerinligi;
            }

            await _lastikDal.UpdateAsync(set);
        }

        private static string Uyari(LastikSeti takili, bool kisDonemi)
        {
            if (takili == null)
            {
                return Messages.LastikSetiYok;
            }

            if (kisDonemi && takili.Mevsim == LastikMevsimi.Yaz)
            {
                return Messages.KisLastigiUyarisi;
            }

            if (takili.DisDerinligiMm != null && takili.DisDerinligiMm <= 1.6m)
            {
                return Messages.LastikDisDerinligiUyarisi;
            }

            return null;
        }

        private async Task<string> YetkiHatasiAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
            {
                return Messages.UserNotFound;
            }

            return user.Role == CompanyRole.Driver ? Messages.AuthorizationDenied : null;
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

        private static LastikDto MapToDto(LastikSeti set)
        {
            return new LastikDto
            {
                Id = set.Id,
                VehicleId = set.VehicleId,
                Ad = set.Ad,
                Mevsim = set.Mevsim.ToString(),
                Marka = set.Marka,
                Ebat = set.Ebat,
                DisDerinligiMm = set.DisDerinligiMm,
                TakilmaTarihi = set.TakilmaTarihi,
                TakilmaKm = set.TakilmaKm,
                SokulmeTarihi = set.SokulmeTarihi,
                SokulmeKm = set.SokulmeKm,
                ToplamKm = set.ToplamKm,
                Takili = set.Takili
            };
        }
    }
}
