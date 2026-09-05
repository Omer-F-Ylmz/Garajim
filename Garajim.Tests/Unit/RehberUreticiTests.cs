using System.Text.Json;
using System.Text.RegularExpressions;
using Garajim.RehberUretici;

namespace Garajim.Tests.Unit
{
    public class RehberUreticiTests
    {
        private static readonly object Kilit = new object();
        private static string _cikti;
        private static UretimSonucu _sonuc;

        private readonly string _cikis;

        public RehberUreticiTests()
        {
            lock (Kilit)
            {
                if (_cikti == null)
                {
                    _cikti = Path.Combine(Path.GetTempPath(), "rehber-" + Guid.NewGuid().ToString("N"));
                    _sonuc = Uretici.Uret(BilgiKlasoru(), _cikti, Sabitler.TabanAdres);
                }
            }

            _cikis = _cikti;
        }

        private static string Kok()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok.FullName;
        }

        private static string BilgiKlasoru() => Path.Combine(Kok(), "Garajim.Business", "Usta", "Bilgi");

        private string Oku(string goreliYol) => File.ReadAllText(Path.Combine(_cikis, goreliYol));

        private string[] Sayfalar() => Directory.GetFiles(_cikis, "*.html", SearchOption.AllDirectories);

        [Fact]
        public void SayfaSayisiKayitSayisiArtiHublardir()
        {
            Assert.Equal(387, _sonuc.Kayitlar.Count);
            Assert.Equal(_sonuc.Kayitlar.Count + Bolumler.Hepsi.Count + 1, Sayfalar().Length);
        }

