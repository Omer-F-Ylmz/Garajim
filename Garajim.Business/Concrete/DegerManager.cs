using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class DegerManager : IDegerService
    {
        public const int GunlukTahminHakki = 3;

        private readonly IAracDegerDal _degerDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IDegerTahminEdici _tahminEdici;

        public DegerManager(IAracDegerDal degerDal, IVehicleAccessService vehicleAccess, IDegerTahminEdici tahminEdici)
        {
            _degerDal = degerDal;
            _vehicleAccess = vehicleAccess;
            _tahminEdici = tahminEdici;
        }

        public async Task<IDataResult<DegerSerisiDto>> GetSeriAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<DegerSerisiDto>(Messages.VehicleNotFound);

            var kayitlar = await _degerDal.GetSeriAsync(vehicleId, QueryLimits.MaxListSize);

            var seri = new DegerSerisiDto
            {
                VehicleId = vehicleId,
                Plaka = vehicle.Plate,
                Kayitlar = kayitlar.Select(MapToDto).ToList()
            };

            seri.SonDeger = seri.Kayitlar.FirstOrDefault();

            if (kayitlar.Count > 1)
            {
                var enEski = kayitlar[kayitlar.Count - 1];
                seri.DegerKaybi = enEski.Deger - kayitlar[0].Deger;
            }

            return new SuccessDataResult<DegerSerisiDto>(seri);
        }

        public async Task<IDataResult<AracDegerDto>> GirAsync(int userId, int vehicleId, DegerGirDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<AracDegerDto>(Messages.VehicleNotFound);

            if (!Enum.IsDefined(dto.Kaynak) || dto.Kaynak == DegerKaynagi.Tahmin)
                return new ErrorDataResult<AracDegerDto>(Messages.InvalidValue);

            if (dto.Deger <= 0)
                return new ErrorDataResult<AracDegerDto>(Messages.DegerGecersiz);

            if (dto.Tarih == default || dto.Tarih.Date > DateTime.UtcNow.Date || dto.Tarih.Year < 1950)
                return new ErrorDataResult<AracDegerDto>(Messages.DegerTarihiGecersiz);

            var kayit = await EkleAsync(vehicle, dto.Tarih.Date, dto.Deger, dto.Kaynak, dto.Not);

            return new SuccessDataResult<AracDegerDto>(MapToDto(kayit), Messages.DegerKaydedildi);
        }

        public async Task<IDataResult<DegerTahminSonucuDto>> TahminAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<DegerTahminSonucuDto>(Messages.VehicleNotFound);

            var bugun = DateTime.UtcNow.Date;
            var alinan = await _degerDal.GunlukTahminSayisiAsync(vehicleId, bugun);
            if (alinan >= GunlukTahminHakki)
                return new ErrorDataResult<DegerTahminSonucuDto>(Messages.DegerTahminSiniri);

            if (vehicle.KasaTipi == null)
            {
                return new ErrorDataResult<DegerTahminSonucuDto>(Messages.DegerKasaTipiGerekli);
            }

            if (string.IsNullOrWhiteSpace(vehicle.Vites))
            {
                return new ErrorDataResult<DegerTahminSonucuDto>(Messages.DegerVitesGerekli);
            }

            var sonuc = _tahminEdici.Tahmin(
                vehicle.Brand,
                vehicle.Model,
                vehicle.Year,
                vehicle.CurrentKm,
                vehicle.FuelType.ToString(),
                vehicle.Vites,
                KasaTipiAdlari.ModelDegeri(vehicle.KasaTipi.Value));

            if (sonuc == null || sonuc.KapsamDisi || sonuc.Fiyat == null || sonuc.Fiyat <= 0)
            {
                return new ErrorDataResult<DegerTahminSonucuDto>(Messages.DegerModelKapsamDisi);
            }

            var kayit = await EkleAsync(vehicle, bugun, sonuc.Fiyat.Value, DegerKaynagi.Tahmin, Messages.DegerTahminUyarisi);

            return new SuccessDataResult<DegerTahminSonucuDto>(new DegerTahminSonucuDto
            {
                Kayit = MapToDto(kayit),
                Uyari = Messages.DegerTahminUyarisi,
                KalanHak = GunlukTahminHakki - alinan - 1
            }, Messages.DegerTahminAlindi);
        }

        public async Task<decimal> FiloToplamDegerAsync(List<int> vehicleIds)
        {
            return vehicleIds.Count == 0 ? 0m : await _degerDal.FiloToplamSonDegerAsync(vehicleIds);
        }

        public async Task<AracDegerDto> KarneDegeriAsync(int vehicleId)
        {
            var kayitlar = await _degerDal.GetSeriAsync(vehicleId, QueryLimits.MaxListSize);

            var beyan = kayitlar.FirstOrDefault(d => d.Kaynak == DegerKaynagi.Beyan || d.Kaynak == DegerKaynagi.Ekspertiz);

            return beyan == null ? null : MapToDto(beyan);
        }

        public async Task<decimal?> DonemDegerKaybiAsync(int vehicleId, DateTime baslangic, DateTime bitis)
        {
            var ilk = await _degerDal.AraliktakiIlkAsync(vehicleId, baslangic, bitis);
            var son = await _degerDal.AraliktakiSonAsync(vehicleId, baslangic, bitis);

            if (ilk == null || son == null || ilk.Id == son.Id)
            {
                return null;
            }

            return ilk.Deger - son.Deger;
        }

        private async Task<AracDeger> EkleAsync(Vehicle vehicle, DateTime tarih, decimal deger, DegerKaynagi kaynak, string not)
        {
            var kayit = new AracDeger
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                Tarih = tarih,
                Deger = decimal.Round(deger, 2),
                Kaynak = kaynak,
                Not = Kirp(not, 200),
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _degerDal.AddAsync(kayit);
            return kayit;
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

        private static AracDegerDto MapToDto(AracDeger kayit)
        {
            return new AracDegerDto
            {
                Id = kayit.Id,
                VehicleId = kayit.VehicleId,
                Tarih = kayit.Tarih,
                Deger = kayit.Deger,
                Kaynak = kayit.Kaynak.ToString(),
                KaynakAdi = DegerAdlari.Kaynak(kayit.Kaynak),
                Not = kayit.Not
            };
        }
    }
}
