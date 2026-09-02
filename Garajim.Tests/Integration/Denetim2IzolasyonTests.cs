using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class Denetim2IzolasyonTests : IClassFixture<UstaWebApplicationFactory>
    {
        private const string Surum = "2026-09-v1";

        private readonly UstaWebApplicationFactory _factory;

        public Denetim2IzolasyonTests(UstaWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.Istemci.Uretici = null;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => "34DN" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

        private async Task<(HttpClient Client, int UserId)> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("dn"), fullName = "Denetim Sahip", password = "Test1234!" });
            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", veri.GetProperty("token").GetString());
            await client.PostAsJsonAsync("/api/Usta/onay", new { metinSurumu = Surum });
            return (client, 0);
        }

        private async Task<(HttpClient Client, int UserId)> UyeOlusturAsync(HttpClient sahip, string rol)
        {
            var eposta = Eposta("dn" + rol.ToLowerInvariant());
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Denetim " + rol, role = rol });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await client.PostAsJsonAsync("/api/Usta/onay", new { metinSurumu = Surum });
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 120000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static string Token(string url)
        {
            return url.Substring(url.IndexOf("?t=", StringComparison.Ordinal) + 3);
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        // T-1 · yabancı şirketin kaydına okuma
        [Fact]
        public async Task YabanciSirketinKayitlariniOkumaHepsindeDortYuzDort()
        {
            var (birinci, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci);

            await birinci.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = "2027-01-01" });
            var evrakId = (await VeriAsync(await birinci.GetAsync($"/api/Evrak?vehicleId={aracId}")))[0].GetProperty("id").GetInt32();

            await birinci.PostAsJsonAsync("/api/Yolculuk", new { vehicleId = aracId, tarih = "2026-03-01", baslangicKm = 120000, bitisKm = 120100, amac = "Is" });
            var yolculukId = (await VeriAsync(await birinci.GetAsync($"/api/Yolculuk?vehicleId={aracId}")))[0].GetProperty("id").GetInt32();

            var lastikId = (await VeriAsync(await birinci.PostAsJsonAsync("/api/Lastik", new { vehicleId = aracId, ad = "Yaz", mevsim = "Yaz", takilmaTarihi = "2026-04-01", takilmaKm = 120000 }))).GetProperty("id").GetInt32();

            var sohbetId = (await VeriAsync(await birinci.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = aracId }))).GetProperty("id").GetInt32();

            var (ikinci, _) = await SahipOlusturAsync();

            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.GetAsync($"/api/Vehicles/{aracId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.GetAsync($"/api/Vehicles/{aracId}/evrak")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-12-31")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.GetAsync($"/api/Yolculuk?vehicleId={aracId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.GetAsync($"/api/Lastik?vehicleId={aracId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.GetAsync($"/api/Usta/sohbet/{sohbetId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.GetAsync($"/api/Export/yakit.csv?vehicleId={aracId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.PutAsJsonAsync($"/api/Evrak/{evrakId}", new { evrakTuru = "Kasko", bitisTarihi = "2027-06-01" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.DeleteAsync($"/api/Yolculuk/{yolculukId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.DeleteAsync($"/api/Lastik/{lastikId}")).StatusCode);
        }

        // T-1 · yabancı VehicleId / UserId ile yazma
        [Fact]
        public async Task YabanciAracIdIleYazmaReddedilir()
        {
            var (birinci, _) = await SahipOlusturAsync();
            var yabanciArac = await AracEkleAsync(birinci);

            var (ikinci, _) = await SahipOlusturAsync();

            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.PostAsJsonAsync("/api/Evrak", new { vehicleId = yabanciArac, evrakTuru = "Kasko", bitisTarihi = "2027-01-01" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.PostAsJsonAsync("/api/Yolculuk", new { vehicleId = yabanciArac, tarih = "2026-03-01", baslangicKm = 1, bitisKm = 2, amac = "Is" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.PostAsJsonAsync("/api/Lastik", new { vehicleId = yabanciArac, ad = "Yaz", mevsim = "Yaz", takilmaTarihi = "2026-04-01", takilmaKm = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = yabanciArac })).StatusCode);
        }

        [Fact]
        public async Task YabanciKullaniciIdIleEvrakAcilamaz()
        {
            var (birinci, _) = await SahipOlusturAsync();
            var (_, yabanciUserId) = await UyeOlusturAsync(birinci, "Driver");

            var (ikinci, _) = await SahipOlusturAsync();

            var cevap = await ikinci.PostAsJsonAsync("/api/Evrak", new { userId = yabanciUserId, evrakTuru = "Ehliyet", bitisTarihi = "2027-01-01" });

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        // T-2 · Driver kapsamı yalnız aktif zimmet
        [Fact]
        public async Task DriverZimmetsizAracaHicbirUctanErisemez()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var (surucu, _) = await UyeOlusturAsync(sahip, "Driver");

            Assert.Equal(HttpStatusCode.NotFound, (await surucu.GetAsync($"/api/Vehicles/{aracId}/evrak")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await surucu.GetAsync($"/api/Yolculuk?vehicleId={aracId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await surucu.GetAsync($"/api/Lastik?vehicleId={aracId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await surucu.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = aracId })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await surucu.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-12-31")).StatusCode);
        }

        [Fact]
        public async Task ZimmetSonlanincaDriverErisimiKapanir()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var (surucu, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });
            Assert.Equal(HttpStatusCode.OK, (await surucu.GetAsync($"/api/Lastik?vehicleId={aracId}")).StatusCode);

            await sahip.PutAsJsonAsync("/api/Assignments/end", new { vehicleId = aracId });

            Assert.Equal(HttpStatusCode.NotFound, (await surucu.GetAsync($"/api/Lastik?vehicleId={aracId}")).StatusCode);
        }

        // T-2 · yazma yetkileri
        [Fact]
        public async Task DriverYazmaYasakOkumaSerbestUclari()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var (surucu, surucuId) = await UyeOlusturAsync(sahip, "Driver");
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });

            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = "2027-01-01" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.PostAsJsonAsync("/api/Lastik", new { vehicleId = aracId, ad = "Yaz", mevsim = "Yaz", takilmaTarihi = "2026-04-01", takilmaKm = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.GetAsync("/api/Davet")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.GetAsync("/api/Usta/stats")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.GetAsync("/api/Team")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.GetAsync("/api/Reports/filo-maliyet?baslangic=2026-01-01&bitis=2026-12-31")).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await surucu.GetAsync($"/api/Lastik?vehicleId={aracId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await surucu.GetAsync("/api/Reports/dashboard")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await surucu.GetAsync("/api/Export/yakit.csv")).StatusCode);
        }

        [Fact]
        public async Task ManagerYazabilirAmaSahipIsleriKapali()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var (yonetici, _) = await UyeOlusturAsync(sahip, "Manager");

            Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = "2027-01-01" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync("/api/Lastik", new { vehicleId = aracId, ad = "Yaz", mevsim = "Yaz", takilmaTarihi = "2026-04-01", takilmaKm = 120000 })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await yonetici.GetAsync("/api/Reports/filo-maliyet?baslangic=2026-01-01&bitis=2026-12-31")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await yonetici.GetAsync("/api/Team")).StatusCode);

            Assert.Equal(HttpStatusCode.Forbidden, (await yonetici.GetAsync("/api/Usta/stats")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await yonetici.PostAsJsonAsync("/api/Plan/yukseltme-talebi", new { istenenPlan = "Filo" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await yonetici.PostAsJsonAsync("/api/Team", new { email = Eposta("x"), fullName = "X", role = "Driver" })).StatusCode);
        }

        // T-4 · hata gövdeleri sızdırmaz
        [Fact]
        public async Task HataGovdesindeIcAdVeYiginIziYok()
        {
            var (birinci, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci);
            var (ikinci, _) = await SahipOlusturAsync();

            var cevap = await ikinci.GetAsync($"/api/Yolculuk?vehicleId={aracId}");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
            Assert.DoesNotContain("Garajim.", govde);
            Assert.DoesNotContain("Exception", govde);
            Assert.DoesNotContain("   at ", govde);
            Assert.DoesNotContain("Sqlite", govde);
            Assert.Contains("bulunamad", govde);
        }

        [Fact]
        public async Task UstaHataMesajlariTurkceVeTekduze()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var sohbetId = (await VeriAsync(await sahip.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = aracId }))).GetProperty("id").GetInt32();

            _factory.Istemci.Uretici = _ =>
            {
                var sonuc = SahteUstaIstemci.Varsayilan();
                sonuc.Yanit = null;
                sonuc.Hata = "HTTP 500";
                return sonuc;
            };

            var cevap = await sahip.PostAsJsonAsync($"/api/Usta/sohbet/{sohbetId}/mesaj", new { metin = "rolanti dalgalaniyor" });
            var govde = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement;

            Assert.Equal(HttpStatusCode.BadGateway, cevap.StatusCode);
            Assert.False(govde.GetProperty("success").GetBoolean());
            Assert.Contains("yanıt üretemedi", govde.GetProperty("message").GetString());
            Assert.DoesNotContain("HTTP 500", govde.GetProperty("message").GetString());
            Assert.DoesNotContain("Gemini", govde.GetProperty("message").GetString());
        }

        // T-6 · anonim uç dayanıklılığı
        [Fact]
        public async Task AnonimUclardaVarYokPasifSuresiDolmusAyniDortYuzDort()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var karne = await VeriAsync(await sahip.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new { kapsam = new { bakimGecmisi = true }, sonKullanmaGun = 30 }));
            var token = Token(karne.GetProperty("url").GetString());

            var anonim = _factory.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await anonim.GetAsync($"/api/karne/{token}")).StatusCode);

            var gecerliGovde = await (await anonim.GetAsync($"/api/karne/{new string('a', 43)}")).Content.ReadAsStringAsync();

            await sahip.DeleteAsync($"/api/Vehicles/{aracId}/karne");
            var pasifCevap = await anonim.GetAsync($"/api/karne/{token}");
            var pasifGovde = await pasifCevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, pasifCevap.StatusCode);
            Assert.Equal(gecerliGovde, pasifGovde);
        }

        [Fact]
        public async Task KarneTokenuBaskaSirketinAracinaAcilmaz()
        {
            var (birinci, _) = await SahipOlusturAsync();
            var aracA = await AracEkleAsync(birinci);
            var karne = await VeriAsync(await birinci.PostAsJsonAsync($"/api/Vehicles/{aracA}/karne", new { kapsam = new { bakimGecmisi = true, belgeler = true } }));
            var token = Token(karne.GetProperty("url").GetString());

            var (ikinci, _) = await SahipOlusturAsync();
            var aracB = await AracEkleAsync(ikinci);
            await ikinci.PostAsJsonAsync("/api/Documents", new { vehicleId = aracB });

            var anonim = _factory.CreateClient();
            var yabanciBelge = await anonim.GetAsync($"/api/karne/{token}/belge/999999");

            Assert.Equal(HttpStatusCode.NotFound, yabanciBelge.StatusCode);
        }

        [Fact]
        public async Task TakvimTokenuYalnizKendiAboneliginiVerir()
        {
            var (birinci, _) = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci);
            await birinci.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = "2027-01-01" });

            var abonelik = await VeriAsync(await birinci.PostAsync("/api/Takvim/abonelik", null));
            var url = abonelik.GetProperty("url").GetString();
            var token = url.Substring(url.LastIndexOf('/') + 1).Replace(".ics", string.Empty);

            var (ikinci, _) = await SahipOlusturAsync();
            var yabanciArac = await AracEkleAsync(ikinci);
            await ikinci.PostAsJsonAsync("/api/Evrak", new { vehicleId = yabanciArac, evrakTuru = "Muayene", bitisTarihi = "2027-02-02" });

            var anonim = _factory.CreateClient();
            var ics = await (await anonim.GetAsync($"/api/takvim/{token}.ics")).Content.ReadAsStringAsync();

            Assert.Contains("Kasko", ics);
            Assert.DoesNotContain("Muayene", ics);
        }
    }
}