        [Fact]
        public void SluglarTekildir()
        {
            var sluglar = _sonuc.Kayitlar.Select(k => k.Bolum + "/" + k.Slug).ToList();

            Assert.Equal(sluglar.Count, sluglar.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void HerSayfaZorunluOgeleriTasir()
        {
            foreach (var yol in Sayfalar())
            {
                var html = File.ReadAllText(yol);
                var ad = Path.GetFileName(yol);

                Assert.Contains("<title>", html);
                Assert.Contains("name=\"description\"", html);
                Assert.Contains("rel=\"canonical\"", html);
                Assert.Contains("utm_source=rehber", html);
                Assert.Contains(Sabitler.Uyari, html);
                Assert.Contains("BreadcrumbList", html);
                Assert.DoesNotContain("innerHTML", html);
                Assert.False(html.Contains(" onclick", StringComparison.OrdinalIgnoreCase), ad + " satır içi olay taşıyor");
            }
        }

        [Fact]
        public void BaslikVeAciklamaSinirlariTutar()
        {
            foreach (var yol in Sayfalar())
            {
                var html = File.ReadAllText(yol);

                var baslik = Regex.Match(html, "<title>(.*?)</title>", RegexOptions.Singleline).Groups[1].Value;
                var aciklama = Regex.Match(html, "name=\"description\" content=\"(.*?)\"", RegexOptions.Singleline).Groups[1].Value;

                Assert.True(baslik.Length > 0 && baslik.Length <= 60, Path.GetFileName(yol) + " title " + baslik.Length);
                Assert.True(aciklama.Length > 0 && aciklama.Length <= 155, Path.GetFileName(yol) + " description " + aciklama.Length);
            }
        }

        [Fact]
        public void KayitSayfalariMakaleVeIlgiliBaglantiTasir()
        {
            foreach (var kayit in _sonuc.Kayitlar)
            {
                var html = Oku(Path.Combine(kayit.Bolum, kayit.Slug + ".html"));

                Assert.Contains("\"@type\":\"Article\"", html);
                Assert.Contains(kayit.Guncelleme, html);
                Assert.InRange(kayit.Ilgili.Count, 3, 6);
            }
        }

        [Fact]
        public void IlgiliBaglantilarKirikDegil()
        {
            var mevcut = new HashSet<string>(
                Sayfalar().Select(y => Path.GetRelativePath(_cikis, y).Replace('\\', '/')),
                StringComparer.Ordinal);

            var kirik = new List<string>();

            foreach (var yol in Sayfalar())
            {
                foreach (Match eslesme in Regex.Matches(File.ReadAllText(yol), "href=\"(/rehber/[^\"]*)\""))
                {
                    var hedef = eslesme.Groups[1].Value.Substring("/rehber/".Length);

                    if (hedef.Length == 0 || hedef.EndsWith("/", StringComparison.Ordinal))
                    {
                        hedef += "index.html";
                    }

                    if (!mevcut.Contains(hedef))
                    {
                        kirik.Add(Path.GetFileName(yol) + " -> " + eslesme.Groups[1].Value);
                    }
                }
            }

            Assert.Empty(kirik);
        }

        [Fact]
        public void BakimSayfalariKuralKutusuTasir()
        {
            var sayfalar = _sonuc.Kayitlar
                .Where(k => k.Bolum == Bolumler.Bakim && k.Id != Sabitler.BakimKuralId)
                .ToList();

            Assert.NotEmpty(sayfalar);

            foreach (var kayit in sayfalar)
            {
                Assert.Contains(Sabitler.BakimKuralBasligi, Oku(Path.Combine(kayit.Bolum, kayit.Slug + ".html")));
            }
        }

        [Fact]
        public void IndexJsonSemasiVeBoyutu()
        {
            var yol = Path.Combine(_cikis, "index.json");
            var uzunluk = new FileInfo(yol).Length;

            Assert.True(uzunluk <= 150 * 1024, uzunluk + " bayt");

            using var belge = JsonDocument.Parse(File.ReadAllText(yol));
            var kokEleman = belge.RootElement;

            Assert.Equal(JsonValueKind.Array, kokEleman.ValueKind);
            Assert.Equal(_sonuc.Kayitlar.Count, kokEleman.GetArrayLength());

            foreach (var oge in kokEleman.EnumerateArray())
            {
                Assert.False(string.IsNullOrWhiteSpace(oge.GetProperty("slug").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(oge.GetProperty("baslik").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(oge.GetProperty("kategori").GetString()));
                Assert.True(oge.GetProperty("anahtarlar").GetArrayLength() > 0);
            }
        }

        [Fact]
        public void SitemapTumRehberUrlleriniIcerir()
        {
            var sitemap = File.ReadAllText(Path.Combine(_cikis, "sitemap.xml"));

            Assert.Contains(Sabitler.TabanAdres + "/rehber/", sitemap);
            Assert.Contains("<lastmod>", sitemap);

            foreach (var kayit in _sonuc.Kayitlar.Take(20))
            {
                Assert.Contains("/rehber/" + kayit.Bolum + "/" + kayit.Slug + ".html", sitemap);
            }

            Assert.Equal(_sonuc.Kayitlar.Count + Bolumler.Hepsi.Count + Sabitler.DurgunSayfalar.Length,
                Regex.Matches(sitemap, "<loc>").Count);
        }

        [Fact]
        public void UreticiFikirSabitidir()
        {
            var ikinci = Path.Combine(Path.GetTempPath(), "rehber-" + Guid.NewGuid().ToString("N"));

            try
            {
                Uretici.Uret(BilgiKlasoru(), ikinci, Sabitler.TabanAdres);

                var ilkDosyalar = Directory.GetFiles(_cikis, "*", SearchOption.AllDirectories)
                    .Select(y => Path.GetRelativePath(_cikis, y)).OrderBy(y => y, StringComparer.Ordinal).ToList();
                var ikinciDosyalar = Directory.GetFiles(ikinci, "*", SearchOption.AllDirectories)
                    .Select(y => Path.GetRelativePath(ikinci, y)).OrderBy(y => y, StringComparer.Ordinal).ToList();

                Assert.Equal(ilkDosyalar, ikinciDosyalar);

                foreach (var gorel in ilkDosyalar)
                {
                    Assert.Equal(
                        File.ReadAllText(Path.Combine(_cikis, gorel)),
                        File.ReadAllText(Path.Combine(ikinci, gorel)));
                }
            }
            finally
            {
                Directory.Delete(ikinci, true);
            }
        }

        [Fact]
        public void BozukKayitAtlanirUretimKirilmaz()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "rehber-bozuk-" + Guid.NewGuid().ToString("N"));
            var cikis = Path.Combine(Path.GetTempPath(), "rehber-bozuk-cikti-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(klasor);

            try
            {
                File.WriteAllText(Path.Combine(klasor, "belirtiler.json"),
                    "[{\"id\":\"blr-001\",\"kategori\":\"fren\",\"anahtarlar\":[\"fren sesi\"],\"metin\":\"Belirti: iyi kayıt.\",\"kaynak\":\"k\",\"guncelleme\":\"2026-09-02\"}," +
                    "{\"id\":\"blr-002\",\"kategori\":\"fren\",\"anahtarlar\":[],\"metin\":\"   \",\"kaynak\":\"\",\"guncelleme\":\"\"}]");

                var sonuc = Uretici.Uret(klasor, cikis, Sabitler.TabanAdres);

                Assert.Single(sonuc.Kayitlar);
                Assert.Contains(sonuc.Uyarilar, u => u.Contains("blr-002"));
            }
            finally
            {
                Directory.Delete(klasor, true);

                if (Directory.Exists(cikis))
                {
                    Directory.Delete(cikis, true);
                }
            }
        }

        [Fact]
        public void ObdVeBelirtiCaprazBaglanir()
        {
            var caprazlar = _sonuc.Kayitlar
                .Where(k => k.Bolum == Bolumler.Obd)
                .Count(k => k.Ilgili.Any(i => i.Bolum != Bolumler.Obd));

            Assert.True(caprazlar > 0, "hiçbir OBD sayfası başka bölüme bağlanmıyor");
        }

        [Fact]
        public void HubSayfalariVeAramaKutusuVar()
        {
            Assert.Contains("rehber.js", Oku("index.html"));
            Assert.Contains("rehber-ara", Oku("index.html"));

            foreach (var bolum in Bolumler.Hepsi)
            {
                Assert.Contains("<h1", Oku(Path.Combine(bolum.Yol, "index.html")));
            }
        }
    }
}
