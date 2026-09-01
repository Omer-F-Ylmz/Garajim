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
    public class EvrakManager : IEvrakService
    {
        private readonly IEvrakDal _evrakDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly EvrakKurallari _kurallar;

        public EvrakManager(
            IEvrakDal evrakDal,
            IUserDal userDal,
            IVehicleDal vehicleDal,
            IVehicleAccessService vehicleAccess,
            EvrakKurallari kurallar)
        {
            _evrakDal = evrakDal;
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _vehicleAccess = vehicleAccess;
            _kurallar = kurallar;
        }

        public async Task<IDataResult<List<EvrakDto>>> GetListAsync(int userId, int? vehicleId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<List<EvrakDto>>(Messages.UserNotFound);

            if (vehicleId != null && await _vehicleAccess.GetAccessibleAsync(userId, vehicleId.Value) == null)
                return new ErrorDataResult<List<EvrakDto>>(Messages.VehicleNotFound);

            var kayitlar = await _evrakDal.GetListAsync(e => vehicleId == null || e.VehicleId == vehicleId);
            return new SuccessDataResult<List<EvrakDto>>(await ErisilebilirleriHazirlaAsync(user, kayitlar));
        }

        public async Task<IDataResult<List<EvrakDto>>> GetTakvimAsync(int userId, string ay)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<List<EvrakDto>>(Messages.UserNotFound);

            if (!AyCoz(ay, out var baslangic))
                return new ErrorDataResult<List<EvrakDto>>(Messages.InvalidValue);

            var bitis = baslangic.AddMonths(1);
            var kayitlar = await _evrakDal.GetListAsync(e => e.BitisTarihi >= baslangic && e.BitisTarihi < bitis);

            return new SuccessDataResult<List<EvrakDto>>(await ErisilebilirleriHazirlaAsync(user, kayitlar));
        }

        public async Task<IDataResult<EvrakDto>> GetByIdAsync(int userId, int id)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<EvrakDto>(Messages.UserNotFound);

            var kayit = await _evrakDal.GetAsync(e => e.Id == id);
            if (kayit == null || !await ErisebilirMiAsync(user, kayit))
                return new ErrorDataResult<EvrakDto>(Messages.EvrakNotFound);

            return new SuccessDataResult<EvrakDto>(await MapAsync(kayit));
        }

        public async Task<IDataResult<EvrakDto>> AddAsync(int userId, EvrakCreateDto dto)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<EvrakDto>(Messages.UserNotFound);

            if (!Enum.IsDefined(dto.EvrakTuru))
                return new ErrorDataResult<EvrakDto>(Messages.InvalidValue);

            if ((dto.VehicleId == null) == (dto.UserId == null))
                return new ErrorDataResult<EvrakDto>(Messages.EvrakSahibiTekOlmali);

            if (dto.UserId != null && user.Role == CompanyRole.Driver)
                return new ErrorDataResult<EvrakDto>(Messages.AuthorizationDenied);

            Vehicle vehicle = null;
            if (dto.VehicleId != null)
            {
                if (user.Role == CompanyRole.Driver)
                    return new ErrorDataResult<EvrakDto>(Messages.AuthorizationDenied);

                vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId.Value);
                if (vehicle == null)
                    return new ErrorDataResult<EvrakDto>(Messages.VehicleNotFound);
            }

            if (dto.UserId != null && await _userDal.GetAsync(u => u.Id == dto.UserId) == null)
                return new ErrorDataResult<EvrakDto>(Messages.UserNotFound);

            var bitis = dto.BitisTarihi ?? _kurallar.SonrakiTarih(
                dto.EvrakTuru,
                vehicle?.KullanimTuru ?? KullanimTuru.Hususi,
                dto.BaslangicTarihi,
                vehicle?.IlkTescilTarihi);

            var kayit = new EvrakKaydi
            {
                CompanyId = user.CompanyId,
                VehicleId = dto.VehicleId,
                UserId = dto.UserId,
                EvrakTuru = dto.EvrakTuru,
                BaslangicTarihi = dto.BaslangicTarihi?.Date,
                BitisTarihi = bitis.Date,
                Saglayici = dto.Saglayici,
                PoliceNo = dto.PoliceNo,
                Not = dto.Not,
                DocumentId = dto.DocumentId,
                Aktif = true,
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _evrakDal.AddAsync(kayit);
            return new SuccessDataResult<EvrakDto>(await MapAsync(kayit), Messages.EvrakAdded);
        }

        public async Task<IDataResult<EvrakDto>> UpdateAsync(int userId, int id, EvrakUpdateDto dto)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<EvrakDto>(Messages.UserNotFound);

            if (user.Role == CompanyRole.Driver)
                return new ErrorDataResult<EvrakDto>(Messages.AuthorizationDenied);

            if (!Enum.IsDefined(dto.EvrakTuru))
                return new ErrorDataResult<EvrakDto>(Messages.InvalidValue);

            var kayit = await _evrakDal.GetAsync(e => e.Id == id);
            if (kayit == null || !await ErisebilirMiAsync(user, kayit))
                return new ErrorDataResult<EvrakDto>(Messages.EvrakNotFound);

            kayit.EvrakTuru = dto.EvrakTuru;
            kayit.BaslangicTarihi = dto.BaslangicTarihi?.Date;
            kayit.BitisTarihi = dto.BitisTarihi.Date;
            kayit.Saglayici = dto.Saglayici;
            kayit.PoliceNo = dto.PoliceNo;
            kayit.Not = dto.Not;
            kayit.DocumentId = dto.DocumentId;

            await _evrakDal.UpdateAsync(kayit);
            return new SuccessDataResult<EvrakDto>(await MapAsync(kayit), Messages.EvrakUpdated);
        }

        public async Task<IDataResult<EvrakDto>> YenileAsync(int userId, int id)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<EvrakDto>(Messages.UserNotFound);

            if (user.Role == CompanyRole.Driver)
                return new ErrorDataResult<EvrakDto>(Messages.AuthorizationDenied);

            var eski = await _evrakDal.GetAsync(e => e.Id == id);
            if (eski == null || !await ErisebilirMiAsync(user, eski))
                return new ErrorDataResult<EvrakDto>(Messages.EvrakNotFound);

            var vehicle = eski.VehicleId == null ? null : await _vehicleDal.GetAsync(v => v.Id == eski.VehicleId);

            var yeniBitis = _kurallar.SonrakiTarih(
                eski.EvrakTuru,
                vehicle?.KullanimTuru ?? KullanimTuru.Hususi,
                eski.BitisTarihi,
                vehicle?.IlkTescilTarihi);

            await _evrakDal.PasiflestirAsync(eski.Id);

            var yeni = new EvrakKaydi
            {
                CompanyId = eski.CompanyId,
                VehicleId = eski.VehicleId,
                UserId = eski.UserId,
                EvrakTuru = eski.EvrakTuru,
                BaslangicTarihi = eski.BitisTarihi,
                BitisTarihi = yeniBitis.Date,
                Saglayici = eski.Saglayici,
                PoliceNo = null,
                Not = eski.Not,
                Aktif = true,
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _evrakDal.AddAsync(yeni);
            return new SuccessDataResult<EvrakDto>(await MapAsync(yeni), Messages.EvrakRenewed);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            if (user.Role == CompanyRole.Driver)
                return new ErrorResult(Messages.AuthorizationDenied);

            var kayit = await _evrakDal.GetAsync(e => e.Id == id);
            if (kayit == null || !await ErisebilirMiAsync(user, kayit))
                return new ErrorResult(Messages.EvrakNotFound);

            await _evrakDal.DeleteAsync(kayit);
            return new SuccessResult(Messages.EvrakDeleted);
        }

        private async Task<List<EvrakDto>> ErisilebilirleriHazirlaAsync(AppUser user, List<EvrakKaydi> kayitlar)
        {
            var liste = new List<EvrakDto>();

            foreach (var kayit in kayitlar.OrderBy(k => k.BitisTarihi))
            {
                if (await ErisebilirMiAsync(user, kayit))
                {
                    liste.Add(await MapAsync(kayit));
                }
            }

            return liste;
        }

        private async Task<bool> ErisebilirMiAsync(AppUser user, EvrakKaydi kayit)
        {
            if (user.Role != CompanyRole.Driver)
            {
                return true;
            }

            if (kayit.UserId != null)
            {
                return kayit.UserId == user.Id;
            }

            return kayit.VehicleId != null
                   && await _vehicleAccess.GetAccessibleAsync(user.Id, kayit.VehicleId.Value) != null;
        }

        private async Task<EvrakDto> MapAsync(EvrakKaydi kayit)
        {
            var vehicle = kayit.VehicleId == null ? null : await _vehicleDal.GetAsync(v => v.Id == kayit.VehicleId);
            var kullanici = kayit.UserId == null ? null : await _userDal.GetAsync(u => u.Id == kayit.UserId);
            var bugun = DateTime.UtcNow.Date;

            return new EvrakDto
            {
                Id = kayit.Id,
                VehicleId = kayit.VehicleId,
                Plaka = vehicle?.Plate,
                UserId = kayit.UserId,
                KullaniciAdi = kullanici?.FullName,
                EvrakTuru = kayit.EvrakTuru.ToString(),
                EvrakAdi = EvrakAdlari.Ad(kayit.EvrakTuru),
                BaslangicTarihi = kayit.BaslangicTarihi,
                BitisTarihi = kayit.BitisTarihi,
                Saglayici = kayit.Saglayici,
                PoliceNo = kayit.PoliceNo,
                Not = kayit.Not,
                DocumentId = kayit.DocumentId,
                Aktif = kayit.Aktif,
                Durum = EvrakKurallari.Durum(kayit.BitisTarihi, bugun),
                KalanGun = (int)(kayit.BitisTarihi.Date - bugun).TotalDays
            };
        }

        private static bool AyCoz(string ay, out DateTime baslangic)
        {
            baslangic = default;

            if (string.IsNullOrWhiteSpace(ay))
            {
                baslangic = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                return true;
            }

            return DateTime.TryParseExact(ay + "-01", "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out baslangic);
        }
    }
}
