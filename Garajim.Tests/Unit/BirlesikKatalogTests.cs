using System.Text.Json;
using Garajim.Business.Katalog;

namespace Garajim.Tests.Unit
{
    public class BirlesikKatalogTests : IDisposable
    {
        private readonly string _klasor;

        public BirlesikKatalogTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-katalog-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_klasor);

            File.Copy(TrKatalogYolu(), Path.Combine(_klasor, AracKatalogu.DosyaAdi));
        }

        public void Dispose()
        {
            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }

        private static string DepoKoku()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return kok.FullName;
        }

        private static string TrKatalogYolu() =>
            Path.Combine(DepoKoku(), "Garajim.Business", "Katalog", AracKatalogu.DosyaAdi);

        private void GlobalYaz(string icerik) =>
            File.WriteAllText(Path.Combine(_klasor, AracKatalogu.GlobalDosyaAdi), icerik);

        private const string OrnekGlobal = @"{
  ""surum"": ""test-g1"",
  ""kaynak"": ""test"",
  ""uretimTarihi"": ""2026-09-08"",
  ""markalar"": [
    { ""ad"": ""Tesla"", ""tr"": true, ""seriler"": [ { ""ad"": ""Model 3"", ""tr"": true }, { ""ad"": ""Cybertruck"", ""tr"": false } ] },
    { ""ad"": ""Land Rover"", ""tr"": false, ""seriler"": [ { ""ad"": ""Defender"", ""tr"": false } ] }
  ]
}";

        // ---------- Global dosya yokken ----------

        [Fact]
        public void GlobalDosyaYokkenTrKatalogYuklenir()
        {
            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.Equal(56, katalog.Markalar.Count);
            Assert.True(katalog.MarkaVar("Fiat"));
            Assert.True(katalog.SeriVar("Fiat", "Egea"));
        }

        [Fact]
        public void GlobalDosyaYokkenHerSeyTrSayilir()
        {
            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.All(katalog.Markalar, m => Assert.True(katalog.TrMarkaMi(m.Ad), m.Ad + " tr değil"));
            Assert.True(katalog.TrSeriMi("Tesla", "Model 3"));
        }

        [Fact]
        public void GlobalDosyaYokkenGlobalSurumBostur()
        {
            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.Null(katalog.GlobalSurum);
            Assert.False(string.IsNullOrWhiteSpace(katalog.Surum));
        }

        // ---------- Global dosya varken ----------

        [Fact]
        public void GlobalDosyaVarkenMarkalarBirlesir()
        {
            GlobalYaz(OrnekGlobal);

            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.Equal(57, katalog.Markalar.Count);
            Assert.True(katalog.MarkaVar("Land Rover"));
            Assert.True(katalog.MarkaVar("Fiat"));
        }

        [Fact]
        public void GlobalSeriTrOlmayanOlarakIsaretlenir()
        {
            GlobalYaz(OrnekGlobal);

            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.True(katalog.SeriVar("Tesla", "Cybertruck"));
            Assert.False(katalog.TrSeriMi("Tesla", "Cybertruck"));
            Assert.True(katalog.TrSeriMi("Tesla", "Model 3"));
        }

        [Fact]
        public void GlobalMarkaTrOlmayanOlarakIsaretlenir()
        {
            GlobalYaz(OrnekGlobal);

            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.False(katalog.TrMarkaMi("Land Rover"));
            Assert.True(katalog.TrMarkaMi("Tesla"));
        }

        [Fact]
        public void GlobalSurumOkunur()
        {
            GlobalYaz(OrnekGlobal);

            Assert.Equal("test-g1", AracKatalogu.Yukle(_klasor).GlobalSurum);
        }

        [Fact]
        public void GlobalAyniSeriyiFarkliMarkalardaKabulEder()
        {
            GlobalYaz(@"{
  ""surum"": ""test-g2"", ""kaynak"": ""test"", ""uretimTarihi"": ""2026-09-08"",
  ""markalar"": [
    { ""ad"": ""Land Rover"", ""tr"": false, ""seriler"": [ { ""ad"": ""Sport"", ""tr"": false } ] },
    { ""ad"": ""GMC"", ""tr"": false, ""seriler"": [ { ""ad"": ""Sport"", ""tr"": false } ] }
  ]
}");

            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.True(katalog.SeriVar("Land Rover", "Sport"));
            Assert.True(katalog.SeriVar("GMC", "Sport"));
        }

        [Fact]
        public void GlobalTrKatalogunuEzmez()
        {
            GlobalYaz(@"{
  ""surum"": ""test-g3"", ""kaynak"": ""test"", ""uretimTarihi"": ""2026-09-08"",
  ""markalar"": [ { ""ad"": ""Fiat"", ""tr"": false, ""seriler"": [ { ""ad"": ""Egea"", ""tr"": false } ] } ]
}");

            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.True(katalog.TrMarkaMi("Fiat"));
            Assert.True(katalog.TrSeriMi("Fiat", "Egea"));
        }

        [Fact]
        public void BozukGlobalDosyaYuklemeyiDusurur()
        {
            GlobalYaz("{ bozuk json");

            Assert.Throws<InvalidOperationException>(() => AracKatalogu.Yukle(_klasor));
        }

        [Fact]
        public void GlobalSurumsuzDosyaYuklemeyiDusurur()
        {
            GlobalYaz(@"{ ""markalar"": [ { ""ad"": ""X Marka"", ""tr"": false, ""seriler"": [ { ""ad"": ""Y"", ""tr"": false } ] } ] }");

            Assert.Throws<InvalidOperationException>(() => AracKatalogu.Yukle(_klasor));
        }

        // ---------- Arama ----------

        [Fact]
        public void MarkaAramasiTurkceDuyarsizdir()
        {
            var katalog = AracKatalogu.Yukle(_klasor);

            var sonuc = katalog.MarkaAra("sko", 50);

            Assert.Contains("Skoda", sonuc.Adlar);
        }

        [Fact]
        public void MarkaAramasiOnekiOneAlir()
        {
            var katalog = AracKatalogu.Yukle(_klasor);

            var sonuc = katalog.MarkaAra("re", 50);

            Assert.Contains("Renault", sonuc.Adlar);

            var sonOnek = sonuc.Adlar.FindLastIndex(a => a.StartsWith("Re", StringComparison.OrdinalIgnoreCase));
            var ilkIceren = sonuc.Adlar.FindIndex(a => !a.StartsWith("Re", StringComparison.OrdinalIgnoreCase));

            Assert.True(ilkIceren < 0 || sonOnek < ilkIceren, "onek eslesmeleri basta olmali");
        }

        [Fact]
        public void MarkaAramasiTrGrubunuOneAlir()
        {
            GlobalYaz(OrnekGlobal);

            var katalog = AracKatalogu.Yukle(_klasor);
            var sonuc = katalog.MarkaAra("la", 50);

            var trIlk = sonuc.Adlar.FindIndex(a => katalog.TrMarkaMi(a));
            var globalIlk = sonuc.Adlar.FindIndex(a => !katalog.TrMarkaMi(a));

            Assert.True(globalIlk < 0 || trIlk < globalIlk, "TR grubu başta olmalı");
        }

        [Fact]
        public void MarkaAramasiSayfalar()
        {
            var katalog = AracKatalogu.Yukle(_klasor);

            var sonuc = katalog.MarkaAra(null, 10);

            Assert.Equal(10, sonuc.Kayitlar.Count);
            Assert.Equal(56, sonuc.Toplam);
            Assert.True(sonuc.DahaVar);
        }

        [Fact]
        public void SeriAramasiMarkaIcindeSuzer()
        {
            var katalog = AracKatalogu.Yukle(_klasor);

            var sonuc = katalog.SeriAra("Fiat", "eg", 50);

            Assert.Contains("Egea", sonuc.Adlar);
            Assert.DoesNotContain("Egea", katalog.SeriAra("Renault", "eg", 50).Adlar);
        }

        [Fact]
        public void SeriAramasiTrGrubunuOneAlir()
        {
            GlobalYaz(OrnekGlobal);

            var katalog = AracKatalogu.Yukle(_klasor);
            var sonuc = katalog.SeriAra("Tesla", null, 50);

            Assert.Equal("Model 3", sonuc.Adlar.First());
            Assert.Contains("Cybertruck", sonuc.Adlar);
        }

        [Fact]
        public void AramaBilinmeyenMarkadaBosDoner()
        {
            var katalog = AracKatalogu.Yukle(_klasor);

            Assert.Empty(katalog.SeriAra("Yok Boyle Marka", null, 50).Kayitlar);
        }

        // ---------- Sözlük eşitliği yalnız TR alt kümesinde ----------

        [Fact]
        public void GlobalGirdilerSozlukEsitliginiBozmaz()
        {
            GlobalYaz(OrnekGlobal);

            var katalog = AracKatalogu.Yukle(_klasor);

            var trMarkalar = katalog.Markalar.Where(m => katalog.TrMarkaMi(m.Ad)).Select(m => m.Ad).ToList();

            Assert.Equal(56, trMarkalar.Count);

            var trSeriler = katalog.Markalar
                .SelectMany(m => katalog.Seriler(m.Ad).Where(s => katalog.TrSeriMi(m.Ad, s)))
                .ToList();

            Assert.Equal(391, trSeriler.Count);
        }

        [Fact]
        public void GlobalBelgeSemasiUreticininYazdigiylaUyumlu()
        {
            GlobalYaz(OrnekGlobal);

            using var belge = JsonDocument.Parse(OrnekGlobal);
            var kok = belge.RootElement;

            Assert.True(kok.TryGetProperty("surum", out _));
            Assert.True(kok.TryGetProperty("kaynak", out _));
            Assert.True(kok.TryGetProperty("uretimTarihi", out _));
            Assert.True(kok.TryGetProperty("markalar", out _));

            Assert.NotNull(AracKatalogu.Yukle(_klasor).GlobalSurum);
        }
    }
}
