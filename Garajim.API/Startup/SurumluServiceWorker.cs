namespace Garajim.API.Startup
{
    public static class SurumluServiceWorker
    {
        public const string Yol = "/sw.js";

        public static IApplicationBuilder UseSurumluServiceWorker(this IApplicationBuilder app, IWebHostEnvironment ortam)
        {
            var dosya = Path.Combine(ortam.WebRootPath ?? string.Empty, "sw.js");

            return app.Use(async (context, next) =>
            {
                if (!context.Request.Path.Equals(Yol, StringComparison.OrdinalIgnoreCase) || !File.Exists(dosya))
                {
                    await next();
                    return;
                }

                var govde = (await File.ReadAllTextAsync(dosya)).Replace(SurumBilgisi.YerTutucu, SurumBilgisi.Surum);

                context.Response.ContentType = "text/javascript; charset=utf-8";
                context.Response.Headers.CacheControl = "no-cache, must-revalidate";
                context.Response.Headers["Service-Worker-Allowed"] = "/";

                await context.Response.WriteAsync(govde);
            });
        }
    }
}
