using Garajim.Business.Abstract;
using Garajim.Business.Seed;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Business.Jobs
{
    public class DemoSifirlamaJob
    {
        private readonly ICompanyDal _companyDal;
        private readonly IUserDal _userDal;
        private readonly IDocumentDal _documentDal;
        private readonly IDocumentService _documentService;
        private readonly DemoDataSeeder _seeder;
        private readonly IConfiguration _configuration;
        private readonly TenantContext _tenantContext;
        private readonly ILogger<DemoSifirlamaJob> _logger;

        public DemoSifirlamaJob(
            ICompanyDal companyDal,
            IUserDal userDal,
            IDocumentDal documentDal,
            IDocumentService documentService,
            DemoDataSeeder seeder,
            IConfiguration configuration,
            TenantContext tenantContext,
            ILogger<DemoSifirlamaJob> logger = null)
        {
            _companyDal = companyDal;
            _userDal = userDal;
            _documentDal = documentDal;
            _documentService = documentService;
            _seeder = seeder;
            _configuration = configuration;
            _tenantContext = tenantContext;
            _logger = logger ?? NullLogger<DemoSifirlamaJob>.Instance;
        }

        public async Task RunAsync()
        {
            if (!_configuration.GetValue("DemoSeed:Enabled", false))
            {
                return;
            }

            var demoUser = await _userDal.GetForAuthenticationAsync(DemoDataSeeder.DemoEmail);

            if (demoUser == null)
            {
                await _seeder.RunAsync();
                return;
            }

            List<string> silinecekDosyalar;

            using (var kapsam = SystemScope.For(_tenantContext, demoUser.CompanyId))
            {
                silinecekDosyalar = (await _documentDal.GetListAsync()).Select(b => b.StoredName).ToList();

                await _companyDal.SirketKayitlariniSilAsync(demoUser.CompanyId);
            }

            foreach (var saklananAd in silinecekDosyalar)
            {
                try
                {
                    _documentService.DosyaSil(saklananAd);
                }
                catch (Exception hata)
                {
                    _logger.LogWarning(hata, "Demo dosyası kaldırılamadı: {Dosya}", saklananAd);
                }
            }

            await _seeder.RunAsync();

            _logger.LogInformation(
                "Demo verisi sıfırlandı. SirketId={SirketId} SilinenDosya={SilinenDosya}",
                demoUser.CompanyId, silinecekDosyalar.Count);
        }
    }
}
