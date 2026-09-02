using System.Security.Claims;

namespace Garajim.API.Startup
{
    public static class PahaliUclar
    {
        public const string RateLimitPolicy = "pahali";

        public const long VarsayilanDosyaSiniri = 5 * 1024 * 1024;

        public const long GovdePayi = 1 * 1024 * 1024;

        public static long MaxIstekGovdesi(IConfiguration configuration)
        {
            var dosyaSiniri = configuration.GetValue("Documents:MaxFileSizeBytes", VarsayilanDosyaSiniri);

            if (dosyaSiniri <= 0)
            {
                dosyaSiniri = VarsayilanDosyaSiniri;
            }

            return dosyaSiniri + GovdePayi;
        }

        public static string Bolum(HttpContext httpContext)
        {
            var kullanici = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return string.IsNullOrWhiteSpace(kullanici)
                ? "ip:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen")
                : "kullanici:" + kullanici;
        }
    }
}
