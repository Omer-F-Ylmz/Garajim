using Garajim.Business.Abstract;
using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Business.Jobs
{
    public class FisTemizlemeJob
    {
        public const int BeklemeGunu = 30;

        private readonly ICompanyDal _companyDal;
        private readonly IReceiptService _receiptService;
        private readonly IReceiptDraftDal _draftDal;
        private readonly IUserDal _userDal;
        private readonly TenantContext _tenantContext;
        private readonly ILogger<FisTemizlemeJob> _logger;

        public FisTemizlemeJob(
            ICompanyDal companyDal,
            IReceiptService receiptService,
            IReceiptDraftDal draftDal,
            IUserDal userDal,
            TenantContext tenantContext,
            ILogger<FisTemizlemeJob> logger = null)
        {
            _companyDal = companyDal;
            _receiptService = receiptService;
            _draftDal = draftDal;
            _userDal = userDal;
            _tenantContext = tenantContext;
            _logger = logger ?? NullLogger<FisTemizlemeJob>.Instance;
        }

        public async Task RunAsync()
        {
            var sinir = DateTime.UtcNow.AddDays(-BeklemeGunu);
            var sirketler = await _companyDal.GetListAsync();

            foreach (var sirket in sirketler)
            {
                try
                {
                    await SirketIcinAsync(sirket.Id, sinir);
                }
                catch (Exception hata)
                {
                    _logger.LogError(hata,
                        "Fiş temizleme {SirketId} numaralı şirkette başarısız oldu, diğer şirketlerle devam ediliyor.",
                        sirket.Id);
                }
            }
        }

        private async Task SirketIcinAsync(int companyId, DateTime sinir)
        {
            using var kapsam = SystemScope.For(_tenantContext, companyId);

            var eskiler = await _draftDal.GetListAsync(d =>
                d.Durum == ReceiptDraftStatus.Bekliyor && d.OlusturmaTarihi < sinir);

            if (eskiler.Count == 0)
            {
                return;
            }

            var sahip = (await _userDal.GetListAsync(u => u.Role == CompanyRole.Owner && u.IsActive)).FirstOrDefault();

            if (sahip == null)
            {
                return;
            }

            var silinen = 0;

            foreach (var taslak in eskiler)
            {
                var sonuc = await _receiptService.RejectAsync(sahip.Id, taslak.Id);

                if (sonuc.Success)
                {
                    silinen++;
                }
            }

            _logger.LogInformation(
                "Bekleyen fiş taslakları temizlendi. SirketId={SirketId} Silinen={Silinen}",
                companyId, silinen);
        }
    }
}
