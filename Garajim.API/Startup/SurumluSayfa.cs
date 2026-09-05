using Microsoft.Extensions.Primitives;

namespace Garajim.API.Startup
{
    public static class SurumluSayfa
    {
        public static readonly string[] Sayfalar =
        {
            "/index.html",
            "/yardim.html",
            "/yenilikler.html",
            "/yonetim.html"
        };

        public const string SurumAnahtari = "v";

        public static bool Kapsamda(PathString yol)
        {
            var deger = yol.HasValue ? yol.Value : string.Empty;

            if (deger == "/" || deger.Length == 0)
            {
                return true;
            }

            return Sayfalar.Contains(deger, StringComparer.OrdinalIgnoreCase);
        }

        public static IApplicationBuilder UseSurumluSayfa(this IApplicationBuilder app, IWebHostEnvironment ortam)
        {
            var kok = ortam.WebRootPath ?? string.Empty;

            return app.Use(async (context, next) =>
            {
                if (!HttpMethods.IsGet(context.Request.Method) || !Kapsamda(context.Request.Path))
                {
                    await next();
                    return;
                }

                var ad = context.Request.Path.Value == "/" || !context.Request.Path.HasValue
                    ? "index.html"
                    : context.Request.Path.Value.TrimStart('/');

                var dosya = Path.Combine(kok, ad);

                if (!File.Exists(dosya))
                {
                    await next();
                    return;
                }

                var govde = (await File.ReadAllTextAsync(dosya)).Replace(SurumBilgisi.YerTutucu, SurumBilgisi.Surum);

                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers.CacheControl = new StringValues("no-cache, must-revalidate");

                await context.Response.WriteAsync(govde);
            });
        }
    }
}
