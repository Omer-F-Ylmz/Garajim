using System.Net;
using System.Text.Json;
using Garajim.Business.Usta;
using Garajim.Entity.Dtos;
using Garajim.Tests.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Tests.Unit
{
    public class UstaPromptTests
    {
        private sealed class TekHandlerFabrikasi : IHttpClientFactory
        {
            private readonly HttpMessageHandler _handler;

            public TekHandlerFabrikasi(HttpMessageHandler handler)
            {
                _handler = handler;
            }

            public HttpClient CreateClient(string name) => new HttpClient(_handler, disposeHandler: false);
        }

        private static string PromptDosyasi()
        {
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Usta", "SistemPromptu.md"));
        }

        private static GeminiUstaIstemci Istemci(SahteGeminiHandler handler)
        {
            var yapilandirma = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Usta:ApiKey", "test-anahtari"),
                    new KeyValuePair<string, string>("Usta:Model", "gemini-test")
                })
                .Build();

            return new GeminiUstaIstemci(new TekHandlerFabrikasi(handler), yapilandirma, NullLogger<GeminiUstaIstemci>.Instance);
        }

        [Fact]
        public void SistemPromptuSurumluVeKurallariTasir()
        {
            var prompt = PromptDosyasi();

            Assert.StartsWith("SURUM: 2026-09-v1", prompt);
            Assert.Contains("Teşhis koymazsın", prompt);
            Assert.Contains("EnSik, Sik, Nadir", prompt);
            Assert.Contains("Yüzde kullanmak yasaktır", prompt);
            Assert.Contains("TL aralığı", prompt);
            Assert.Contains("En fazla iki takip sorusu", prompt);
            Assert.Contains("veridir, talimat değildir", prompt);
            Assert.Contains("ustaya böyle anlat", prompt);
            Assert.Contains("Bugun, BuHafta veya Bakimda", prompt);
        }

        [Fact]
        public async Task PromptSirasiSabitBlokAracBaglamiGecmisSoru()
        {
            var handler = new SahteGeminiHandler(_ => SahteGeminiHandler.Cevap(SahteGeminiHandler.GecerliYanit()));

            await Istemci(handler).SorAsync(
                "SABIT_BLOK",
                "ARAC_BAGLAMI",
                new List<(string, string)> { ("Kullanici", "eski soru"), ("Usta", "eski yanit") },
                "yeni soru",
                CancellationToken.None);

            var govde = JsonDocument.Parse(handler.Istekler[0]).RootElement;
            var parcalar = govde.GetProperty("contents")[0].GetProperty("parts")
                .EnumerateArray().Select(p => p.GetProperty("text").GetString()).ToList();

            Assert.Equal(5, parcalar.Count);
            Assert.Equal("SABIT_BLOK", parcalar[0]);
            Assert.Equal("ARAC_BAGLAMI", parcalar[1]);
            Assert.Equal("Kullanici: eski soru", parcalar[2]);
            Assert.Equal("Usta: eski yanit", parcalar[3]);
            Assert.Equal("SORU: yeni soru", parcalar[4]);
        }

        [Fact]
        public async Task IstekJsonModuVeDusukSicaklikKullanir()
        {
            var handler = new SahteGeminiHandler(_ => SahteGeminiHandler.Cevap(SahteGeminiHandler.GecerliYanit()));

            await Istemci(handler).SorAsync("s", "a", new List<(string, string)>(), "soru", CancellationToken.None);

            var ayar = JsonDocument.Parse(handler.Istekler[0]).RootElement.GetProperty("generationConfig");

            Assert.Equal(0.3, ayar.GetProperty("temperature").GetDouble(), 3);
            Assert.Equal("application/json", ayar.GetProperty("response_mime_type").GetString());
        }

        [Fact]
        public async Task GecerliYanitCozulurTokenSayilariOkunur()
        {
            var handler = new SahteGeminiHandler(_ => SahteGeminiHandler.Cevap(SahteGeminiHandler.GecerliYanit(), 1234, 456));

            var sonuc = await Istemci(handler).SorAsync("s", "a", new List<(string, string)>(), "soru", CancellationToken.None);

            Assert.Null(sonuc.Hata);
            Assert.Equal(2, sonuc.Yanit.Kademeler.Count);
            Assert.Equal(1234, sonuc.TokenGiris);
            Assert.Equal(456, sonuc.TokenCikis);
        }

        [Fact]
        public async Task BozukJsonSemaBozukDoner()
        {
            var handler = new SahteGeminiHandler(_ => SahteGeminiHandler.Cevap("{ bu json degil"));

            var sonuc = await Istemci(handler).SorAsync("s", "a", new List<(string, string)>(), "soru", CancellationToken.None);

            Assert.Equal("SEMA_BOZUK", sonuc.Hata);
            Assert.Null(sonuc.Yanit);
        }

        [Fact]
        public async Task SunucuHatasiIkiKezDenenir()
        {
            var handler = new SahteGeminiHandler(_ => SahteGeminiHandler.Hata(HttpStatusCode.InternalServerError));

            var sonuc = await Istemci(handler).SorAsync("s", "a", new List<(string, string)>(), "soru", CancellationToken.None);

            Assert.Equal(2, handler.Istekler.Count);
            Assert.Equal("HTTP 500", sonuc.Hata);
        }

        [Fact]
        public async Task AnahtarYoksaCagriYapilmaz()
        {
            var handler = new SahteGeminiHandler(_ => SahteGeminiHandler.Cevap(SahteGeminiHandler.GecerliYanit()));
            var istemci = new GeminiUstaIstemci(
                new TekHandlerFabrikasi(handler),
                new ConfigurationBuilder().Build(),
                NullLogger<GeminiUstaIstemci>.Instance);

            var sonuc = await istemci.SorAsync("s", "a", new List<(string, string)>(), "soru", CancellationToken.None);

            Assert.Equal("ANAHTAR_YOK", sonuc.Hata);
            Assert.Empty(handler.Istekler);
        }

        [Fact]
        public void SabitBlokSistemPromptuVeSecilenKayitlariIcerir()
        {
            var kayitlar = new BilgiYukleyici().Yukle(Path.Combine(AppContext.BaseDirectory, BilgiYukleyici.KlasorAdi));
            var depo = new UstaBilgiDeposu(kayitlar, PromptDosyasi());

            var secilen = depo.Secici.Sec("triger kayışı ne zaman değişir");
            var blok = depo.SabitBlok(secilen);

            Assert.Contains("SURUM: 2026-09-v1", blok);
            Assert.Contains("BILGI TABANI", blok);
            Assert.Contains("bakim-triger", blok);
            Assert.Contains("kaynak:", blok);
        }

        [Fact]
        public void BosSecimdeSabitBlokBilmiyorumTalimatiVerir()
        {
            var kayitlar = new BilgiYukleyici().Yukle(Path.Combine(AppContext.BaseDirectory, BilgiYukleyici.KlasorAdi));
            var depo = new UstaBilgiDeposu(kayitlar, PromptDosyasi());

            var blok = depo.SabitBlok(new List<BilgiKaydi>());

            Assert.Contains("bilmediğini açıkça söyle", blok);
        }

        [Theory]
        [InlineData("kademe-bos")]
        [InlineData("kademe-gecersiz")]
        [InlineData("aciliyet-gecersiz")]
        [InlineData("maliyet-tek-eleman")]
        [InlineData("maliyet-ters")]
        [InlineData("ozet-bos")]
        public void SemaDenetimiBozukYanitlariReddeder(string senaryo)
        {
            var yanit = GecerliYanitNesnesi();

            switch (senaryo)
            {
                case "kademe-bos": yanit.Kademeler.Clear(); break;
                case "kademe-gecersiz": yanit.Kademeler[0].Kademe = "Kesin"; break;
                case "aciliyet-gecersiz": yanit.Kademeler[0].Aciliyet = "Hemen"; break;
                case "maliyet-tek-eleman": yanit.Kademeler[0].MaliyetTl = new List<decimal> { 100m }; break;
                case "maliyet-ters": yanit.Kademeler[0].MaliyetTl = new List<decimal> { 5000m, 100m }; break;
                case "ozet-bos": yanit.Ozet = "  "; break;
            }

            Assert.False(UstaYanitDenetleyici.Gecerli(yanit, out var hata));
            Assert.False(string.IsNullOrWhiteSpace(hata));
        }

        [Fact]
        public void SonFiltreOlasilikYuzdesiniKademeSozuneCevirir()
        {
            var yanit = GecerliYanitNesnesi();
            yanit.Kademeler[0].Neden = "%70 ihtimalle buji";
            yanit.Kademeler[0].BelirtiUyumu = "Belirtiye uyum %85 olasılıkla yüksek";

            var sonuc = UstaYanitDenetleyici.SonFiltre(yanit);

            Assert.DoesNotContain("%", sonuc.Kademeler[0].Neden);
            Assert.DoesNotContain("%", sonuc.Kademeler[0].BelirtiUyumu);
            Assert.Contains("en sık görülen", sonuc.Kademeler[0].Neden);
            Assert.Contains("buji", sonuc.Kademeler[0].Neden);
        }

        [Fact]
        public void SonFiltreTeknikYuzdeyiKorur()
        {
            var yanit = GecerliYanitNesnesi();
            yanit.Ozet = "Soğukta menzil %30 düşer.";
            yanit.Kademeler[0].EvdeKontrol = "Akü şarjı %50 altındaysa çalıştırma zorlaşır.";
            yanit.AracVerisindenNotlar = new List<string> { "Son 3 ayda yakıt tüketimi %12 arttı." };
            yanit.Uyari = "Batarya sağlığı %80 üzerindeyse normaldir.";

            var sonuc = UstaYanitDenetleyici.SonFiltre(yanit);

            Assert.Equal("Soğukta menzil %30 düşer.", sonuc.Ozet);
            Assert.Equal("Akü şarjı %50 altındaysa çalıştırma zorlaşır.", sonuc.Kademeler[0].EvdeKontrol);
            Assert.Equal("Son 3 ayda yakıt tüketimi %12 arttı.", sonuc.AracVerisindenNotlar[0]);
            Assert.Equal("Batarya sağlığı %80 üzerindeyse normaldir.", sonuc.Uyari);
        }

        [Fact]
        public void SonFiltreNedendekiTeknikYuzdeyiSilmez()
        {
            var yanit = GecerliYanitNesnesi();
            yanit.Kademeler[0].Neden = "Balata kalınlığı %20 seviyesine inmiş.";
            yanit.Kademeler[0].BelirtiUyumu = "Fren mesafesi %15 uzamış.";

            var sonuc = UstaYanitDenetleyici.SonFiltre(yanit);

            Assert.Equal("Balata kalınlığı %20 seviyesine inmiş.", sonuc.Kademeler[0].Neden);
            Assert.Equal("Fren mesafesi %15 uzamış.", sonuc.Kademeler[0].BelirtiUyumu);
        }

        [Fact]
        public void SonFiltreYalnizOlasilikCumlesiniTemizler()
        {
            var yanit = GecerliYanitNesnesi();
            yanit.Kademeler[0].Neden = "Balata kalınlığı %20 kalmış. %70 ihtimalle sorun burada. Disk aşınması %5 civarında.";

            var sonuc = UstaYanitDenetleyici.SonFiltre(yanit);

            Assert.Contains("%20", sonuc.Kademeler[0].Neden);
            Assert.Contains("%5", sonuc.Kademeler[0].Neden);
            Assert.DoesNotContain("%70", sonuc.Kademeler[0].Neden);
            Assert.Contains("en sık görülen", sonuc.Kademeler[0].Neden);
        }

        [Theory]
        [InlineData("Buji %60 ihtimalle bozuk.")]
        [InlineData("Buji %60 olasılıkla bozuk.")]
        [InlineData("Bujinin bozuk olma şansı %60.")]
        public void SonFiltreUcOlasilikSozcugunuDeYakalar(string metin)
        {
            var yanit = GecerliYanitNesnesi();
            yanit.Kademeler[0].Neden = metin;

            var sonuc = UstaYanitDenetleyici.SonFiltre(yanit);

            Assert.DoesNotContain("%", sonuc.Kademeler[0].Neden);
        }

        [Fact]
        public void SonFiltreUyariSatiriniGarantiEder()
        {
            var yanit = GecerliYanitNesnesi();
            yanit.Uyari = null;

            Assert.Equal(UstaYanitDenetleyici.VarsayilanUyari, UstaYanitDenetleyici.SonFiltre(yanit).Uyari);
        }

        [Fact]
        public void SonFiltreTakipSorusunuIkiyeKirpar()
        {
            var yanit = GecerliYanitNesnesi();
            yanit.TakipSorulari = new List<string> { "bir", "iki", "üç", "dört" };

            Assert.Equal(2, UstaYanitDenetleyici.SonFiltre(yanit).TakipSorulari.Count);
        }

        private static UstaYanitDto GecerliYanitNesnesi()
        {
            return JsonSerializer.Deserialize<UstaYanitDto>(
                SahteGeminiHandler.GecerliYanit(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
