using Garajim.Business.Usta;

namespace Garajim.Tests.Unit
{
    public class UstaBilgiTabaniTests
    {
        private static readonly IReadOnlyList<BilgiKaydi> Kayitlar =
            new BilgiYukleyici().Yukle(Path.Combine(AppContext.BaseDirectory, BilgiYukleyici.KlasorAdi));

        private static BilgiSecici Secici() => new BilgiSecici(Kayitlar);

        [Fact]
        public void BesDosyaninTamamiYuklenirVeElliKayitGelir()
        {
            Assert.Equal(50, Kayitlar.Count);
            Assert.Equal(5, Kayitlar.Select(k => k.Kategori).Distinct().Count() >= 5 ? 5 : Kayitlar.Select(k => k.Kategori).Distinct().Count());
            Assert.All(Kayitlar, k => Assert.False(string.IsNullOrWhiteSpace(k.Metin)));
            Assert.All(Kayitlar, k => Assert.NotEmpty(k.Anahtarlar));
            Assert.Equal(Kayitlar.Count, Kayitlar.Select(k => k.Id).Distinct().Count());
        }

        [Fact]
        public void EksikAlanliDosyaAcikMesajlaDurdurur()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "usta-bilgi-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(klasor);
            File.WriteAllText(Path.Combine(klasor, "bozuk.json"),
                "[{\"id\":\"x\",\"kategori\":\"Test\",\"anahtarlar\":[\"a\"],\"metin\":\"\",\"kaynak\":\"k\",\"guncelleme\":\"2026-01-01\"}]");

            var hata = Assert.Throws<InvalidOperationException>(() => new BilgiYukleyici().Yukle(klasor));

            Assert.Contains("bozuk.json", hata.Message);
            Assert.Contains("metin", hata.Message);

            Directory.Delete(klasor, true);
        }

        [Fact]
        public void BozukJsonAcikMesajlaDurdurur()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "usta-bilgi-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(klasor);
            File.WriteAllText(Path.Combine(klasor, "kirik.json"), "[{ bu json degil ]");

            var hata = Assert.Throws<InvalidOperationException>(() => new BilgiYukleyici().Yukle(klasor));

            Assert.Contains("kirik.json", hata.Message);

            Directory.Delete(klasor, true);
        }

        [Fact]
        public void TekrarEdenIdReddedilir()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "usta-bilgi-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(klasor);
            File.WriteAllText(Path.Combine(klasor, "tekrar.json"),
                "[{\"id\":\"a\",\"kategori\":\"T\",\"anahtarlar\":[\"x\"],\"metin\":\"m\",\"kaynak\":\"k\",\"guncelleme\":\"2026-01-01\"}," +
                "{\"id\":\"a\",\"kategori\":\"T\",\"anahtarlar\":[\"y\"],\"metin\":\"m\",\"kaynak\":\"k\",\"guncelleme\":\"2026-01-01\"}]");

            var hata = Assert.Throws<InvalidOperationException>(() => new BilgiYukleyici().Yukle(klasor));

            Assert.Contains("tekrar ediyor", hata.Message);

            Directory.Delete(klasor, true);
        }

        [Theory]
        [InlineData("P0300 kodu aldım", "P0300")]
        [InlineData("cihaz p0171 veriyor", "P0171")]
        [InlineData("Arıza kodu U0100 çıktı", "U0100")]
        [InlineData("B1234 ve C0035 kodları var", "B1234")]
        public void DtcKoduYakalanir(string soru, string beklenen)
        {
            Assert.Contains(beklenen, BilgiSecici.DtcKodlari(soru));
        }

        [Theory]
        [InlineData("P03000")]
        [InlineData("A0300")]
        [InlineData("P4300")]
        [InlineData("P030G")]
        public void GecersizKodYakalanmaz(string soru)
        {
            Assert.Empty(BilgiSecici.DtcKodlari(soru));
        }

        [Fact]
        public void DtcKoduDogrudanIlgiliKaydiGetirir()
        {
            var secilen = Secici().Sec("Aracın motor arıza lambası yandı, cihaz P0420 gösteriyor");

            Assert.Equal("obd-p0420", secilen[0].Id);
        }

        [Theory]
        [InlineData("motor yagi ne zaman degisir")]
        [InlineData("MOTOR YAĞI ne zaman değişir")]
        [InlineData("motor yagı, ne zaman değişir?")]
        public void TurkceKarakterVeNoktalamaDuyarsizEslesir(string soru)
        {
            var secilen = Secici().Sec(soru);

            Assert.Contains(secilen, k => k.Id == "bakim-yag-benzin");
        }

        [Fact]
        public void NormalizeTurkceKarakterleriKatlar()
        {
            Assert.Equal("sisli gunes cigi", BilgiSecici.Normalize("Şişli   Güneş, Çiği!"));
            Assert.Equal("kis lastigi", BilgiSecici.Normalize("KIŞ LASTİĞİ"));
        }

        [Fact]
        public void SecimEnFazlaYirmiBesKayitDoner()
        {
            var soru = string.Join(" ", Kayitlar.SelectMany(k => k.Anahtarlar));

            var secilen = Secici().Sec(soru);

            Assert.True(secilen.Count <= BilgiSecici.MaxKayit, $"25 sınırı aşıldı: {secilen.Count}");
        }

        [Fact]
        public void SecimTokenButcesiniAsmaz()
        {
            var soru = string.Join(" ", Kayitlar.SelectMany(k => k.Anahtarlar));

            var toplam = Secici().Sec(soru).Sum(k => BilgiSecici.TokenTahmini(k.Metin));

            Assert.True(toplam <= BilgiSecici.MaxToken, $"3.000 token bütçesi aşıldı: {toplam}");
        }

        [Fact]
        public void AlakasizSoruBosDoner()
        {
            Assert.Empty(Secici().Sec("bugün hava çok güzel yürüyüşe çıkalım"));
        }
    }
}
