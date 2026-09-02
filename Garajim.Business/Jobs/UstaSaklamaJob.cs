using Garajim.Core.Multitenancy;
using Garajim.Dal.Abstract;

namespace Garajim.Business.Jobs
{
    public class UstaSaklamaJob
    {
        public const int SaklamaAyi = 24;

        private readonly ICompanyDal _companyDal;
        private readonly IUstaSohbetDal _sohbetDal;
        private readonly IUstaMesajDal _mesajDal;
        private readonly UstaOzetJob _ozetJob;
        private readonly TenantContext _tenantContext;

        public UstaSaklamaJob(
            ICompanyDal companyDal,
            IUstaSohbetDal sohbetDal,
            IUstaMesajDal mesajDal,
            UstaOzetJob ozetJob,
            TenantContext tenantContext)
        {
            _companyDal = companyDal;
            _sohbetDal = sohbetDal;
            _mesajDal = mesajDal;
            _ozetJob = ozetJob;
            _tenantContext = tenantContext;
        }

        public async Task RunAsync()
        {
            await _ozetJob.RunAsync();

            var sinir = DateTime.UtcNow.Date.AddMonths(-SaklamaAyi);
            var companies = await _companyDal.GetListAsync();

            foreach (var company in companies)
            {
                _tenantContext.SetCompany(company.Id);

                var eskiler = await _sohbetDal.EskiSohbetIdleriAsync(sinir);
                if (eskiler.Count == 0)
                {
                    continue;
                }

                await _mesajDal.DeleteBySohbetlerAsync(eskiler);
                await _sohbetDal.SohbetleriSilAsync(eskiler);
            }

            _tenantContext.Clear();
        }
    }
}
