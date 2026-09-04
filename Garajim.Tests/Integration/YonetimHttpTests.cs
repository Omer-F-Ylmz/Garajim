using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class YonetimHttpTests : IDisposable
    {
        private const string YoneticiEposta = "yonetici@garajim.local";

        private class YonetimFactory : GarajimWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["App:YoneticiEposta"] = YoneticiEposta,
                        ["Usta:Enabled"] = "false"
                    });
                });
            }
        }

        private readonly YonetimFactory _factory = new YonetimFactory();

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> KullaniciAsync(string eposta)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Yönetim", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2020,
                currentKm = 40000,
                fuelType = "Dizel"
            });
            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BaskaKullanici403Alir()
        {
            var client = await KullaniciAsync(Eposta("yonetimbaska"));

            var cevap = await client.GetAsync("/api/Yonetim/ozet");

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task GirissizReddedilir()
        {
            var cevap = await _factory.CreateClient().GetAsync("/api/Yonetim/ozet");

            Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        }

        [Fact]
        public async Task YoneticiOzetiGorur()
        {
            var yonetici = await KullaniciAsync(YoneticiEposta);
            await AracEkleAsync(yonetici, "34YNT001");

            var baskaSirket = await KullaniciAsync(Eposta("yonetimdiger"));
            await AracEkleAsync(baskaSirket, "34YNT002");
            await baskaSirket.PostAsJsonAsync("/api/GeriBildirim", new
            {
                tur = "Oneri",
                mesaj = "Yönetim özetinde görünmeli",
                sayfa = "bakim",
                surum = "1.0.0+test"
            });

            var cevap = await yonetici.GetAsync("/api/Yonetim/ozet");
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.True(veri.GetProperty("sirketSayisi").GetInt32() >= 2);
            Assert.True(veri.GetProperty("kullaniciSayisi").GetInt32() >= 2);
            Assert.True(veri.GetProperty("aracSayisi").GetInt32() >= 2);
            Assert.Equal(30, veri.GetProperty("gunlukKayitlar").GetArrayLength());
            Assert.False(veri.GetProperty("ustaAcik").GetBoolean());
            Assert.True(veri.GetProperty("bellek").GetProperty("calismaKumesiMb").GetDouble() > 0);
            Assert.Contains(veri.GetProperty("sonGeriBildirimler").EnumerateArray(),
                g => g.GetProperty("mesaj").GetString() == "Yönetim özetinde görünmeli");
        }

        [Fact]
        public async Task OzetFisVeTokenAlanlariniTasir()
        {
            var yonetici = await KullaniciAsync(YoneticiEposta);

            var cevap = await yonetici.GetAsync("/api/Yonetim/ozet");
            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            foreach (var alan in new[]
            {
                "fisSayisi", "fisDogrulukOrani", "otoOnayOrani", "aiTokenKullanilan",
                "aiTahminiMaliyetUsd", "kotaHatasi", "karnePaylasimOrani", "davetKayitOrani"
            })
            {
                Assert.True(veri.TryGetProperty(alan, out _), alan + " alanı eksik.");
            }
        }

        [Fact]
        public async Task DemoSifirlamaYalnizYoneticiye()
        {
            var baska = await KullaniciAsync(Eposta("yonetimdemobaska"));
            Assert.Equal(HttpStatusCode.Forbidden, (await baska.PostAsync("/api/Yonetim/demo-sifirla", null)).StatusCode);

            var yonetici = await KullaniciAsync(YoneticiEposta);
            var cevap = await yonetici.PostAsync("/api/Yonetim/demo-sifirla", null);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }
    }
}
