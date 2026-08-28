namespace Garajim.Core.Multitenancy
{
    public interface ITenantProvider
    {
        int? CompanyId { get; }
    }
}
