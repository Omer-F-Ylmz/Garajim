using System.Net;
using System.Text;
using System.Text.Json;
using Garajim.Calibration;

namespace Garajim.Tests.Unit
{
    public class CalibrationTests
    {
        private const string OrnekCsv =
            "dosya;zorluk;tur;tarih;tutar;km;plaka;litre;aciklama\n" +
            "fis1.jpg;kolay;Yakıt;15.08.2026;1.484,36;123.456;34 abc 123;32,50;pompa fişi\n" +
            "fis2.jpg;orta;Bakım;01.07.2026;4.500,00;;;;servis\n" +
            "fis3.jpg;zor;Masraf;30.06.2026;350,00;;06DEF45;;otopark\n";

        private static string CsvYaz(string icerik)
        {
            var yol = Path.Combine(Path.GetTempPath(), "kalib-" + Guid.NewGuid().ToString("N") + ".csv");
            File.WriteAllText(yol, icerik, new UTF8Encoding(true));
            return yol;
        }

        [Fact]
        public void CsvUcSatiriTumAlanlarlaAyristirir()
        {
            var yol = CsvYaz(OrnekCsv);

            var satirlar = CevapAnahtari.Oku(yol);

            Assert.Equal(3, satirlar.Count);

            var ilk = satirlar[0];
            Assert.Equal("fis1.jpg", ilk.Dosya);
            Assert.Equal("kolay", ilk.Zorluk);
            Assert.Equal("Yakit", ilk.Tur);
            Assert.Equal(new DateTime(2026, 8, 15), ilk.Tarih);
            Assert.Equal(1484.36m, ilk.Tutar);
            Assert.Equal(123456, ilk.Km);
            Assert.Equal("34ABC123", ilk.Plaka);
            Assert.Equal(32.50m, ilk.Litre);

            File.Delete(yol);
        }

        [Fact]
        public void BosAlanlarNullOkunur()
        {
            var yol = CsvYaz(OrnekCsv);

            var ikinci = CevapAnahtari.Oku(yol)[1];

            Assert.Null(ikinci.Km);
            Assert.Null(ikinci.Plaka);
            Assert.Null(ikinci.Litre);
            Assert.Equal("Bakim", ikinci.Tur);

            File.Delete(yol);
        }

        [Theory]
        [InlineData("Yakıt", "Yakit")]
        [InlineData("yakit", "Yakit")]
        [InlineData("BAKIM", "Bakim")]
        [InlineData("Bakım", "Bakim")]
        [InlineData("masraf", "Masraf")]
        public void TurTurkceKarakterDuyarsizNormalizeEdilir(string girdi, string beklenen)
        {
            Assert.Equal(beklenen, CevapAnahtari.TuruNormalizeEt(girdi));
        }

        [Theory]
        [InlineData("1.484,36", 1484.36)]
        [InlineData("350,00", 350.0)]
        [InlineData("12", 12.0)]
        [InlineData("", null)]
        public void TurkSayiFormatiOkunur(string girdi, double? beklenen)
        {
            var sonuc = CevapAnahtari.SayiOku(girdi);
            if (beklenen == null)
            {
                Assert.Null(sonuc);
            }
            else
            {
                Assert.Equal((decimal)beklenen.Value, sonuc.Value);
            }
        }

        [Fact]
        public void TutarVeLitreVirgulOndalikToleransiylaKarsilastirilir()
        {
            Assert.True(Karsilastirici.OndalikEsit(1484.36m, 1484.365m));
            Assert.True(Karsilastirici.OndalikEsit(1484.36m, 1484.35m));
            Assert.False(Karsilastirici.OndalikEsit(1484.36m, 1484.40m));
            Assert.True(Karsilastirici.OndalikEsit(null, null));
            Assert.False(Karsilastirici.OndalikEsit(null, 1m));
        }

        [Fact]
        public void KmTamEslesmeliPlakaBoslukVeBuyukKucukDuyarsiz()
        {
            Assert.True(Karsilastirici.TamsayiEsit(123456, 123456));
            Assert.False(Karsilastirici.TamsayiEsit(123456, 123457));

            Assert.True(Karsilastirici.PlakaEsit("34 abc 123", "34ABC123"));
            Assert.True(Karsilastirici.PlakaEsit(null, null));
            Assert.False(Karsilastirici.PlakaEsit("34ABC123", "06DEF45"));
        }

