namespace Garajim.API.Startup
{
    public static class GuvenlikBasliklari
    {
        public const string VarsayilanScriptKaynaklari = "https://cdn.jsdelivr.net";

        public static string Politika(IConfiguration configuration)
        {
            var ekScript = configuration["Security:ScriptKaynaklari"];
            var scriptKaynaklari = string.IsNullOrWhiteSpace(ekScript) ? VarsayilanScriptKaynaklari : ekScript.Trim();

            return string.Join("; ", new[]
            {
                "default-src 'self'",
                "base-uri 'self'",
                "object-src 'none'",
                "frame-ancestors 'none'",
                "form-action 'self'",
                "img-src 'self' data:",
                "font-src 'self'",
                "connect-src 'self'",
                "style-src 'self' 'unsafe-inline'",
                "script-src 'self' " + scriptKaynaklari
            });
        }

        public static IApplicationBuilder UseGuvenlikBasliklari(this IApplicationBuilder app, IConfiguration configuration)
        {
            var politika = Politika(configuration);

            return app.Use(async (context, next) =>
            {
                var basliklar = context.Response.Headers;
                basliklar["X-Content-Type-Options"] = "nosniff";
                basliklar["X-Frame-Options"] = "DENY";
                basliklar["Referrer-Policy"] = "no-referrer";
                basliklar["Cross-Origin-Opener-Policy"] = "same-origin";
                basliklar["Content-Security-Policy"] = politika;
                basliklar[SurumBilgisi.BaslikAdi] = SurumBilgisi.Surum;

                await next();
            });
        }
    }
}
