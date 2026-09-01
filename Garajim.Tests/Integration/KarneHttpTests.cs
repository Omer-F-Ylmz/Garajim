using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class KarneHttpTests : IDisposable
    {
        private sealed class KarneFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;

            public KarneFactory(string klasor)
            {
                _klasor = klasor;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Documents:StoragePath"] = _klasor,
                    ["App:BaseUrl"] = "https://ornek.garajim.app"
                }));
            }
        }

        private static readonly byte[] PngIcerik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

        private readonly string _klasor;
        private readonly KarneFactory _factory;

        public KarneHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-karne-" + Guid.NewGuid().ToString("N"));
            _factory = new KarneFactory(_klasor);
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Karne Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("driver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Karne Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 120000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static async Task BakimEkleAsync(HttpClient client, int aracId)
        {
            await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = "2026-06-01",
                km = 118000,
                cost = 4200m,
                serviceName = "Yetkili Servis",
                note = "",
                parcalar = new object[]
                {
                    new { parcaTuru = "MotorYagi", aciklama = "5W30", adet = 1, tutar = 1800m, marka = (string)null }
                }
            });
        }

        private static async Task<int> BelgeEkleAsync(HttpClient client, int aracId)
        {
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(PngIcerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(dosya, "file", "ruhsat.png");
            form.Add(new StringContent(aracId.ToString()), "vehicleId");
            var cevap = await client.PostAsync("/api/Documents", form);
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static Task<HttpResponseMessage> KarneOlusturAsync(HttpClient client, int aracId, object kapsam, int? sonKullanmaGun = null)
        {
            return client.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new { kapsam, sonKullanmaGun });
        }

        private static object TamKapsam => new
        {
            bakimGecmisi = true,
            parcaHafizasi = true,
            yakitOzeti = true,
            belgeler = true,
            plakaGoster = true,
            tutarGoster = true
        };

        [Fact]
        public async Task TokenYalnizKendiAracininKarnesiniVerir()
        {
            var birinci = await SahipOlusturAsync();
            var birinciArac = await AracEkleAsync(birinci, "34KRN001");
            await BakimEkleAsync(birinci, birinciArac);

            var ikinci = await SahipOlusturAsync();
            var ikinciArac = await AracEkleAsync(ikinci, "06KRN002");
            await BakimEkleAsync(ikinci, ikinciArac);

            var birinciKarne = await VeriAsync(await KarneOlusturAsync(birinci, birinciArac, TamKapsam));
            var token = TokenAl(birinciKarne.GetProperty("url").GetString());

            var anonim = _factory.CreateClient();
            var karne = await VeriAsync(await anonim.GetAsync($"/api/karne/{token}"));

            Assert.Equal("34KRN001", karne.GetProperty("arac").GetProperty("plaka").GetString());
            Assert.Equal(1, karne.GetProperty("bakimlar").GetArrayLength());
        }

        [Fact]
        public async Task PasifVeSuresiDolmusKarne404Doner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34KRN003");

            var karne = await VeriAsync(await KarneOlusturAsync(sahip, aracId, TamKapsam));
            var token = TokenAl(karne.GetProperty("url").GetString());

            var anonim = _factory.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await anonim.GetAsync($"/api/karne/{token}")).StatusCode);

            await sahip.DeleteAsync($"/api/Vehicles/{aracId}/karne");

            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync($"/api/karne/{token}")).StatusCode);
        }

        [Fact]
        public async Task KapsamKapaliAlanYanittaYok()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34KRN004");
            await BakimEkleAsync(sahip, aracId);
            var belgeId = await BelgeEkleAsync(sahip, aracId);

            var karne = await VeriAsync(await KarneOlusturAsync(sahip, aracId, new
            {
                bakimGecmisi = true,
                parcaHafizasi = false,
                yakitOzeti = false,
                belgeler = false,
                plakaGoster = false,
                tutarGoster = false
            }));
            var token = TokenAl(karne.GetProperty("url").GetString());

            var anonim = _factory.CreateClient();
            var govde = await VeriAsync(await anonim.GetAsync($"/api/karne/{token}"));

            Assert.Equal("34 *** 001".Substring(0, 3), govde.GetProperty("arac").GetProperty("plaka").GetString().Substring(0, 3));
            Assert.Contains("***", govde.GetProperty("arac").GetProperty("plaka").GetString());
            Assert.Equal(0, govde.GetProperty("parcalar").GetArrayLength());
            Assert.Equal(0, govde.GetProperty("belgeler").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, govde.GetProperty("bakimlar")[0].GetProperty("tutar").ValueKind);

            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync($"/api/karne/{token}/belge/{belgeId}")).StatusCode);
        }

        [Fact]
        public async Task BelgelerAcikkenBelgeIndirilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34KRN005");
            var belgeId = await BelgeEkleAsync(sahip, aracId);

            var karne = await VeriAsync(await KarneOlusturAsync(sahip, aracId, TamKapsam));
            var token = TokenAl(karne.GetProperty("url").GetString());

            var anonim = _factory.CreateClient();
            var cevap = await anonim.GetAsync($"/api/karne/{token}/belge/{belgeId}");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal(PngIcerik.Length, (await cevap.Content.ReadAsByteArrayAsync()).Length);
        }

        [Fact]
        public async Task BaskaAracinBelgesi404Doner()
        {
            var sahip = await SahipOlusturAsync();
            var aracA = await AracEkleAsync(sahip, "34KRN006");
            var aracB = await AracEkleAsync(sahip, "34KRN007");
            var belgeB = await BelgeEkleAsync(sahip, aracB);

            var karne = await VeriAsync(await KarneOlusturAsync(sahip, aracA, TamKapsam));
            var token = TokenAl(karne.GetProperty("url").GetString());

            var anonim = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync($"/api/karne/{token}/belge/{belgeB}")).StatusCode);
        }

        [Fact]
        public async Task DriverKarneOlusturamaz()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34KRN008");
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });

            var cevap = await KarneOlusturAsync(surucu, aracId, TamKapsam);

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task IkinciOlusturmaEskisiniPasiflestirir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34KRN009");

            var ilk = TokenAl((await VeriAsync(await KarneOlusturAsync(sahip, aracId, TamKapsam))).GetProperty("url").GetString());
            var ikinci = TokenAl((await VeriAsync(await KarneOlusturAsync(sahip, aracId, TamKapsam))).GetProperty("url").GetString());

            var anonim = _factory.CreateClient();

            Assert.NotEqual(ilk, ikinci);
            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync($"/api/karne/{ilk}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await anonim.GetAsync($"/api/karne/{ikinci}")).StatusCode);
        }

        [Fact]
        public async Task GoruntulenmeSayaciArtar()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34KRN010");
            var token = TokenAl((await VeriAsync(await KarneOlusturAsync(sahip, aracId, TamKapsam))).GetProperty("url").GetString());

            var anonim = _factory.CreateClient();
            await anonim.GetAsync($"/api/karne/{token}");
            await anonim.GetAsync($"/api/karne/{token}");
            await anonim.GetAsync($"/api/karne/{token}");

            var istatistik = await VeriAsync(await sahip.GetAsync("/api/Vehicles/karne-stats"));

            Assert.Equal(3, istatistik.GetProperty("toplamGoruntulenme").GetInt32());
            Assert.Equal(1, istatistik.GetProperty("karnesiAktifArac").GetInt32());
        }

        [Fact]
        public async Task GecersizToken404Doner()
        {
            var anonim = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync("/api/karne/olmayan-token-degeri")).StatusCode);
        }

        [Fact]
        public async Task SuresiDolmusKarne404Doner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34KRN011");

            var karne = await VeriAsync(await KarneOlusturAsync(sahip, aracId, TamKapsam, -1));
            var token = TokenAl(karne.GetProperty("url").GetString());

            var anonim = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync($"/api/karne/{token}")).StatusCode);
        }

        private static string TokenAl(string url)
        {
            var i = url.IndexOf("t=", StringComparison.Ordinal);
            return url.Substring(i + 2);
        }

        public void Dispose()
        {
            _factory.Dispose();
            try { Directory.Delete(_klasor, true); } catch { }
        }
    }
}