        [Fact]
        public void AlanDogruluguToplamVeZorlugaGoreHesaplanir()
        {
            var sonuclar = new List<DosyaSonucu>
            {
                new DosyaSonucu { Dosya = "a.jpg", Zorluk = "kolay", AlanDogru = { ["tarih"] = true, ["tutar"] = true } },
                new DosyaSonucu { Dosya = "b.jpg", Zorluk = "zor", AlanDogru = { ["tarih"] = true, ["tutar"] = false } }
            };

            var rapor = Rapor.Olustur(sonuclar);

            Assert.Equal(100, rapor.AlanDogruluk["tarih"], 1);
            Assert.Equal(50, rapor.AlanDogruluk["tutar"], 1);
            Assert.Equal(100, rapor.ZorlukDogruluk["kolay"], 1);
            Assert.Equal(50, rapor.ZorlukDogruluk["zor"], 1);
        }

        private sealed class SahteHandler : HttpMessageHandler
        {
            public List<string> Yollar { get; } = new List<string>();
            public HttpStatusCode YuklemeDurumu { get; set; } = HttpStatusCode.OK;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var yol = request.RequestUri.AbsolutePath;
                Yollar.Add(request.Method + " " + yol);

                string govde;
                var durum = HttpStatusCode.OK;

                if (yol.EndsWith("/login"))
                {
                    govde = "{\"data\":{\"token\":\"sahte-token\"},\"success\":true}";
                }
                else if (yol == "/api/Receipts" && request.Method == HttpMethod.Post)
                {
                    durum = YuklemeDurumu;
                    govde = durum == HttpStatusCode.OK
                        ? "{\"data\":{\"taslakId\":7,\"durum\":\"Bekliyor\",\"taslak\":{\"id\":7,\"tarih\":\"2026-08-15T00:00:00\",\"toplamTutar\":1484.36,\"km\":123456,\"plaka\":\"34ABC123\",\"litre\":32.50,\"tahminiTur\":\"Yakit\",\"guvenSkoru\":0.9,\"sureMs\":120}},\"success\":true}"
                        : "{\"data\":null,\"success\":false,\"message\":\"limit\"}";
                }
                else if (yol.EndsWith("/confirm"))
                {
                    govde = "{\"data\":{\"id\":7},\"success\":true}";
                }
                else
                {
                    govde = "{\"data\":null,\"success\":true}";
                }

                return Task.FromResult(new HttpResponseMessage(durum)
                {
                    Content = new StringContent(govde, Encoding.UTF8, "application/json")
                });
            }
        }

        [Fact]
        public async Task UctanUcaAkisLoginYukleOkuOnaylaSirasiyla()
        {
            var handler = new SahteHandler();
            var istemci = new GarajimIstemci(new HttpClient(handler) { BaseAddress = new Uri("http://sahte.local") });
            await istemci.GirisYapAsync("a@b.c", "sifre");

            var taslak = await istemci.FisYukleAsync(new byte[] { 1, 2, 3 }, "fis1.jpg");
            await istemci.OnaylaAsync(taslak.TaslakId, 1, "Yakit", new DateTime(2026, 8, 15), 1484.36m, 123456, 32.50m);

            Assert.Equal(1484.36m, taslak.Taslak.ToplamTutar);
            Assert.Equal("34ABC123", taslak.Taslak.Plaka);
            Assert.Contains("POST /api/Auth/login", handler.Yollar);
            Assert.Contains("POST /api/Receipts", handler.Yollar);
            Assert.Contains("POST /api/Receipts/7/confirm", handler.Yollar);
        }

        [Fact]
        public async Task LimitAsimindaIstisnaFirlatilir()
        {
            var handler = new SahteHandler { YuklemeDurumu = HttpStatusCode.TooManyRequests };
            var istemci = new GarajimIstemci(new HttpClient(handler) { BaseAddress = new Uri("http://sahte.local") });
            await istemci.GirisYapAsync("a@b.c", "sifre");

            await Assert.ThrowsAsync<LimitAsildiException>(() => istemci.FisYukleAsync(new byte[] { 1 }, "fis.jpg"));
        }
    }
}
