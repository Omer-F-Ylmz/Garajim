namespace Garajim.Core.Multitenancy
{
    public sealed class SystemScope : IDisposable
    {
        private readonly TenantContext _tenantContext;
        private readonly int? _oncekiCompanyId;

        private SystemScope(TenantContext tenantContext, int companyId)
        {
            _tenantContext = tenantContext;
            _oncekiCompanyId = tenantContext.CompanyId;
            _tenantContext.SetCompany(companyId);
        }

        public static SystemScope For(TenantContext tenantContext, int companyId)
        {
            return new SystemScope(tenantContext, companyId);
        }

        public void Dispose()
        {
            if (_oncekiCompanyId == null)
            {
                _tenantContext.Clear();
            }
            else
            {
                _tenantContext.SetCompany(_oncekiCompanyId.Value);
            }
        }
    }
}
