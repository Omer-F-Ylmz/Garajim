using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class DocumentHttpTests : IDisposable
    {
        private static readonly byte[] PngBaslik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] PdfBaslik = { 0x25, 0x50, 0x44, 0x46 };

        private sealed class BelgeFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;
            private readonly long _kota;

            public BelgeFactory(string klasor, long kota)
            {
                _klasor = klasor;
                _kota = kota;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Documents:StoragePath"] = _klasor,
                        ["Documents:MaxFileSizeBytes"] = (1024 * 1024).ToString(),
                        ["Documents:CompanyQuotaBytes"] = _kota.ToString()
                    });
                });
            }
        }

        private readonly string _klasor;
        private readonly BelgeFactory _factory;

        public DocumentHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-belge-" + Guid.NewGuid().ToString("N"));
            _factory = new BelgeFactory(_klasor, 10 * 1024 * 1024);
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync(BelgeFactory factory = null)
        {
            var f = factory ?? _factory;
            var client = f.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Filo Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("driver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            var sifre = veri.GetProperty("temporaryPassword").GetString();
            var userId = veri.GetProperty("userId").GetInt32();

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = sifre });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, userId);
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2018,
                currentKm = 100000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static byte[] Dosya(byte[] baslik, int toplamBoyut)
        {
            var icerik = new byte[Math.Max(baslik.Length, toplamBoyut)];
            Array.Copy(baslik, icerik, baslik.Length);
            return icerik;
        }

        private static async Task<HttpResponseMessage> YukleAsync(HttpClient client, int vehicleId, byte[] icerik, string dosyaAdi)
        {
            using var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(icerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(dosya, "file", dosyaAdi);
            form.Add(new StringContent(vehicleId.ToString()), "vehicleId");
            return await client.PostAsync("/api/Documents", form);
        }

        [Fact]
        public async Task GecerliBelgeYuklenirVeIndirilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34BLG111");
            var icerik = Dosya(PngBaslik, 2048);

            var yukle = await YukleAsync(sahip, aracId, icerik, "ruhsat.png");
            var govde = await yukle.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, yukle.StatusCode);

            var belgeId = JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
            var indir = await sahip.GetAsync($"/api/Documents/{belgeId}/download");
            var inen = await indir.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.OK, indir.StatusCode);
            Assert.Equal(icerik.Length, inen.Length);
            Assert.Equal(PngBaslik, inen.Take(PngBaslik.Length).ToArray());

            var liste = await sahip.GetStringAsync($"/api/Documents?vehicleId={aracId}");
            Assert.Contains("ruhsat.png", liste);
        }

        [Fact]
        public async Task BoyutSiniriAsanBelgeReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34BLG222");
            var buyuk = Dosya(PngBaslik, 2 * 1024 * 1024);

            var cevap = await YukleAsync(sahip, aracId, buyuk, "buyuk.png");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Contains("boyut", govde, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UzantisiDegistirilmisDosyaReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34BLG333");
            var sahte = Encoding.ASCII.GetBytes("Bu aslinda duz metin, PNG degil.");

            var cevap = await YukleAsync(sahip, aracId, sahte, "sahte.png");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Contains("uyuşmuyor", govde, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task IzinsizUzantiReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34BLG444");

            var cevap = await YukleAsync(sahip, aracId, Dosya(PdfBaslik, 512), "zararli.exe");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Contains("uzantı", govde, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DizinAsmaDenemesiGuvenliSaklanir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34BLG555");

            var cevap = await YukleAsync(sahip, aracId, Dosya(PngBaslik, 1024), "../../../../evil.png");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            var veri = JsonDocument.Parse(govde).RootElement.GetProperty("data");
            Assert.Equal("evil.png", veri.GetProperty("originalName").GetString());

            var disaridaKalan = Directory.Exists(_klasor)
                ? Directory.GetFiles(Path.GetDirectoryName(_klasor.TrimEnd(Path.DirectorySeparatorChar)), "evil.png", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();
            Assert.Empty(disaridaKalan);
        }

        [Fact]
        public async Task BaskaSirketinBelgesineErisilemez()
        {
            var sahipA = await SahipOlusturAsync();
            var sahipB = await SahipOlusturAsync();
            var bArac = await AracEkleAsync(sahipB, "06BLG666");

            var yukle = await YukleAsync(sahipB, bArac, Dosya(PdfBaslik, 1024), "police.pdf");
            var belgeId = JsonDocument.Parse(await yukle.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var indir = await sahipA.GetAsync($"/api/Documents/{belgeId}/download");
            var sil = await sahipA.DeleteAsync($"/api/Documents/{belgeId}");

            Assert.Equal(HttpStatusCode.NotFound, indir.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, sil.StatusCode);
        }

        [Fact]
        public async Task SirketKotasiAsilincaReddedilir()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "garajim-kota-" + Guid.NewGuid().ToString("N"));
            using var darFactory = new BelgeFactory(klasor, 3 * 1024);
            var client = darFactory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("kota"), fullName = "Kota Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var aracId = await AracEkleAsync(client, "34KOT777");

            var ilk = await YukleAsync(client, aracId, Dosya(PngBaslik, 2048), "bir.png");
            var ikinci = await YukleAsync(client, aracId, Dosya(PngBaslik, 2048), "iki.png");
            var govde = await ikinci.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
            Assert.Contains("kota", govde, StringComparison.OrdinalIgnoreCase);

            if (Directory.Exists(klasor)) Directory.Delete(klasor, true);
        }

        [Fact]
        public async Task SurucuYalnizZimmetliAracaBelgeEkleyebilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34BLG888");
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);

            var zimmetsiz = await YukleAsync(surucu, aracId, Dosya(PngBaslik, 1024), "once.png");
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });
            var zimmetli = await YukleAsync(surucu, aracId, Dosya(PngBaslik, 1024), "sonra.png");

            Assert.Equal(HttpStatusCode.NotFound, zimmetsiz.StatusCode);
            Assert.Equal(HttpStatusCode.OK, zimmetli.StatusCode);
        }

        [Fact]
        public async Task BelgeSilinincaDosyaDaSilinir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34BLG999");
            var yukle = await YukleAsync(sahip, aracId, Dosya(PngBaslik, 1024), "silinecek.png");
            var belgeId = JsonDocument.Parse(await yukle.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var oncekiSayi = Directory.GetFiles(_klasor).Length;
            var sil = await sahip.DeleteAsync($"/api/Documents/{belgeId}");
            var sonrakiSayi = Directory.GetFiles(_klasor).Length;

            Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
            Assert.Equal(oncekiSayi - 1, sonrakiSayi);
            Assert.Equal(HttpStatusCode.NotFound, (await sahip.GetAsync($"/api/Documents/{belgeId}/download")).StatusCode);
        }

        [Fact]
        public async Task DosyalarWwwrootDisindaSaklanir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34BLG101");
            await YukleAsync(sahip, aracId, Dosya(PngBaslik, 1024), "gizli.png");

            var dosyalar = Directory.GetFiles(_klasor);

            Assert.NotEmpty(dosyalar);
            Assert.DoesNotContain("wwwroot", _klasor, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(dosyalar, d => Path.GetFileName(d) == "gizli.png");
        }

        public void Dispose()
        {
            _factory.Dispose();
            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }
    }
}
