using Garajim.Business.Katalog;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Business.Jobs
{
    public class KatalogEslemeJob
    {
        private readonly ICompanyDal _companyDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly AracKatalogu _katalog;
        private readonly TenantContext _tenantContext;
        private readonly ILogger<KatalogEslemeJob> _logger;

        public KatalogEslemeJob(
            ICompanyDal companyDal,
            IVehicleDal vehicleDal,
            AracKatalogu katalog,
            TenantContext tenantContext,
            ILogger<KatalogEslemeJob> logger = null)
        {
            _companyDal = companyDal;
            _vehicleDal = vehicleDal;
            _katalog = katalog;
            _tenantContext = tenantContext;
            _logger = logger ?? NullLogger<KatalogEslemeJob>.Instance;
        }

        public async Task<int> RunAsync()
        {
            var sirketler = await _companyDal.GetListAsync();
            var toplam = 0;

            foreach (var sirket in sirketler)
            {
                try
                {
                    toplam += await SirketIcinAsync(sirket.Id);
                }
                catch (Exception hata)
                {
                    _logger.LogError(hata,
                        "Katalog eşlemesi {SirketId} numaralı şirkette başarısız oldu, diğer şirketlerle devam ediliyor.",
                        sirket.Id);
                }
            }

            if (toplam > 0)
            {
                _logger.LogInformation("Katalog eşlemesi {Adet} aracın marka/model alanını güncelledi.", toplam);
            }

            return toplam;
        }

        private async Task<int> SirketIcinAsync(int companyId)
        {
            using var kapsam = SystemScope.For(_tenantContext, companyId);

            var araclar = await _vehicleDal.GetListAsync();
            var degisen = 0;

            foreach (var arac in araclar)
            {
                var eslesme = AracEslestirici.Esle(_katalog, arac.Brand, arac.Model);

                var marka = eslesme?.Marka ?? _katalog.MarkaYazimi(arac.Brand) ?? arac.Brand;
                var seri = eslesme?.Seri ?? arac.Model;
                var eslesmedi = eslesme == null;

                var motor = arac.Motor;

                if (eslesme != null && !string.IsNullOrWhiteSpace(eslesme.Motor) && string.IsNullOrWhiteSpace(motor))
                {
                    motor = eslesme.Motor;
                }

                if (marka == arac.Brand && seri == arac.Model && eslesmedi == arac.ModelEslesmedi && motor == arac.Motor)
                {
                    continue;
                }

                arac.Brand = marka;
                arac.Model = seri;
                arac.ModelEslesmedi = eslesmedi;
                arac.Motor = motor;

                await _vehicleDal.UpdateAsync(arac);
                degisen++;
            }

            return degisen;
        }
    }
}
