using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class HasarManager : IHasarService
    {
        public const int MaxFoto = 20;

        private readonly IHasarDosyasiDal _dosyaDal;
        private readonly IHasarFotoDal _fotoDal;
        private readonly IDocumentService _documentService;
        private readonly IDocumentDal _documentDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IUnitOfWork _unitOfWork;

        public HasarManager(
            IHasarDosyasiDal dosyaDal,
            IHasarFotoDal fotoDal,
            IDocumentService documentService,
            IDocumentDal documentDal,
            IUserDal userDal,
            IVehicleAccessService vehicleAccess,
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _dosyaDal = dosyaDal;
            _fotoDal = fotoDal;
            _documentService = documentService;
            _documentDal = documentDal;
            _userDal = userDal;
            _vehicleAccess = vehicleAccess;
        }

        public async Task<IDataResult<List<HasarDto>>> GetListAsync(int userId, int? vehicleId)
        {
            List<Vehicle> araclar;

            if (vehicleId != null)
            {
                var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId.Value);
                if (vehicle == null)
                    return new ErrorDataResult<List<HasarDto>>(Messages.VehicleNotFound);
                araclar = new List<Vehicle> { vehicle };
            }
            else
            {
                araclar = await _vehicleAccess.GetAccessibleListAsync(userId);
            }

            if (araclar.Count == 0)
                return new SuccessDataResult<List<HasarDto>>(new List<HasarDto>());

            var plakalar = araclar.ToDictionary(a => a.Id, a => a.Plate);
            var dosyalar = await _dosyaDal.GetListeAsync(araclar.Select(a => a.Id).ToList(), QueryLimits.MaxListSize);

            var liste = new List<HasarDto>();
            foreach (var dosya in dosyalar)
            {
                var dto = MapToDto(dosya, plakalar);
                dto.FotoSayisi = await _fotoDal.SayiAsync(dosya.Id);
                liste.Add(dto);
            }

            return new SuccessDataResult<List<HasarDto>>(liste);
        }

        public async Task<IDataResult<HasarDto>> GetAsync(int userId, int id)
        {
            var erisim = await ErisimAsync(userId, id);
            if (erisim.Hata != null)
                return new ErrorDataResult<HasarDto>(erisim.Hata);

            var plakalar = new Dictionary<int, string> { [erisim.Arac.Id] = erisim.Arac.Plate };
            var dto = MapToDto(erisim.Dosya, plakalar);
            dto.Fotograflar = await FotolariAl(erisim.Dosya.Id);
            dto.FotoSayisi = dto.Fotograflar.Count;

            return new SuccessDataResult<HasarDto>(dto);
        }

        public async Task<IDataResult<HasarDto>> OlusturAsync(int userId, HasarOlusturDto dto)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<HasarDto>(Messages.UserNotFound);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<HasarDto>(Messages.VehicleNotFound);

            var hata = Dogrula(dto.OlayTarihi, dto.Tur, dto.TutanakTuru, dto.Aciklama, dto.OlayKm, dto.HasarBedeli);
            if (hata != null)
                return new ErrorDataResult<HasarDto>(hata);

            var dosya = new HasarDosyasi
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                OlayTarihi = dto.OlayTarihi.Date,
                Tur = dto.Tur,
                Konum = Kirp(dto.Konum, 200),
                Aciklama = Kirp(dto.Aciklama, 1000),
                OlayKm = dto.OlayKm,
                TutanakTuru = dto.TutanakTuru,
                KarsiTarafPlaka = Kirp(dto.KarsiTarafPlaka, 15),
                KarsiTarafSigorta = Kirp(dto.KarsiTarafSigorta, 100),
                KarsiTarafPoliceNo = Kirp(dto.KarsiTarafPoliceNo, 50),
                SigortaDosyaNo = Kirp(dto.SigortaDosyaNo, 50),
                HasarBedeli = dto.HasarBedeli,
                Durum = HasarDurumu.Acik,
                OlusturanUserId = userId,
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _dosyaDal.AddAsync(dosya);

            var plakalar = new Dictionary<int, string> { [vehicle.Id] = vehicle.Plate };
            return new SuccessDataResult<HasarDto>(MapToDto(dosya, plakalar), Messages.HasarDosyasiAcildi);
        }

        public async Task<IResult> GuncelleAsync(int userId, int id, HasarGuncelleDto dto)
        {
            var yetki = await YoneticiMiAsync(userId);
            if (yetki != null)
                return new ErrorResult(yetki);

            var erisim = await ErisimAsync(userId, id);
            if (erisim.Hata != null)
                return new ErrorResult(erisim.Hata);

            if (!Enum.IsDefined(dto.Durum))
                return new ErrorResult(Messages.InvalidValue);

            var hata = Dogrula(dto.OlayTarihi, dto.Tur, dto.TutanakTuru, dto.Aciklama, dto.OlayKm, dto.HasarBedeli);
            if (hata != null)
                return new ErrorResult(hata);

            var dosya = erisim.Dosya;
            dosya.OlayTarihi = dto.OlayTarihi.Date;
            dosya.Tur = dto.Tur;
            dosya.Konum = Kirp(dto.Konum, 200);
            dosya.Aciklama = Kirp(dto.Aciklama, 1000);
            dosya.OlayKm = dto.OlayKm;
            dosya.TutanakTuru = dto.TutanakTuru;
            dosya.KarsiTarafPlaka = Kirp(dto.KarsiTarafPlaka, 15);
            dosya.KarsiTarafSigorta = Kirp(dto.KarsiTarafSigorta, 100);
            dosya.KarsiTarafPoliceNo = Kirp(dto.KarsiTarafPoliceNo, 50);
            dosya.SigortaDosyaNo = Kirp(dto.SigortaDosyaNo, 50);
            dosya.HasarBedeli = dto.HasarBedeli;
            dosya.Durum = dto.Durum;

            await _dosyaDal.UpdateAsync(dosya);
            return new SuccessResult(Messages.HasarDosyasiGuncellendi);
        }

        public async Task<IResult> SilAsync(int userId, int id)
        {
            var yetki = await YoneticiMiAsync(userId);
            if (yetki != null)
                return new ErrorResult(yetki);

            var erisim = await ErisimAsync(userId, id);
            if (erisim.Hata != null)
                return new ErrorResult(erisim.Hata);

            var fotolar = await _fotoDal.GetByDosyaAsync(id);
            var silinecekDosyalar = new List<string>();

            await using (var islem = await _unitOfWork.BeginTransactionAsync())
            {
                foreach (var foto in fotolar)
                {
                    await _fotoDal.DeleteAsync(foto);

                    var satir = await _documentService.SatirSilAsync(userId, foto.DocumentId);
                    if (!satir.Success)
                        return new ErrorResult(satir.Message);

                    silinecekDosyalar.Add(satir.Data);
                }

                await _dosyaDal.DeleteAsync(erisim.Dosya);
                await _unitOfWork.CommitAsync();
            }

            foreach (var saklananAd in silinecekDosyalar)
            {
                _documentService.DosyaSil(saklananAd);
            }

            return new SuccessResult(Messages.HasarDosyasiSilindi);
        }

        public async Task<IDataResult<HasarFotoDto>> FotoEkleAsync(int userId, int id, HasarFotoEtiketi etiket, string dosyaAdi, byte[] icerik)
        {
            var erisim = await ErisimAsync(userId, id);
            if (erisim.Hata != null)
                return new ErrorDataResult<HasarFotoDto>(erisim.Hata);

            if (!Enum.IsDefined(etiket))
                return new ErrorDataResult<HasarFotoDto>(Messages.InvalidValue);

            var mevcut = await _fotoDal.SayiAsync(id);
            if (mevcut >= MaxFoto)
                return new ErrorDataResult<HasarFotoDto>(Messages.HasarFotoSiniri);

            var yukleme = await _documentService.UploadAsync(userId, new DocumentUploadDto
            {
                VehicleId = erisim.Dosya.VehicleId,
                FileName = dosyaAdi,
                Content = icerik
            });

            if (!yukleme.Success)
                return new ErrorDataResult<HasarFotoDto>(yukleme.Message);

            var sira = await _fotoDal.SonSiraAsync(id) + 1;
            var foto = new HasarFoto
            {
                CompanyId = erisim.Dosya.CompanyId,
                HasarDosyasiId = id,
                DocumentId = yukleme.Data.Id,
                Etiket = etiket,
                Sira = sira,
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _fotoDal.AddAsync(foto);

            return new SuccessDataResult<HasarFotoDto>(new HasarFotoDto
            {
                Id = foto.Id,
                DocumentId = foto.DocumentId,
                Etiket = foto.Etiket.ToString(),
                EtiketAdi = HasarAdlari.Etiket(foto.Etiket),
                Sira = foto.Sira,
                DosyaAdi = yukleme.Data.OriginalName
            }, Messages.HasarFotoEklendi);
        }

        public async Task<IResult> FotoSilAsync(int userId, int id, int fotoId)
        {
            var erisim = await ErisimAsync(userId, id);
            if (erisim.Hata != null)
                return new ErrorResult(erisim.Hata);

            var foto = await _fotoDal.GetAsync(f => f.Id == fotoId && f.HasarDosyasiId == id);
            if (foto == null)
                return new ErrorResult(Messages.HasarFotoBulunamadi);

            string saklananAd;

            await using (var islem = await _unitOfWork.BeginTransactionAsync())
            {
                await _fotoDal.DeleteAsync(foto);

                var satir = await _documentService.SatirSilAsync(userId, foto.DocumentId);
                if (!satir.Success)
                    return new ErrorResult(satir.Message);

                saklananAd = satir.Data;
                await _unitOfWork.CommitAsync();
            }

            _documentService.DosyaSil(saklananAd);

            return new SuccessResult(Messages.HasarFotoSilindi);
        }

        public async Task<int> AcikDosyaSayisiAsync(List<int> vehicleIds)
        {
            return vehicleIds.Count == 0 ? 0 : await _dosyaDal.AcikSayisiAsync(vehicleIds);
        }

        public async Task<List<HasarKarneSatiriDto>> KarneSatirlariAsync(int vehicleId)
        {
            var dosyalar = await _dosyaDal.GetListeAsync(new List<int> { vehicleId }, QueryLimits.MaxListSize);

            return dosyalar
                .Select(d => new HasarKarneSatiriDto
                {
                    OlayTarihi = d.OlayTarihi,
                    Tur = HasarAdlari.Tur(d.Tur),
                    Durum = d.Durum == HasarDurumu.Kapandi ? "Onarıldı" : "Açık"
                })
                .ToList();
        }

        private async Task<List<HasarFotoDto>> FotolariAl(int hasarDosyasiId)
        {
            var fotolar = await _fotoDal.GetByDosyaAsync(hasarDosyasiId);
            if (fotolar.Count == 0)
            {
                return new List<HasarFotoDto>();
            }

            var belgeIdler = fotolar.Select(f => f.DocumentId).ToList();
            var belgeler = (await _documentDal.GetListAsync(d => belgeIdler.Contains(d.Id)))
                .ToDictionary(d => d.Id, d => d.OriginalName);

            return fotolar.Select(f => new HasarFotoDto
            {
                Id = f.Id,
                DocumentId = f.DocumentId,
                Etiket = f.Etiket.ToString(),
                EtiketAdi = HasarAdlari.Etiket(f.Etiket),
                Sira = f.Sira,
                DosyaAdi = belgeler.TryGetValue(f.DocumentId, out var ad) ? ad : null
            }).ToList();
        }

        private async Task<(HasarDosyasi Dosya, Vehicle Arac, string Hata)> ErisimAsync(int userId, int id)
        {
            var dosya = await _dosyaDal.GetAsync(h => h.Id == id);
            if (dosya == null)
                return (null, null, Messages.HasarDosyasiBulunamadi);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dosya.VehicleId);
            if (vehicle == null)
                return (null, null, Messages.HasarDosyasiBulunamadi);

            return (dosya, vehicle, null);
        }

        private async Task<string> YoneticiMiAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
            {
                return Messages.UserNotFound;
            }

            return user.Role == CompanyRole.Driver ? Messages.AuthorizationDenied : null;
        }

        private static string Dogrula(DateTime olayTarihi, HasarTuru tur, TutanakTuru tutanak, string aciklama, int? olayKm, decimal? bedel)
        {
            if (!Enum.IsDefined(tur) || !Enum.IsDefined(tutanak))
                return Messages.InvalidValue;

            if (string.IsNullOrWhiteSpace(aciklama))
                return Messages.InvalidValue;

            if (olayTarihi == default || olayTarihi.Date > TarihToleransi.EnGecGun() || olayTarihi.Year < 1950)
                return Messages.HasarTarihiGecersiz;

            if (olayKm != null && olayKm < 0)
                return Messages.InvalidValue;

            if (bedel != null && bedel < 0)
                return Messages.InvalidValue;

            return null;
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

        private static HasarDto MapToDto(HasarDosyasi dosya, Dictionary<int, string> plakalar)
        {
            return new HasarDto
            {
                Id = dosya.Id,
                VehicleId = dosya.VehicleId,
                Plaka = plakalar.TryGetValue(dosya.VehicleId, out var plaka) ? plaka : null,
                OlayTarihi = dosya.OlayTarihi,
                Tur = dosya.Tur.ToString(),
                TurAdi = HasarAdlari.Tur(dosya.Tur),
                Konum = dosya.Konum,
                Aciklama = dosya.Aciklama,
                OlayKm = dosya.OlayKm,
                TutanakTuru = dosya.TutanakTuru.ToString(),
                TutanakTuruAdi = HasarAdlari.Tutanak(dosya.TutanakTuru),
                KarsiTarafPlaka = dosya.KarsiTarafPlaka,
                KarsiTarafSigorta = dosya.KarsiTarafSigorta,
                KarsiTarafPoliceNo = dosya.KarsiTarafPoliceNo,
                SigortaDosyaNo = dosya.SigortaDosyaNo,
                HasarBedeli = dosya.HasarBedeli,
                Durum = dosya.Durum.ToString(),
                DurumAdi = HasarAdlari.Durum(dosya.Durum),
                OlusturmaTarihi = dosya.OlusturmaTarihi
            };
        }
    }
}
