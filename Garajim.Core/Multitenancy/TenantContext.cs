namespace Garajim.Core.Multitenancy
{
    public class TenantContext : ITenantProvider
    {
        public int? CompanyId { get; private set; }

        public void SetCompany(int companyId)
        {
            CompanyId = companyId;
        }

        public void Clear()
        {
            CompanyId = null;
        }
    }
}
