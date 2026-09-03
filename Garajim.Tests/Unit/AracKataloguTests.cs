using Garajim.Business.Katalog;
using Garajim.ML.Models;

namespace Garajim.Tests.Unit
{
    public class AracKataloguTests
    {
        private static string DepoKoku()
        {
            var klasor = new DirectoryInfo(AppContext.BaseDirectory);

            while (klasor != null && !File.Exists(Path.Combine(klasor.FullName, "Garajim.sln")))
            {
                klasor = klasor.Parent;
            }

            Assert.NotNull(klasor);
            return klasor.FullName;
        }

        private static AracKatalogu Katalog() =>
            AracKatalogu.Yukle(Path.Combine(AppContext.BaseDirectory, AracKatalogu.KlasorAdi));

        private static FiyatModeliSozlugu Sozluk() =>
            FiyatModeliSozlugu.Yukle(Path.Combine(AppContext.BaseDirectory, "MLModels", "price-model.zip"));

        [Fact]
        public void SozluktekiHerMarkaKatalogda()
        {
            var katalog = Katalog();
            var eksik = Sozluk().Markalar.Where(m => !katalog.MarkaVar(m)).ToList();

            Assert.True(eksik.Count == 0, "Katalogda olmayan marka: " + string.Join(", ", eksik));
        }

        [Fact]
        public void SozluktekiHerSeriKatalogda()
        {
            var katalog = Katalog();
            var eksik = Sozluk().Seriler.Where(s => katalog.SerininMarkasi(s) == null).ToList();

            Assert.True(eksik.Count == 0, "Katalogda olmayan seri: " + string.Join(", ", eksik));
        }

        [Fact]
        public void KatalogdakiHerAdSozlukte()
        {
            var katalog = Katalog();
            var sozluk = Sozluk();

            var fazlaMarka = katalog.MarkaAdlari.Where(m => !sozluk.MarkaTaniniyor(m)).ToList();
            var fazlaSeri = katalog.Markalar
                .SelectMany(m => m.Seriler)
                .Where(s => !sozluk.SeriTaniniyor(s))
                .ToList();

            Assert.True(fazlaMarka.Count == 0, "Sözlükte olmayan marka: " + string.Join(", ", fazlaMarka));
            Assert.True(fazlaSeri.Count == 0, "Sözlükte olmayan seri: " + string.Join(", ", fazlaSeri));
        }

        [Fact]
        public void HerSeriTekMarkada()
        {
            var katalog = Katalog();

            var yinelenen = katalog.Markalar
                .SelectMany(m => m.Seriler.Select(s => new { Marka = m.Ad, Seri = s }))
                .GroupBy(x => x.Seri, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key + " -> " + string.Join(", ", g.Select(x => x.Marka)))
                .ToList();

            Assert.True(yinelenen.Count == 0, "Birden fazla markada geçen seri: " + string.Join(" | ", yinelenen));
        }

        [Fact]
        public void KatalogSurumTasir()
        {
            Assert.False(string.IsNullOrWhiteSpace(Katalog().Surum));
        }

        [Fact]
        public void MarkaVeSeriYazimiKatalogtanDuzeltilir()
        {
            var katalog = Katalog();

            Assert.Equal("Fiat", katalog.MarkaYazimi("fiat"));
            Assert.Equal("Egea", katalog.SeriYazimi("FIAT", "egea"));
            Assert.Null(katalog.MarkaYazimi("Olmayan Marka"));
            Assert.Null(katalog.SeriYazimi("Fiat", "Corolla"));
        }

        [Fact]
        public void SemaHatasindaYuklemeDurur()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "garajim-katalog-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(klasor);

            try
            {
                var yol = Path.Combine(klasor, AracKatalogu.DosyaAdi);

                File.WriteAllText(yol, "{\"surum\":\"1\",\"markalar\":[{\"ad\":\"A\",\"seriler\":[\"X\"]},{\"ad\":\"B\",\"seriler\":[\"X\"]}]}");
                Assert.Throws<InvalidOperationException>(() => AracKatalogu.Yukle(klasor));

                File.WriteAllText(yol, "{\"surum\":\"1\",\"markalar\":[{\"ad\":\"A\",\"seriler\":[]}]}");
                Assert.Throws<InvalidOperationException>(() => AracKatalogu.Yukle(klasor));

                File.WriteAllText(yol, "{\"markalar\":[{\"ad\":\"A\",\"seriler\":[\"X\"]}]}");
                Assert.Throws<InvalidOperationException>(() => AracKatalogu.Yukle(klasor));

                File.WriteAllText(yol, "{ bozuk json");
                Assert.Throws<InvalidOperationException>(() => AracKatalogu.Yukle(klasor));
            }
            finally
            {
                Directory.Delete(klasor, true);
            }
        }

        [Fact]
        public void KatalogDosyasiDepodaDuruyor()
        {
            Assert.True(File.Exists(Path.Combine(DepoKoku(), "Garajim.Business", "Katalog", AracKatalogu.DosyaAdi)));
        }
    }
}
