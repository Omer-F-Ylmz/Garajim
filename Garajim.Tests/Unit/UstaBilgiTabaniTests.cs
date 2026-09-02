using Garajim.Business.Usta;

namespace Garajim.Tests.Unit
{
    public class UstaBilgiTabaniTests
    {
        private static readonly IReadOnlyList<BilgiKaydi> Kayitlar =
            new BilgiYukleyici().Yukle(Path.Combine(AppContext.BaseDirectory, BilgiYukleyici.KlasorAdi));

        private static BilgiSecici Secici() => new BilgiSecici(Kayitlar);

        [Fact]
        public void BesDosyaninTamamiYuklenirVeSemayaUyar()
        {
            var dosyalar = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, BilgiYukleyici.KlasorAdi), "*.json");

            Assert.Equal(5, dosyalar.Length);
            Assert.True(Kayitlar.Count >= 50, $"Bilgi tabanı beklenenden küçük: {Kayitlar.Count}");
            Assert.All(Kayitlar, k => Assert.False(string.IsNullOrWhiteSpace(k.Metin)));
            Assert.All(Kayitlar, k => Assert.False(string.IsNullOrWhiteSpace(k.Kategori)));
            Assert.All(Kayitlar, k => Assert.False(string.IsNullOrWhiteSpace(k.Kaynak)));
            Assert.All(Kayitlar, k => Assert.NotEmpty(k.Anahtarlar));
            Assert.Equal(Kayitlar.Count, Kayitlar.Select(k => k.Id).Distinct().Count());
        }

        [Fact]
        public void HerDosyaEnAzOnKayitTasir()
        {
            var klasor = Path.Combine(AppContext.BaseDirectory, BilgiYukleyici.KlasorAdi);

            foreach (var dosya in Directory.GetFiles(klasor, "*.json"))
            {
                var tekDosya = System.Text.Json.JsonSerializer.Deserialize<List<BilgiKaydi>>(
                    File.ReadAllText(dosya),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Assert.True(tekDosya.Count >= 10, $"{Path.GetFileName(dosya)} on kayittan az: {tekDosya.Count}");
            }
        }

        [Fact]
        public void FrenMetalikSesiBelirtiKaydiniGetirir()
        {
            var secilen = Secici().Sec("Frende metalik ses geliyor");

            Assert.Contains(secilen, k => k.Id == "blr-001");
            Assert.Equal("blr-001", secilen[0].Id);
            Assert.Contains(secilen[0].Anahtarlar, a => a.Contains("metalik ses"));
        }

        [Fact]
        public void MuayeneSinyalRengiTuvturkKaydiniGetirir()
        {
            var secilen = Secici().Sec("muayeneden kaldım sinyal rengi");

            Assert.Contains(secilen, k => k.Id == "tvt-005");
            Assert.Contains(secilen, k => k.Kategori == "tuvturk");
        }

        [Fact]
        public void IkiSoruBirbirininKayitlariniGetirmez()
        {
            var fren = Secici().Sec("Frende metalik ses geliyor").Select(k => k.Id).ToList();
            var muayene = Secici().Sec("muayeneden kaldım sinyal rengi").Select(k => k.Id).ToList();

            Assert.DoesNotContain("tvt-005", fren);
            Assert.DoesNotContain("blr-001", muayene);
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

            Assert.Equal("obd-P0420", secilen[0].Id);
        }

        [Theory]
        [InlineData("periyodik bakim ne zaman")]
        [InlineData("PERİYODİK BAKIM ne zaman")]
        [InlineData("periyodik bakım, ne zaman?")]
        public void TurkceKarakterVeNoktalamaDuyarsizEslesir(string soru)
        {
            var secilen = Secici().Sec(soru);

            Assert.Contains(secilen, k => k.Id == BilgiSecici.BakimKuralKaydi);
        }

        [Fact]
        public void DtcKoduYeniSemadaKaydiGetirir()
        {
            var secilen = Secici().Sec("P0420 kodu çıktı");

            Assert.Contains(secilen, k => k.Id == "obd-P0420");
        }

        [Fact]
        public void TrigerSorusuBakimKaydiniGetirir()
        {
            var secilen = Secici().Sec("egea 1.4 triger ne zaman");

            Assert.Contains(secilen, k => k.Id == "bkm-001");
        }

        [Fact]
        public void KisLastigiSorusuTurkiyeKaydiniGetirir()
        {
            var secilen = Secici().Sec("kış lastiği zorunlu mu");

            Assert.Contains(secilen, k => k.Id == "tro-001");
        }

        [Fact]
        public void BakimKaydiSecilinceKuralKaydiHerZamanEklenir()
        {
            var secilen = Secici().Sec("egea 1.4 triger ne zaman");

            Assert.Contains(secilen, k => k.Kategori == BilgiSecici.BakimKategorisi && k.Id != BilgiSecici.BakimKuralKaydi);
            Assert.Contains(secilen, k => k.Id == BilgiSecici.BakimKuralKaydi);
            Assert.Equal(BilgiSecici.BakimKuralKaydi, secilen[0].Id);
        }

        [Fact]
        public void KuralKaydiButceDolsaBileEklenir()
        {
            var secici = Secici();
            var soru = string.Join(" ", Kayitlar
                .Where(k => k.Kategori == BilgiSecici.BakimKategorisi)
                .SelectMany(k => k.Anahtarlar));

            var secilen = secici.Sec(soru);
            var token = secilen.Sum(k => BilgiSecici.TokenTahmini(k.Metin));

            Assert.Contains(secilen, k => k.Id == BilgiSecici.BakimKuralKaydi);
            Assert.True(token > BilgiSecici.MaxToken - BilgiSecici.TokenTahmini(
                Kayitlar.Single(k => k.Id == BilgiSecici.BakimKuralKaydi).Metin) || secilen.Count > 1);
        }

        [Theory]
        [InlineData("triger kayışı kaç kilometrede değişir")]
        [InlineData("yağ ne zaman değişir")]
        public void GenelBakimSorusuKuralKaydiniBirinciSiradaGetirir(string soru)
        {
            var secilen = Secici().Sec(soru);

            Assert.NotEmpty(secilen);
            Assert.Equal(BilgiSecici.BakimKuralKaydi, secilen[0].Id);
        }

        [Fact]
        public void BakimDisiSorudaKuralKaydiEklenmez()
        {
            var secilen = Secici().Sec("P0420 kodu çıktı");

            Assert.DoesNotContain(secilen, k => k.Id == BilgiSecici.BakimKuralKaydi);
        }

        [Fact]
        public void KuralKaydiTekrarEtmez()
        {
            var secilen = Secici().Sec("bakım aralığı periyodik bakım tablosu");

            Assert.Single(secilen.Where(k => k.Id == BilgiSecici.BakimKuralKaydi));
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
