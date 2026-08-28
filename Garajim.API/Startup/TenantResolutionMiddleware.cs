using Garajim.Core.Multitenancy;

namespace Garajim.API.Startup
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
        {
            var claim = context.User?.FindFirst(GarajimClaimTypes.CompanyId);

            if (claim != null && int.TryParse(claim.Value, out var companyId) && companyId > 0)
            {
                tenantContext.SetCompany(companyId);
            }

            await _next(context);
        }
    }
}
