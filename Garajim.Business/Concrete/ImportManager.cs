using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Import;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class ImportManager : IImportService
    {
        public const long MaxBoyut = 5 * 1024 * 1024;
        public const int MaxSatir = CsvOkuyucu.MaxSatir;

        private static readonly string[] KayitTurleri = { "Yakit", "Bakim", "Masraf" };

        private readonly IImportKaydiDal _importDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IFuelDal _fuelDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IExpenseDal _expenseDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IUnitOfWork _unitOfWork;

        public ImportManager(
            IImportKaydiDal importDal,
            IUserDal userDal,
            IVehicleDal vehicleDal,
            IFuelDal fuelDal,
            IMaintenanceDal maintenanceDal,
            IExpenseDal expenseDal,
            IVehicleAccessService vehicleAccess,
            IUnitOfWork unitOfWork)
        {
            _importDal = importDal;
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _fuelDal = fuelDal;
            _maintenanceDal = maintenanceDal;
            _expenseDal = expenseDal;
            _vehicleAccess = vehicleAccess;
            _unitOfWork = unitOfWork;
        }

        public async Task<IDataResult<ImportOnizlemeDto>> OnizleAsync(int userId, byte[] icerik, string kayitTuru)
        {
            var yetki = await YetkiHatasiAsync(userId);
            if (yetki != null)
                return new ErrorDataResult<ImportOnizlemeDto>(yetki);

            var boyutHatasi = BoyutHatasi(icerik);
            if (boyutHatasi != null)
                return new ErrorDataResult<ImportOnizlemeDto>(boyutHatasi);

            var tur = KayitTuruNormalize(kayitTuru);
            var tablo = CsvOkuyucu.Oku(icerik, tur);

            if (tablo.Basliklar.Count < 2)
                return new ErrorDataResult<ImportOnizlemeDto>(Messages.ImportBozukDosya);

            if (tablo.SinirAsildi || tablo.Satirlar.Count > MaxSatir)
                return new ErrorDataResult<ImportOnizlemeDto>(Messages.ImportCokFazlaSatir);

            var sablon = ImportSablonlari.Sez(tablo);
            var eslesme = ImportSablonlari.SutunOner(tablo, tur);

            var onizleme = new ImportOnizlemeDto
            {
                Sablon = sablon,
                Ayrac = tablo.Ayrac == '\t' ? "TAB" : tablo.Ayrac.ToString(),
                KayitTuru = tur,
                Basliklar = tablo.Basliklar,
                OnerilenEslesme = eslesme,
                GerekliAlanlar = GerekliAlanlar(tur).ToList(),
                ToplamSatir = tablo.Satirlar.Count,
                OrnekSatirlar = tablo.Satirlar.Take(20).ToList()
            };

            for (var i = 0; i < tablo.Satirlar.Count; i++)
            {
                var hata = SatirHatasi(tablo.Satirlar[i], eslesme, tur);
                if (hata != null)
                {
                    onizleme.HataliSatirlar.Add(new ImportHataDto
                    {
                        SatirNo = tablo.SatirNolari[i],
                        Sebep = hata,
                        Icerik = string.Join(tablo.Ayrac.ToString(), tablo.Satirlar[i])
                    });
                }
            }

            return new SuccessDataResult<ImportOnizlemeDto>(onizleme);
        }

        public async Task<IDataResult<ImportSonucDto>> UygulaAsync(int userId, ImportUygulaDto dto)
        {
            var yetki = await YetkiHatasiAsync(userId);
            if (yetki != null)
                return new ErrorDataResult<ImportSonucDto>(yetki);

            var boyutHatasi = BoyutHatasi(dto.Icerik);
            if (boyutHatasi != null)
                return new ErrorDataResult<ImportSonucDto>(boyutHatasi);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<ImportSonucDto>(Messages.VehicleNotFound);

            var tur = KayitTuruNormalize(dto.KayitTuru);
            var eslesme = dto.Eslesme ?? new Dictionary<string, int>();

            foreach (var gerekli in GerekliAlanlar(tur))
            {
                if (!eslesme.ContainsKey(gerekli))
                    return new ErrorDataResult<ImportSonucDto>(Messages.ImportEksikEslesme);
            }

            var tablo = CsvOkuyucu.Oku(dto.Icerik, tur);
            if (tablo.Satirlar.Count > MaxSatir)
                return new ErrorDataResult<ImportSonucDto>(Messages.ImportCokFazlaSatir);

            var mevcutHashler = await _importDal.GetHashesAsync(vehicle.Id);
            var sonuc = new ImportSonucDto { DryRun = dto.DryRun };
            var eklenecekler = new List<(string Hash, DateTime Tarih, decimal Tutar, int? Km, decimal? Litre, bool TamDolum, string Metin)>();
            var buPartideki = new HashSet<string>();

            for (var i = 0; i < tablo.Satirlar.Count; i++)
            {
                var satir = tablo.Satirlar[i];
                var hata = SatirHatasi(satir, eslesme, tur);

                if (hata != null)
                {
                    sonuc.Hatali.Add(new ImportHataDto
                    {
                        SatirNo = tablo.SatirNolari[i],
                        Sebep = hata,
                        Icerik = string.Join(tablo.Ayrac.ToString(), satir)
                    });
                    continue;
                }

                var hash = ImportSatirHash.Hesapla(vehicle.Id, satir);
                if (mevcutHashler.Contains(hash) || !buPartideki.Add(hash))
                {
                    sonuc.Atlanan++;
                    continue;
                }

                eklenecekler.Add((
                    hash,
                    CsvDeger.Tarih(Al(satir, eslesme, "tarih")).Value,
                    CsvDeger.Sayi(Al(satir, eslesme, "tutar")).Value,
                    CsvDeger.Tamsayi(Al(satir, eslesme, "km")),
                    CsvDeger.Sayi(Al(satir, eslesme, "litre")),
                    TamDolumMu(Al(satir, eslesme, "tamdolum")),
                    Al(satir, eslesme, "aciklama") ?? Al(satir, eslesme, "kategori") ?? Al(satir, eslesme, "servis")));
            }

            sonuc.Eklenen = eklenecekler.Count;

            if (dto.DryRun || eklenecekler.Count == 0)
            {
                return new SuccessDataResult<ImportSonucDto>(sonuc, dto.DryRun ? Messages.ImportOnizlendi : Messages.ImportTamamlandi);
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            var enYuksekKm = vehicle.CurrentKm;

            foreach (var kayit in eklenecekler)
            {
                await KayitYazAsync(vehicle, tur, kayit.Tarih, kayit.Tutar, kayit.Km, kayit.Litre, kayit.TamDolum, kayit.Metin);

                await _importDal.AddAsync(new ImportKaydi
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    SatirHash = kayit.Hash,
                    KayitTuru = tur,
                    OlusturmaTarihi = DateTime.UtcNow
                });

                if (kayit.Km != null && kayit.Km.Value > enYuksekKm)
                {
                    enYuksekKm = kayit.Km.Value;
                }
            }

            if (enYuksekKm > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = enYuksekKm;
                await _vehicleDal.UpdateAsync(vehicle);
            }

            await _unitOfWork.CommitAsync();

            return new SuccessDataResult<ImportSonucDto>(sonuc, Messages.ImportTamamlandi);
        }

        private static bool TamDolumMu(string ham)
        {
            if (string.IsNullOrWhiteSpace(ham))
            {
                return true;
            }

            var deger = ham.Trim().ToLowerInvariant();

            return deger != "0" && deger != "false" && deger != "hayir" && deger != "hayır"
                && deger != "no" && deger != "kismi" && deger != "kısmi" && deger != "partial";
        }

        private async Task KayitYazAsync(Vehicle vehicle, string tur, DateTime tarih, decimal tutar, int? kilometre, decimal? litre, bool tamDolum, string metin)
        {
            if (tur == "Yakit")
            {
                await _fuelDal.AddAsync(new FuelRecord
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    Date = tarih,
                    Liters = litre ?? 0m,
                    TotalCost = tutar,
                    Km = kilometre ?? vehicle.CurrentKm,
                    TamDolum = tamDolum
                });
            }
            else if (tur == "Bakim")
            {
                await _maintenanceDal.AddAsync(new MaintenanceRecord
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    Type = MaintenanceType.Diger,
                    Date = tarih,
                    Km = kilometre ?? vehicle.CurrentKm,
                    Cost = tutar,
                    ServiceName = Kirp(metin, 150),
                    Note = null
                });
            }
            else
            {
                await _expenseDal.AddAsync(new ExpenseRecord
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    Category = ExpenseCategory.Diger,
                    Date = tarih,
                    Amount = tutar,
                    Note = Kirp(metin, 500)
                });
            }
        }

        private static string Kirp(string metin, int uzunluk)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return null;
            }

            return metin.Length > uzunluk ? metin.Substring(0, uzunluk) : metin;
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

        private static string BoyutHatasi(byte[] icerik)
        {
            if (icerik == null || icerik.Length == 0)
            {
                return Messages.InvalidValue;
            }

            return icerik.LongLength > MaxBoyut ? Messages.ImportDosyaCokBuyuk : null;
        }

        private static string KayitTuruNormalize(string kayitTuru)
        {
            return KayitTurleri.FirstOrDefault(t => string.Equals(t, kayitTuru, StringComparison.OrdinalIgnoreCase)) ?? "Masraf";
        }

        private static string[] GerekliAlanlar(string kayitTuru)
        {
            return kayitTuru == "Yakit"
                ? new[] { "tarih", "tutar", "litre" }
                : new[] { "tarih", "tutar" };
        }

        private static string Al(string[] satir, Dictionary<string, int> eslesme, string alan)
        {
            if (!eslesme.TryGetValue(alan, out var sira) || sira < 0 || sira >= satir.Length)
            {
                return null;
            }

            var deger = satir[sira];
            return string.IsNullOrWhiteSpace(deger) ? null : deger;
        }

        private static string SatirHatasi(string[] satir, Dictionary<string, int> eslesme, string tur)
        {
            foreach (var gerekli in GerekliAlanlar(tur))
            {
                if (!eslesme.ContainsKey(gerekli))
                {
                    return "Zorunlu sütun eşlenmedi: " + gerekli;
                }
            }

            if (CsvDeger.Tarih(Al(satir, eslesme, "tarih")) == null)
            {
                return "Tarih okunamadı";
            }

            var tutar = CsvDeger.Sayi(Al(satir, eslesme, "tutar"));
            if (tutar == null || tutar <= 0)
            {
                return "Tutar okunamadı";
            }

            if (tur == "Yakit" && CsvDeger.Sayi(Al(satir, eslesme, "litre")) == null)
            {
                return "Litre okunamadı";
            }

            return null;
        }
    }
}
