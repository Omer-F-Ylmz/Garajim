using Garajim.Core.Multitenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;

namespace Garajim.Business.Jobs
{
    public class UstaOzetJob
    {
        private readonly ICompanyDal _companyDal;
        private readonly IUstaMesajDal _mesajDal;
        private readonly IUstaSohbetDal _sohbetDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IMaintenancePartDal _partDal;
        private readonly IUstaCozumOzetiDal _ozetDal;
        private readonly TenantContext _tenantContext;
        private readonly ILogger<UstaOzetJob> _logger;

        public UstaOzetJob(
            ICompanyDal companyDal,
            IUstaMesajDal mesajDal,
            IUstaSohbetDal sohbetDal,
            IVehicleDal vehicleDal,
            IMaintenancePartDal partDal,
            IUstaCozumOzetiDal ozetDal,
            TenantContext tenantContext,
            ILogger<UstaOzetJob> logger = null)
        {
            _logger = logger ?? NullLogger<UstaOzetJob>.Instance;
            _companyDal = companyDal;
            _mesajDal = mesajDal;
            _sohbetDal = sohbetDal;
            _vehicleDal = vehicleDal;
            _partDal = partDal;
            _ozetDal = ozetDal;
            _tenantContext = tenantContext;
        }

        public async Task RunAsync()
        {
            var sayaclar = new Dictionary<(string Marka, string Model, string Motor, string Kategori, string Parca), int>();
            var companies = await _companyDal.GetListAsync();

            try
            {
                foreach (var company in companies)
                {
                    try
                    {
                        _tenantContext.SetCompany(company.Id);
                        await SirketIcinTopla(sayaclar);
                    }
                    catch (Exception hata)
                    {
                        _logger.LogError(hata, "AI Usta özeti {SirketId} numaralı şirkette başarısız oldu, diğer şirketlerle devam ediliyor.", company.Id);
                    }
                }
            }
            finally
            {
                _tenantContext.Clear();
            }

            foreach (var kayit in sayaclar)
            {
                var mevcut = await _ozetDal.BulAsync(kayit.Key.Marka, kayit.Key.Model, kayit.Key.Motor, kayit.Key.Kategori, kayit.Key.Parca);

                if (mevcut == null)
                {
                    await _ozetDal.AddAsync(new UstaCozumOzeti
                    {
                        Marka = kayit.Key.Marka,
                        Model = kayit.Key.Model,
                        Motor = kayit.Key.Motor,
                        BelirtiKategori = kayit.Key.Kategori,
                        ParcaTuru = kayit.Key.Parca,
                        Sayi = kayit.Value,
                        GuncellemeTarihi = DateTime.UtcNow
                    });
                    continue;
                }

                mevcut.Sayi += kayit.Value;
                mevcut.GuncellemeTarihi = DateTime.UtcNow;
                await _ozetDal.UpdateAsync(mevcut);
            }
        }


        private async Task SirketIcinTopla(Dictionary<(string, string, string, string, string), int> sayaclar)
        {
            var mesajlar = await _mesajDal.GetOzetlenmemisCozumluMesajlarAsync();
            if (mesajlar.Count == 0)
            {
                return;
            }

            var islenenler = new List<int>();

            foreach (var mesaj in mesajlar)
            {
                if (string.IsNullOrWhiteSpace(mesaj.BilgiKategorisi))
                {
                    continue;
                }

                var sohbet = await _sohbetDal.GetAsync(s => s.Id == mesaj.SohbetId);
                if (sohbet == null)
                {
                    continue;
                }

                var vehicle = await _vehicleDal.GetAsync(v => v.Id == sohbet.VehicleId);
                if (vehicle == null)
                {
                    continue;
                }

                var parcalar = (await _partDal.GetByVehicleAsync(vehicle.Id))
                    .Where(p => p.MaintenanceRecordId == mesaj.CozumBakimId.Value)
                    .Select(p => p.ParcaTuru)
                    .Distinct()
                    .ToList();

                if (parcalar.Count == 0)
                {
                    parcalar.Add(ParcaTuru.Diger);
                }

                foreach (var parca in parcalar)
                {
                    var anahtar = (Kirp(vehicle.Brand, 60), Kirp(vehicle.Model, 60), Kirp(vehicle.Motor, 40),
                        Kirp(mesaj.BilgiKategorisi, 60), parca.ToString());

                    sayaclar.TryGetValue(anahtar, out var mevcut);
                    sayaclar[anahtar] = mevcut + 1;
                }

                islenenler.Add(mesaj.Id);
            }

            await _mesajDal.OzetlendiIsaretleAsync(islenenler);
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
    }
}
