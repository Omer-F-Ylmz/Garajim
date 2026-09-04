using Garajim.Business.Katalog;
using Garajim.Business.Usta;

namespace Garajim.Tests.Unit
{
    public class YardimSssTests
    {
        private static string Klasor()
        {
            return Path.Combine(AppContext.BaseDirectory, YardimSss.KlasorAdi);
        }

        private static IReadOnlyList<SssKaydi> Yukle() => YardimSss.Yukle(Klasor());

        [Fact]
        public void YirmiBesTenFazlaSoruVardirVeIdlerTekildir()
        {
            var kayitlar = Yukle();

            Assert.True(kayitlar.Count >= 25, "SSS en az 25 kayıt taşımalı, şu an: " + kayitlar.Count);
            Assert.Equal(kayitlar.Count, kayitlar.Select(k => k.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void HerKayitBaslikCevapVeAnahtarTasir()
        {
            foreach (var kayit in Yukle())
            {
                Assert.False(string.IsNullOrWhiteSpace(kayit.Baslik), kayit.Id);
                Assert.False(string.IsNullOrWhiteSpace(kayit.Cevap), kayit.Id);
                Assert.NotEmpty(kayit.Anahtarlar);
                Assert.All(kayit.Anahtarlar, a => Assert.False(string.IsNullOrWhiteSpace(a)));
            }
        }

        [Fact]
        public void ZorunluKonularKapsanir()
        {
            var metin = string.Join(" ", Yukle().Select(k => k.Baslik + " " + k.Cevap + " " + string.Join(" ", k.Anahtarlar)));

            foreach (var konu in new[]
            {
                "doğrulama", "oto onay", "tam dolum", "parça hafızası", "muayene",
                "karne", "acil", "arşiv", "zimmet", "geçiş sihirbazı",
                "hesab", "KVKK", "20", "yakında"
            })
            {
                Assert.Contains(konu, metin, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void BozukSemadaHataFirlatir()
        {
            var gecici = Path.Combine(Path.GetTempPath(), "sss-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(gecici);
            File.WriteAllText(Path.Combine(gecici, YardimSss.DosyaAdi), "[{\"id\":\"a\",\"baslik\":\"\",\"cevap\":\"x\",\"anahtarlar\":[\"y\"]}]");

            Assert.Throws<InvalidOperationException>(() => YardimSss.Yukle(gecici));

            Directory.Delete(gecici, true);
        }

        [Fact]
        public void BilgiTabanindaUygulamaKullanimKategorisiOlur()
        {
            var kayitlar = YardimSss.BilgiKayitlari(Yukle());

            Assert.All(kayitlar, k => Assert.Equal(YardimSss.Kategori, k.Kategori));
            Assert.All(kayitlar, k => Assert.False(string.IsNullOrWhiteSpace(k.Kaynak)));
            Assert.All(kayitlar, k => Assert.True(DateTime.TryParse(k.Guncelleme, out _)));
        }

        [Fact]
        public void KarneSorusuSssKaydiniSecer()
        {
            var secici = new BilgiSecici(YardimSss.BilgiKayitlari(Yukle()));

            var secilen = secici.Sec("karneyi nasıl paylaşırım");

            Assert.Contains(secilen, k => k.Metin.Contains("karne", StringComparison.OrdinalIgnoreCase));
        }
    }
}
