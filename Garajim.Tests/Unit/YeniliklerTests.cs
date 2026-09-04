using System.Text.Json;

namespace Garajim.Tests.Unit
{
    public class YeniliklerTests
    {
        private static DirectoryInfo Kok()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok;
        }

        [Fact]
        public void DegisiklikGunlugundeSurumBasliklariVar()
        {
            var metin = File.ReadAllText(Path.Combine(Kok().FullName, "CHANGELOG.md"));
            var basliklar = metin.Split('\n').Count(s => s.StartsWith("## ", StringComparison.Ordinal));

            Assert.True(basliklar >= 5, "En az beş sürüm başlığı olmalı, şu an: " + basliklar);
            Assert.Contains("Onboarding", metin);
        }

        [Fact]
        public void BuildYenilikleriJsonUretir()
        {
            var yol = Path.Combine(Kok().FullName, "Garajim.API", "wwwroot", "yenilikler.json");

            Assert.True(File.Exists(yol), "yenilikler.json build sırasında üretilmeli.");

            var kayitlar = JsonDocument.Parse(File.ReadAllText(yol)).RootElement;

            Assert.True(kayitlar.GetArrayLength() >= 5);

            var ilk = kayitlar[0];

            Assert.False(string.IsNullOrWhiteSpace(ilk.GetProperty("baslik").GetString()));
            Assert.True(ilk.GetProperty("maddeler").GetArrayLength() > 0);
        }

        [Fact]
        public void YeniliklerSayfasiJsondanOkur()
        {
            var kok = Path.Combine(Kok().FullName, "Garajim.API", "wwwroot");
            var html = File.ReadAllText(Path.Combine(kok, "yenilikler.html"));
            var js = File.ReadAllText(Path.Combine(kok, "yenilikler.js"));

            Assert.Contains("yenilikler.js", html);
            Assert.DoesNotContain("<script>", html);
            Assert.Contains("yenilikler.json", js);
            Assert.DoesNotContain("innerHTML", js);
            Assert.Contains("document.createElement", js);
        }

        [Fact]
        public void SurumSeridiNelerDegistiBaglantisiTasir()
        {
            var html = File.ReadAllText(Path.Combine(Kok().FullName, "Garajim.API", "wwwroot", "index.html"));
            var baslangic = html.IndexOf("id=\"surum-serit\"", StringComparison.Ordinal);
            var bitis = html.IndexOf("</div>", baslangic, StringComparison.Ordinal);
            var blok = html.Substring(baslangic, bitis - baslangic);

            Assert.Contains("yenilikler.html", blok);
            Assert.Contains("Neler değişti?", blok);
        }

        [Fact]
        public void UretilenJsonGitIgnoredadir()
        {
            var ignore = File.ReadAllText(Path.Combine(Kok().FullName, ".gitignore"));

            Assert.Contains("yenilikler.json", ignore);
        }
    }
}
