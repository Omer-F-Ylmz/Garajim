using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class DegerHttpTests : IDisposable
    {
        private sealed class DegerFactory : GarajimWebApplicationFactory
        {
            public SahteDegerTahminEdici Tahminci { get; } = new SahteDegerTahminEdici();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureServices(services =>
                {
                    var mevcut = services.Where(d => d.ServiceType == typeof(IDegerTahminEdici)).ToList();
                    foreach (var kayit in mevcut)
                    {
                        services.Remove(kayit);
                    }

                    services.AddSingleton<IDegerTahminEdici>(Tahminci);
                });
            }
        }

        private readonly DegerFactory _factory;

        public DegerHttpTests()
        {
            _factory = new DegerFactory();
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => "34DG" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("deger"), fullName = "Değer Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string model = "Clio")
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model,
                year = 2019,
                currentKm = 90000,
                fuelType = "Benzin",
                vites = "Manuel"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static Task<HttpResponseMessage> BeyanAsync(HttpClient client, int aracId, string tarih, decimal deger, string kaynak = "Beyan")
        {
            return client.PostAsJsonAsync($"/api/Vehicles/{aracId}/deger", new { tarih, deger, kaynak, not = "Sahibinden ortalama" });
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task BeyanKaydedilirVeSeriDoner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            await BeyanAsync(sahip, aracId, "2026-01-15", 900000m);
            await BeyanAsync(sahip, aracId, "2026-08-15", 820000m);

            var seri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/deger"));

            Assert.Equal(2, seri.GetProperty("kayitlar").GetArrayLength());
            Assert.Equal(820000m, seri.GetProperty("sonDeger").GetProperty("deger").GetDecimal());
            Assert.Equal("Beyan", seri.GetProperty("sonDeger").GetProperty("kaynakAdi").GetString());
            Assert.Equal(80000m, seri.GetProperty("degerKaybi").GetDecimal());
        }

        [Fact]
        public async Task TekDegerdeDegerKaybiBos()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            await BeyanAsync(sahip, aracId, "2026-01-15", 900000m);

            var seri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/deger"));

            Assert.Equal(1, seri.GetProperty("kayitlar").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, seri.GetProperty("degerKaybi").ValueKind);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5000)]
        public async Task SifirVeEksiDegerReddedilir(decimal deger)
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var cevap = await BeyanAsync(sahip, aracId, "2026-01-15", deger);

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task GelecekTarihliDegerReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var yarin = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
            var bugun = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            var gelecek = await BeyanAsync(sahip, aracId, yarin, 900000m);
            var gecerli = await BeyanAsync(sahip, aracId, bugun, 900000m);

            Assert.Equal(HttpStatusCode.BadRequest, gelecek.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gecerli.StatusCode);
        }

        [Theory]
        [InlineData("Uydurma")]
        [InlineData("Tahmin")]
        public async Task GecersizKaynakReddedilir(string kaynak)
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var cevap = await BeyanAsync(sahip, aracId, "2026-01-15", 900000m, kaynak);

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task BaskaSirketinDegeriGorulemez()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci);
            await BeyanAsync(birinci, aracId, "2026-01-15", 900000m);

            var ikinci = await SahipOlusturAsync();

            var oku = await ikinci.GetAsync($"/api/Vehicles/{aracId}/deger");
            var yaz = await BeyanAsync(ikinci, aracId, "2026-02-15", 100m);
            var tahmin = await ikinci.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);

            Assert.Equal(HttpStatusCode.NotFound, oku.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, yaz.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, tahmin.StatusCode);
        }

        [Fact]
        public async Task KapsamDisiModelIcin422Doner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "Bilinmeyen Seri X");

            var cevap = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.UnprocessableEntity, cevap.StatusCode);
            Assert.Contains("kapsam", govde, StringComparison.OrdinalIgnoreCase);

            var seri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/deger"));
            Assert.Equal(0, seri.GetProperty("kayitlar").GetArrayLength());
        }

        [Fact]
        public async Task KapsamdakiModelTahminKaydederVeUyariDoner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var cevap = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
            var veri = await VeriAsync(cevap);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal("Tahmin", veri.GetProperty("kayit").GetProperty("kaynak").GetString());
            Assert.Equal(725000m, veri.GetProperty("kayit").GetProperty("deger").GetDecimal());
            Assert.Equal(2, veri.GetProperty("kalanHak").GetInt32());

            var uyari = veri.GetProperty("uyari").GetString();
            Assert.Contains("Ağustos 2025", uyari);
            Assert.Contains("enflasyon", uyari);
            Assert.Contains("bilgilendirme", uyari);

            var seri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/deger"));
            Assert.Equal(1, seri.GetProperty("kayitlar").GetArrayLength());
            Assert.Equal("Tahmin", seri.GetProperty("sonDeger").GetProperty("kaynakAdi").GetString());
        }

        [Fact]
        public async Task GunlukUcTahminSonrasiDorduncuReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            for (var i = 0; i < DegerManager.GunlukTahminHakki; i++)
            {
                var cevap = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
                Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            }

            var dorduncu = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
            var govde = await dorduncu.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, dorduncu.StatusCode);
            Assert.Contains("3", govde);

            var seri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/deger"));
            Assert.Equal(3, seri.GetProperty("kayitlar").GetArrayLength());

            var digerArac = await AracEkleAsync(sahip, "Egea");
            var digerinTahmini = await sahip.PostAsync($"/api/Vehicles/{digerArac}/deger/tahmin", null);
            Assert.Equal(HttpStatusCode.OK, digerinTahmini.StatusCode);
        }

        [Fact]
        public async Task SahiplikMaliyetiDegerKaybiniEkler()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            await sahip.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId,
                date = "2026-03-10",
                category = "Kasko",
                amount = 12000.0,
                note = "Kasko"
            });

            var tekDeger = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-12-31"));
            Assert.Equal(JsonValueKind.Null, tekDeger.GetProperty("sahiplikMaliyeti").ValueKind);
            Assert.Equal(JsonValueKind.Null, tekDeger.GetProperty("donemDegerKaybi").ValueKind);

            await BeyanAsync(sahip, aracId, "2026-01-15", 900000m);

            var halaTek = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-12-31"));
            Assert.Equal(JsonValueKind.Null, halaTek.GetProperty("sahiplikMaliyeti").ValueKind);

            await BeyanAsync(sahip, aracId, "2026-08-15", 820000m);

            var maliyet = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-12-31"));

            Assert.Equal(80000m, maliyet.GetProperty("donemDegerKaybi").GetDecimal());
            Assert.Equal(12000m, maliyet.GetProperty("toplamMaliyet").GetDecimal());
            Assert.Equal(92000m, maliyet.GetProperty("sahiplikMaliyeti").GetDecimal());
        }

        [Fact]
        public async Task PanelFiloToplamDegeriSonDegerlerdenHesaplar()
        {
            var sahip = await SahipOlusturAsync();
            var birinci = await AracEkleAsync(sahip);
            var ikinci = await AracEkleAsync(sahip, "Egea");

            await BeyanAsync(sahip, birinci, "2026-01-15", 900000m);
            await BeyanAsync(sahip, birinci, "2026-08-15", 820000m);
            await BeyanAsync(sahip, ikinci, "2026-05-01", 640000m);

            var panel = await VeriAsync(await sahip.GetAsync("/api/Reports/dashboard"));

            Assert.Equal(1460000m, panel.GetProperty("filoToplamDeger").GetDecimal());
        }

        [Fact]
        public async Task KarneBayragiKapaliykenDegerGorunmezAcikkenYalnizBeyanGorunur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            await BeyanAsync(sahip, aracId, "2026-01-15", 900000m);
            await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);

            var anonim = _factory.CreateClient();

            var kapali = await sahip.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new { kapsam = new { beyanDegeri = false } });
            var kapaliKarne = JsonDocument.Parse(await anonim.GetStringAsync($"/api/karne/{Token(await kapali.Content.ReadAsStringAsync())}")).RootElement.GetProperty("data");
            Assert.Equal(JsonValueKind.Null, kapaliKarne.GetProperty("beyanDegeri").ValueKind);

            var acik = await sahip.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new { kapsam = new { beyanDegeri = true } });
            var acikKarne = JsonDocument.Parse(await anonim.GetStringAsync($"/api/karne/{Token(await acik.Content.ReadAsStringAsync())}")).RootElement.GetProperty("data");
            var deger = acikKarne.GetProperty("beyanDegeri");

            Assert.Equal(900000m, deger.GetProperty("deger").GetDecimal());
            Assert.Equal("Beyan", deger.GetProperty("kaynakAdi").GetString());
        }

        [Fact]
        public async Task UstaAracBaglaminaDegerGirmez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            await BeyanAsync(sahip, aracId, "2026-01-15", 987654m);

            using var kapsam = _factory.Services.CreateScope();
            var kaynak = File.ReadAllText(Path.Combine(KaynakKoku(), "Garajim.Business", "Concrete", "UstaManager.cs"));
            var baslangic = kaynak.IndexOf("private async Task<string> AracBaglamiAsync", StringComparison.Ordinal);

            Assert.True(baslangic > 0);

            var govde = kaynak.Substring(baslangic);
            var son = govde.IndexOf("\n        private ", StringComparison.Ordinal);
            if (son > 0)
            {
                govde = govde.Substring(0, son);
            }

            Assert.DoesNotContain("Deger", govde.Replace("Deger(", string.Empty));
            Assert.DoesNotContain("AracDeger", govde);
            Assert.DoesNotContain("_degerService", govde);
            Assert.DoesNotContain("_degerDal", govde);
        }

        private static string KaynakKoku()
        {
            var dizin = new DirectoryInfo(AppContext.BaseDirectory);
            while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "Garajim.sln")))
            {
                dizin = dizin.Parent;
            }

            Assert.NotNull(dizin);
            return dizin.FullName;
        }

        private static string Token(string govde)
        {
            var url = JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("url").GetString();
            return url.Substring(url.IndexOf("?t=", StringComparison.Ordinal) + 3);
        }
    }
}
