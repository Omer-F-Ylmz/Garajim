using Garajim.Business.Concrete.Evraklar;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Unit
{
    public class EvrakKurallariTests
    {
        private static EvrakKurallari Kurallar(Dictionary<string, string> ayarlar = null)
        {
            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(ayarlar ?? new Dictionary<string, string>())
                .Build();
            return new EvrakKurallari(yapilandirma);
        }

        [Fact]
        public void HususiMuayeneIkiYilSonrayiOnerir()
        {
            var sonuc = Kurallar().SonrakiTarih(EvrakTuru.Muayene, KullanimTuru.Hususi, new DateTime(2026, 5, 20), null);

            Assert.Equal(new DateTime(2028, 5, 20), sonuc);
        }

        [Fact]
        public void TicariMuayeneBirYilSonrayiOnerir()
        {
            var sonuc = Kurallar().SonrakiTarih(EvrakTuru.Muayene, KullanimTuru.Ticari, new DateTime(2026, 5, 20), null);

            Assert.Equal(new DateTime(2027, 5, 20), sonuc);
        }

        [Fact]
        public void IlkMuayeneTescildenUcYilSonradir()
        {
            var sonuc = Kurallar().SonrakiTarih(EvrakTuru.Muayene, KullanimTuru.Hususi, null, new DateTime(2026, 3, 10));

            Assert.Equal(new DateTime(2029, 3, 10), sonuc);
        }

        [Fact]
        public void TicariAracinIlkMuayenesiDeTescildenUcYilSonradir()
        {
            var sonuc = Kurallar().SonrakiTarih(EvrakTuru.Muayene, KullanimTuru.Ticari, null, new DateTime(2026, 3, 10));

            Assert.Equal(new DateTime(2029, 3, 10), sonuc);
        }

        [Theory]
        [InlineData(EvrakTuru.TrafikSigortasi)]
        [InlineData(EvrakTuru.Kasko)]
        [InlineData(EvrakTuru.Ehliyet)]
        [InlineData(EvrakTuru.SRC)]
        [InlineData(EvrakTuru.Psikoteknik)]
        public void YillikEvraklarBirYilSonrayiOnerir(EvrakTuru tur)
        {
            var sonuc = Kurallar().SonrakiTarih(tur, KullanimTuru.Hususi, new DateTime(2026, 5, 20), null);

            Assert.Equal(new DateTime(2027, 5, 20), sonuc);
        }

        [Fact]
        public void EgzozHususiIkiYilTicariBirYil()
        {
            Assert.Equal(new DateTime(2028, 5, 20), Kurallar().SonrakiTarih(EvrakTuru.EgzozEmisyon, KullanimTuru.Hususi, new DateTime(2026, 5, 20), null));
            Assert.Equal(new DateTime(2027, 5, 20), Kurallar().SonrakiTarih(EvrakTuru.EgzozEmisyon, KullanimTuru.Ticari, new DateTime(2026, 5, 20), null));
        }

        [Fact]
        public void ArtikYilYirmiDokuzSubatBirYilSonraYirmiSekiziOlur()
        {
            var sonuc = Kurallar().SonrakiTarih(EvrakTuru.TrafikSigortasi, KullanimTuru.Hususi, new DateTime(2028, 2, 29), null);

            Assert.Equal(new DateTime(2029, 2, 28), sonuc);
        }

        [Fact]
        public void YilSonuTarihiDogruKayar()
        {
            var sonuc = Kurallar().SonrakiTarih(EvrakTuru.Kasko, KullanimTuru.Hususi, new DateTime(2026, 12, 31), null);

            Assert.Equal(new DateTime(2027, 12, 31), sonuc);
        }

        [Fact]
        public void KisLastigiVarsayilanPencereyiKullanir()
        {
            var pencere = Kurallar().KisLastigiPenceresi();

            Assert.Equal(15, pencere.BaslangicGun);
            Assert.Equal(11, pencere.BaslangicAy);
            Assert.Equal(15, pencere.BitisGun);
            Assert.Equal(4, pencere.BitisAy);
        }

        [Fact]
        public void KisLastigiPenceresiConfigdenEzilir()
        {
            var pencere = Kurallar(new Dictionary<string, string> { ["Evrak:KisLastigi"] = "15-11..15-03" }).KisLastigiPenceresi();

            Assert.Equal(15, pencere.BaslangicGun);
            Assert.Equal(11, pencere.BaslangicAy);
            Assert.Equal(15, pencere.BitisGun);
            Assert.Equal(3, pencere.BitisAy);
        }

        [Theory]
        [InlineData(2026, 11, 14, false)]
        [InlineData(2026, 11, 15, true)]
        [InlineData(2026, 11, 16, true)]
        [InlineData(2026, 12, 15, true)]
        [InlineData(2027, 1, 20, true)]
        [InlineData(2027, 4, 14, true)]
        [InlineData(2027, 4, 15, true)]
        [InlineData(2027, 4, 16, false)]
        [InlineData(2026, 7, 15, false)]
        [InlineData(2026, 10, 31, false)]
        public void KisLastigiPenceresiYilAsanAraligiDogruKapsar(int yil, int ay, int gun, bool beklenen)
        {
            Assert.Equal(beklenen, Kurallar().KisLastigiDonemindeMi(new DateTime(yil, ay, gun)));
        }

        [Theory]
        [InlineData(2026, 10, 14, false)]
        [InlineData(2026, 10, 15, true)]
        [InlineData(2027, 5, 15, true)]
        [InlineData(2027, 5, 16, false)]
        public void ValilikUzatmasiConfigEzmesiyleKarsilanir(int yil, int ay, int gun, bool beklenen)
        {
            var kurallar = Kurallar(new Dictionary<string, string> { ["Evrak:KisLastigi"] = "15-10..15-05" });

            Assert.Equal(beklenen, kurallar.KisLastigiDonemindeMi(new DateTime(yil, ay, gun)));
        }

        [Fact]
        public void KisLastigiPenceresiMetniTurkceKisaltmaKullanir()
        {
            Assert.Equal("15 Kas–15 Nis", Kurallar().KisLastigiPenceresiMetni());
        }

        [Fact]
        public void PencereMetniConfigEzmesiniYansitir()
        {
            var kurallar = Kurallar(new Dictionary<string, string> { ["Evrak:KisLastigi"] = "15-10..15-05" });

            Assert.Equal("15 Eki–15 May", kurallar.KisLastigiPenceresiMetni());
        }


        [Fact]
        public void KisLastigiSonrakiTarihiPencereBitisinaKurulur()
        {
            var sonuc = Kurallar().SonrakiTarih(EvrakTuru.KisLastigi, KullanimTuru.Ticari, new DateTime(2026, 12, 10), null);

            Assert.Equal(new DateTime(2027, 4, 15), sonuc);
        }

        [Fact]
        public void BozukPencereDegeriVarsayilanaDuser()
        {
            var pencere = Kurallar(new Dictionary<string, string> { ["Evrak:KisLastigi"] = "saçma" }).KisLastigiPenceresi();

            Assert.Equal(15, pencere.BaslangicGun);
            Assert.Equal(11, pencere.BaslangicAy);
            Assert.Equal(4, pencere.BitisAy);
        }

        [Fact]
        public void UyariGunleriConfigdenOkunurVarsayilanOtuzVeYedi()
        {
            Assert.Equal(new[] { 30, 7 }, Kurallar().UyariGunleri());
            Assert.Equal(new[] { 60, 14, 1 }, Kurallar(new Dictionary<string, string> { ["Evrak:UyariGunleri"] = "60, 14, 1" }).UyariGunleri());
        }

        [Theory]
        [InlineData(-1, "Gecti")]
        [InlineData(0, "Yaklasiyor")]
        [InlineData(30, "Yaklasiyor")]
        [InlineData(31, "Iyi")]
        public void DurumBitisTarihineGoreHesaplanir(int kalanGun, string beklenen)
        {
            var bugun = new DateTime(2026, 6, 1);

            Assert.Equal(beklenen, EvrakKurallari.Durum(bugun.AddDays(kalanGun), bugun));
        }
    }
}
