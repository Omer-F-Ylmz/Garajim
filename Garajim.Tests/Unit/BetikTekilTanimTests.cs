using System.Text.RegularExpressions;

namespace Garajim.Tests.Unit
{
    public class BetikTekilTanimTests
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

        private static IEnumerable<string> Betikler()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);

            return Directory.GetFiles(Path.Combine(kok.FullName, "Garajim.API", "wwwroot"), "*.js")
                .Select(Path.GetFileName);
        }

        private static Dictionary<string, int> TanimSayilari(string betik)
        {
            var sayilar = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (Match eslesme in Regex.Matches(betik, @"^\s*function\s+([A-Za-z_$][\w$]*)\s*\(", RegexOptions.Multiline))
            {
                var ad = eslesme.Groups[1].Value;
                sayilar[ad] = sayilar.TryGetValue(ad, out var mevcut) ? mevcut + 1 : 1;
            }

            return sayilar;
        }

        [Fact]
        public void SeciliAracTekKezTanimlanir()
        {
            var sayilar = TanimSayilari(Oku("app.js"));

            Assert.True(sayilar.TryGetValue("seciliArac", out var adet), "seciliArac tanımı bulunamadı");
            Assert.Equal(1, adet);
        }

        [Fact]
        public void HicbirIslevIkiKezTanimlanmaz()
        {
            var yinelenenler = new List<string>();

            foreach (var dosya in Betikler())
            {
                foreach (var girdi in TanimSayilari(Oku(dosya)))
                {
                    if (girdi.Value > 1)
                    {
                        yinelenenler.Add(dosya + " -> " + girdi.Key + " (" + girdi.Value + " kez)");
                    }
                }
            }

            Assert.True(yinelenenler.Count == 0, "Yinelenen işlev tanımı: " + string.Join(", ", yinelenenler));
        }

        [Fact]
        public void TanimSayaciGercektenSayar()
        {
            var ornek = "function a() {}\n  function a() {}\nfunction b() {}\n";
            var sayilar = TanimSayilari(ornek);

            Assert.Equal(2, sayilar["a"]);
            Assert.Equal(1, sayilar["b"]);
        }
    }
}
