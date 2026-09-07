using System.Text.RegularExpressions;

namespace Garajim.Tests.Unit
{
    public class ServiceWorkerKabukTests
    {
        private static string Oku(string dosya)
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", dosya));
        }

        private static string KabukListesi()
        {
            var sw = Oku("sw.js");
            var bas = sw.IndexOf("KABUK_DOSYALARI", StringComparison.Ordinal);
            var son = sw.IndexOf("]", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas);
            return sw.Substring(bas, son - bas);
        }

        private static IEnumerable<string> IndeksVarliklari()
        {
            var html = Oku("index.html");

            foreach (Match eslesme in Regex.Matches(html, @"(?:src|href)=""(?!https?:|//|#|/?(?:rehber|yardim|yenilikler|yonetim|karne|acil|sartlar|gizlilik|api)\b)([^""]+\.(?:js|css|png|svg|json))(?:\?[^""]*)?"""))
            {
                var yol = eslesme.Groups[1].Value.TrimStart('/');

                if (yol.StartsWith("img/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return "/" + yol;
            }
        }

        [Fact]
        public void IndeksinIstedigiVarliklarKabukta()
        {
            var kabuk = KabukListesi();
            var eksik = IndeksVarliklari().Distinct().Where(v => !kabuk.Contains("\"" + v + "\"", StringComparison.Ordinal)).ToList();

            Assert.True(eksik.Count == 0, "Kabuk listesinde eksik: " + string.Join(", ", eksik));
        }

        [Fact]
        public void OnbellekAramasiSorguDizesiniYokSayar()
        {
            var sw = Oku("sw.js");

            Assert.Contains("ignoreSearch: true", sw);
            Assert.DoesNotContain("caches.match(istek).then", sw);
        }

        [Fact]
        public void KabukListesiTanitimGorsellerinialmaz()
        {
            Assert.DoesNotContain("/img/", KabukListesi());
        }

        [Fact]
        public void AyristiriciGercektenVarlikBulur()
        {
            var varliklar = IndeksVarliklari().ToList();

            Assert.Contains("/app.js", varliklar);
            Assert.Contains("/styles.css", varliklar);
        }
    }
}
