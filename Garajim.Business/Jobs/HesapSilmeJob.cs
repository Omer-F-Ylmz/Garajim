using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Business.Jobs
{
    public class HesapSilmeJob
    {
        private readonly ICompanyDal _companyDal;
        private readonly IDocumentDal _documentDal;
        private readonly IDocumentService _documentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TenantContext _tenantContext;
        private readonly ILogger<HesapSilmeJob> _logger;

        public HesapSilmeJob(
            ICompanyDal companyDal,
            IDocumentDal documentDal,
            IDocumentService documentService,
            IUnitOfWork unitOfWork,
            TenantContext tenantContext,
            ILogger<HesapSilmeJob> logger = null)
        {
            _companyDal = companyDal;
            _documentDal = documentDal;
            _documentService = documentService;
            _unitOfWork = unitOfWork;
            _tenantContext = tenantContext;
            _logger = logger ?? NullLogger<HesapSilmeJob>.Instance;
        }

        public async Task RunAsync()
        {
            var simdi = DateTime.UtcNow;
            var sirketler = (await _companyDal.GetListAsync())
                .Where(c => c.SilinmePlanlanan != null && c.SilinmePlanlanan <= simdi)
                .ToList();

            foreach (var sirket in sirketler)
            {
                try
                {
                    await SirketiSilAsync(sirket.Id);
                }
                catch (Exception hata)
                {
                    _logger.LogError(hata,
                        "Hesap silme {SirketId} numaralı şirkette başarısız oldu, diğer şirketlerle devam ediliyor.",
                        sirket.Id);
                }
            }
        }

        private async Task SirketiSilAsync(int companyId)
        {
            List<string> silinecekDosyalar;

            using (var kapsam = SystemScope.For(_tenantContext, companyId))
            {
                var belgeler = await _documentDal.GetListAsync();
                silinecekDosyalar = belgeler.Select(b => b.StoredName).ToList();

                await using var islem = await _unitOfWork.BeginTransactionAsync();

                await _companyDal.SirketVerisiniSilAsync(companyId);

                await _unitOfWork.CommitAsync();
            }

            var silinen = 0;

            foreach (var saklananAd in silinecekDosyalar)
            {
                try
                {
                    _documentService.DosyaSil(saklananAd);
                    silinen++;
                }
                catch (Exception hata)
                {
                    _logger.LogWarning(hata, "Silinen şirketin dosyası kaldırılamadı: {Dosya}", saklananAd);
                }
            }

            _logger.LogInformation(
                "Şirket kalıcı olarak silindi. SirketId={SirketId} Belge={Belge} SilinenDosya={SilinenDosya}",
                companyId, silinecekDosyalar.Count, silinen);
        }
    }
}
