using System.Security.Cryptography;
using System.Text;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Parts;
using Garajim.Business.Constants;
using Garajim.Core.Multitenancy;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class KarneManager : IKarneService
    {
        private readonly IKarnePaylasimiDal _karneDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IMaintenancePartDal _partDal;
        private readonly IFuelDal _fuelDal;
        private readonly IDocumentDal _documentDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IDocumentService _documentService;
        private readonly TenantContext _tenantContext;
        private readonly IConfiguration _configuration;

        public KarneManager(
            IKarnePaylasimiDal karneDal,
            IVehicleDal vehicleDal,
            IMaintenanceDal maintenanceDal,
            IMaintenancePartDal partDal,
            IFuelDal fuelDal,
            IDocumentDal documentDal,
            IVehicleAccessService vehicleAccess,
            IDocumentService documentService,
            TenantContext tenantContext,
            IConfiguration configuration)
        {
            _karneDal = karneDal;
            _vehicleDal = vehicleDal;
            _maintenanceDal = maintenanceDal;
            _partDal = partDal;
            _fuelDal = fuelDal;
            _documentDal = documentDal;
            _vehicleAccess = vehicleAccess;
            _documentService = documentService;
            _tenantContext = tenantContext;
            _configuration = configuration;
        }

        public async Task<IDataResult<KarneLinkDto>> OlusturAsync(int userId, int vehicleId, KarneOlusturDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<KarneLinkDto>(Messages.VehicleNotFound);

            await _karneDal.PasiflestirAsync(vehicleId);

            var hamToken = HamTokenUret();
            var kapsam = dto?.Kapsam ?? new KarneKapsamDto();

            var paylasim = new KarnePaylasimi
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                TokenHash = Hashle(hamToken),
                BakimGecmisi = kapsam.BakimGecmisi,
                ParcaHafizasi = kapsam.ParcaHafizasi,
                YakitOzeti = kapsam.YakitOzeti,
                Belgeler = kapsam.Belgeler,
                PlakaGoster = kapsam.PlakaGoster,
                TutarGoster = kapsam.TutarGoster,
                SonKullanma = dto?.SonKullanmaGun == null ? null : DateTime.UtcNow.AddDays(dto.SonKullanmaGun.Value),
                Aktif = true,
                GoruntulenmeSayisi = 0,
                OlusturmaTarihi = DateTime.UtcNow
            };

            await _karneDal.AddAsync(paylasim);

            var taban = (_configuration["App:BaseUrl"] ?? string.Empty).TrimEnd('/');
            return new SuccessDataResult<KarneLinkDto>(new KarneLinkDto
            {
                Url = $"{taban}/karne.html?t={hamToken}",
                SonKullanma = paylasim.SonKullanma,
                GoruntulenmeSayisi = 0
            }, Messages.KarneCreated);
        }

        public async Task<IResult> KapatAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.VehicleNotFound);

            await _karneDal.PasiflestirAsync(vehicleId);
            return new SuccessResult(Messages.KarneClosed);
        }

        public async Task<IDataResult<KarneDto>> GoruntuleAsync(string token)
        {
            var paylasim = await GecerliPaylasimAsync(token);
            if (paylasim == null)
                return new ErrorDataResult<KarneDto>(Messages.KarneNotFound);

            using var kapsam = SystemScope.For(_tenantContext, paylasim.CompanyId);

            var vehicle = await _vehicleDal.GetAsync(v => v.Id == paylasim.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<KarneDto>(Messages.KarneNotFound);

            var karne = new KarneDto
            {
                Arac = new KarneAracDto
                {
                    Plaka = paylasim.PlakaGoster ? vehicle.Plate : PlakaMaskele(vehicle.Plate),
                    Marka = vehicle.Brand,
                    Model = vehicle.Model,
                    Yil = vehicle.Year,
                    YakitTipi = vehicle.FuelType.ToString(),
                    GuncelKm = vehicle.CurrentKm
                }
            };

            if (paylasim.BakimGecmisi)
            {
                var bakimlar = await _maintenanceDal.GetListAsync(m => m.VehicleId == vehicle.Id);
                karne.Bakimlar = bakimlar
                    .OrderByDescending(m => m.Date)
                    .Select(m => new KarneBakimDto
                    {
                        Tarih = m.Date,
                        Tur = m.Type.ToString(),
                        Km = m.Km,
                        Tutar = paylasim.TutarGoster ? m.Cost : null,
                        ServisAdi = m.ServiceName
                    }).ToList();
            }

            if (paylasim.ParcaHafizasi)
            {
                karne.Parcalar = await ParcaOzetiAsync(vehicle);
            }

            if (paylasim.YakitOzeti)
            {
                var yakitlar = await _fuelDal.GetListAsync(f => f.VehicleId == vehicle.Id);
                karne.YakitOzeti = new KarneYakitOzetiDto
                {
                    KayitSayisi = yakitlar.Count,
                    ToplamLitre = yakitlar.Sum(f => f.Liters),
                    ToplamTutar = paylasim.TutarGoster ? yakitlar.Sum(f => f.TotalCost) : null,
                    SonDolumTarihi = yakitlar.Count == 0 ? null : yakitlar.Max(f => f.Date)
                };
            }

            if (paylasim.Belgeler)
            {
                var belgeler = await _documentDal.GetListAsync(d => d.VehicleId == vehicle.Id);
                karne.Belgeler = belgeler
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new KarneBelgeDto { Id = d.Id, Ad = d.OriginalName, Tarih = d.CreatedAt })
                    .ToList();
            }

            await _karneDal.GoruntulenmeArtirAsync(paylasim.Id);

            return new SuccessDataResult<KarneDto>(karne);
        }

        public async Task<IDataResult<DocumentContentDto>> BelgeAsync(string token, int documentId)
        {
            var paylasim = await GecerliPaylasimAsync(token);
            if (paylasim == null || !paylasim.Belgeler)
                return new ErrorDataResult<DocumentContentDto>(Messages.KarneNotFound);

            using var kapsam = SystemScope.For(_tenantContext, paylasim.CompanyId);

            var belge = await _documentDal.GetAsync(d => d.Id == documentId);
            if (belge == null || belge.VehicleId != paylasim.VehicleId)
                return new ErrorDataResult<DocumentContentDto>(Messages.KarneNotFound);

            var tamYol = Path.Combine(DocumentManager.DepoYolunuCoz(_configuration["Documents:StoragePath"]), belge.StoredName);
            if (!File.Exists(tamYol))
                return new ErrorDataResult<DocumentContentDto>(Messages.KarneNotFound);

            return new SuccessDataResult<DocumentContentDto>(new DocumentContentDto
            {
                OriginalName = belge.OriginalName,
                ContentType = belge.ContentType,
                Content = await File.ReadAllBytesAsync(tamYol)
            });
        }

        public async Task<IDataResult<KarneStatsDto>> StatsAsync(int userId)
        {
            var araclar = await _vehicleDal.GetListAsync();
            var (aktifArac, goruntulenme) = await _karneDal.IstatistikAsync();

            return new SuccessDataResult<KarneStatsDto>(new KarneStatsDto
            {
                AracSayisi = araclar.Count,
                KarnesiAktifArac = aktifArac,
                AktifOran = araclar.Count == 0 ? 0 : Math.Round(aktifArac * 100.0 / araclar.Count, 1),
                ToplamGoruntulenme = goruntulenme
            });
        }

        private async Task<KarnePaylasimi> GecerliPaylasimAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var paylasim = await _karneDal.GetByTokenHashAsync(Hashle(token));
            if (paylasim == null)
            {
                return null;
            }

            if (paylasim.SonKullanma != null && paylasim.SonKullanma.Value < DateTime.UtcNow)
            {
                return null;
            }

            return paylasim;
        }

        private async Task<List<ParcaHafizasiDto>> ParcaOzetiAsync(Vehicle vehicle)
        {
            var parcalar = await _partDal.GetByVehicleAsync(vehicle.Id);
            if (parcalar.Count == 0)
            {
                return new List<ParcaHafizasiDto>();
            }

            var kayitlar = await _maintenanceDal.GetListAsync(m => m.VehicleId == vehicle.Id);
            var kayitSozlugu = kayitlar.ToDictionary(k => k.Id);

            var liste = new List<ParcaHafizasiDto>();

            foreach (var grup in parcalar.GroupBy(p => p.ParcaTuru))
            {
                var kayitliOlanlar = grup
                    .Where(p => kayitSozlugu.ContainsKey(p.MaintenanceRecordId))
                    .Select(p => kayitSozlugu[p.MaintenanceRecordId])
                    .OrderByDescending(k => k.Date)
                    .ToList();

                if (kayitliOlanlar.Count == 0)
                {
                    continue;
                }

                var aralik = ParcaAraliklari.Al(grup.Key);
                liste.Add(new ParcaHafizasiDto
                {
                    ParcaTuru = grup.Key,
                    ParcaAdi = aralik.Ad,
                    SonDegisimTarihi = kayitliOlanlar[0].Date,
                    SonDegisimKm = kayitliOlanlar[0].Km,
                    DegisimSayisi = kayitliOlanlar.Count,
                    ToplamTutar = 0m,
                    SonrakiTahminiKm = aralik.Km == null ? null : kayitliOlanlar[0].Km + aralik.Km.Value,
                    SonrakiTahminiTarih = aralik.Ay == null ? null : kayitliOlanlar[0].Date.AddMonths(aralik.Ay.Value),
                    Durum = "Iyi"
                });
            }

            return liste.OrderBy(l => l.ParcaAdi).ToList();
        }

        public static string PlakaMaskele(string plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka) || plaka.Length < 4)
            {
                return "***";
            }

            return plaka.Substring(0, 2) + " *** " + plaka.Substring(plaka.Length - 3);
        }

        private static string HamTokenUret()
        {
            var bayt = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bayt).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string Hashle(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }
    }
}
