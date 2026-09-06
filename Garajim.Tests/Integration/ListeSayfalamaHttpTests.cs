using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class ListeSayfalamaHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ListeSayfalamaHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> HazirlaAsync(string on, string plaka)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Liste", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 40000,
                fuelType = "Dizel"
            });

            var govde = await arac.Content.ReadAsStringAsync();
            Assert.True(arac.IsSuccessStatusCode, govde);

            var id = JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, id);
        }

        private static async Task BakimEkleAsync(HttpClient client, int aracId, int gunOnce, int km, string servis, string not)
        {
            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date.AddDays(-gunOnce),
                km,
                cost = 1000m + km,
                serviceName = servis,
                note = not
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        private static async Task<JsonElement> VeriAsync(HttpClient client, string yol)
        {
            var cevap = await client.GetAsync(yol);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.True(cevap.IsSuccessStatusCode, govde);

            return JsonDocument.Parse(govde).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task ParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("listeduz", "34LS1001");
            await BakimEkleAsync(client, aracId, 3, 41000, "Oto Merkez", "yağ değişimi");

            var veri = await VeriAsync(client, "/api/Maintenance?vehicleId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task SayfaParametresiZarfDondurur()
        {
            var (client, aracId) = await HazirlaAsync("listezarf", "34LS1002");

            for (var i = 0; i < 7; i++)
            {
                await BakimEkleAsync(client, aracId, i + 1, 41000 + i * 100, "Servis " + i, "not " + i);
            }

            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&sayfa=2&boyut=3");

            Assert.Equal(JsonValueKind.Object, veri.ValueKind);
            Assert.Equal(7, veri.GetProperty("toplam").GetInt32());
            Assert.Equal(2, veri.GetProperty("sayfa").GetInt32());
            Assert.Equal(3, veri.GetProperty("boyut").GetInt32());
            Assert.Equal(3, veri.GetProperty("kayitlar").GetArrayLength());
        }

        [Fact]
        public async Task BoyutYuzUstuneCikmaz()
        {
            var (client, aracId) = await HazirlaAsync("listeboyut", "34LS1003");
            await BakimEkleAsync(client, aracId, 1, 41000, "Servis", "not");

            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&boyut=101");

            Assert.Equal(100, veri.GetProperty("boyut").GetInt32());
        }

        [Fact]
        public async Task SiralamaCalisir()
        {
            var (client, aracId) = await HazirlaAsync("listesirala", "34LS1004");
            await BakimEkleAsync(client, aracId, 10, 41000, "Eski", "a");
            await BakimEkleAsync(client, aracId, 1, 45000, "Yeni", "b");

            var artan = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&sirala=km:asc");
            var azalan = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&sirala=km:desc");

            Assert.Equal(41000, artan.GetProperty("kayitlar")[0].GetProperty("km").GetInt32());
            Assert.Equal(45000, azalan.GetProperty("kayitlar")[0].GetProperty("km").GetInt32());
        }

        [Fact]
        public async Task GecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("listekotu", "34LS1005");

            var cevap = await client.GetAsync($"/api/Maintenance?vehicleId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task TurkceAramaBuyukKucukDuyarsizdir()
        {
            var (client, aracId) = await HazirlaAsync("listeara", "34LS1006");
            await BakimEkleAsync(client, aracId, 5, 41000, "Oto Merkez", "yağ filtresi değişti");
            await BakimEkleAsync(client, aracId, 4, 42000, "Lastikçi", "rot balans");

            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&q=YA%C4%9E");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
            Assert.Contains("yağ", veri.GetProperty("kayitlar")[0].GetProperty("note").GetString());
        }

        [Fact]
        public async Task AramaServisAdindaDaCalisir()
        {
            var (client, aracId) = await HazirlaAsync("listeservis", "34LS1007");
            await BakimEkleAsync(client, aracId, 5, 41000, "Şahin Oto", "bakım");
            await BakimEkleAsync(client, aracId, 4, 42000, "Lastikçi", "rot");

            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&q=sahin");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task TarihAraligiSuzer()
        {
            var (client, aracId) = await HazirlaAsync("listetarih", "34LS1008");
            await BakimEkleAsync(client, aracId, 40, 41000, "Eski", "a");
            await BakimEkleAsync(client, aracId, 2, 42000, "Yeni", "b");

            var bas = DateTime.UtcNow.Date.AddDays(-10).ToString("yyyy-MM-dd");
            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&baslangic={bas}&sayfa=1");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
            Assert.Equal("Yeni", veri.GetProperty("kayitlar")[0].GetProperty("serviceName").GetString());
        }

        [Fact]
        public async Task BaskaSirketinAracinaErisilemez()
        {
            var (_, yabanciAracId) = await HazirlaAsync("listeyabanci", "34LS1009");
            var (client, _) = await HazirlaAsync("listebenim", "34LS1010");

            var cevap = await client.GetAsync($"/api/Maintenance?vehicleId={yabanciAracId}&sayfa=1");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        private static async Task YakitEkleAsync(HttpClient client, int aracId, int gunOnce, int km, decimal litre)
        {
            var cevap = await client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = DateTime.UtcNow.Date.AddDays(-gunOnce),
                liters = litre,
                totalCost = litre * 45m,
                km,
                tamDolum = true
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task YakitParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("yakitduz", "34LS1011");
            await YakitEkleAsync(client, aracId, 3, 41000, 40m);

            var veri = await VeriAsync(client, "/api/Fuel?vehicleId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task YakitSayfalamaVeSiralamaCalisir()
        {
            var (client, aracId) = await HazirlaAsync("yakitzarf", "34LS1012");

            for (var i = 0; i < 5; i++)
            {
                await YakitEkleAsync(client, aracId, 20 - i, 41000 + i * 500, 30m + i);
            }

            var veri = await VeriAsync(client, $"/api/Fuel?vehicleId={aracId}&sayfa=1&boyut=2&sirala=km:asc");

            Assert.Equal(5, veri.GetProperty("toplam").GetInt32());
            Assert.Equal(2, veri.GetProperty("kayitlar").GetArrayLength());
            Assert.Equal(41000, veri.GetProperty("kayitlar")[0].GetProperty("km").GetInt32());
        }

        [Fact]
        public async Task YakitTarihAraligiSuzer()
        {
            var (client, aracId) = await HazirlaAsync("yakittarih", "34LS1013");
            await YakitEkleAsync(client, aracId, 40, 41000, 30m);
            await YakitEkleAsync(client, aracId, 2, 42000, 35m);

            var bas = DateTime.UtcNow.Date.AddDays(-10).ToString("yyyy-MM-dd");
            var veri = await VeriAsync(client, $"/api/Fuel?vehicleId={aracId}&baslangic={bas}&sayfa=1");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task YakitGecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("yakitkotu", "34LS1014");

            var cevap = await client.GetAsync($"/api/Fuel?vehicleId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        private static async Task MasrafEkleAsync(HttpClient client, int aracId, int gunOnce, decimal tutar, string not)
        {
            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId,
                category = "Otopark",
                date = DateTime.UtcNow.Date.AddDays(-gunOnce),
                amount = tutar,
                note = not
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task MasrafParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("masrafduz", "34LS1015");
            await MasrafEkleAsync(client, aracId, 3, 250m, "otopark");

            var veri = await VeriAsync(client, "/api/Expenses?vehicleId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task MasrafAramaVeSayfalamaCalisir()
        {
            var (client, aracId) = await HazirlaAsync("masrafara", "34LS1016");
            await MasrafEkleAsync(client, aracId, 5, 250m, "Şişli otoparkı");
            await MasrafEkleAsync(client, aracId, 4, 120m, "köprü geçişi");

            var veri = await VeriAsync(client, $"/api/Expenses?vehicleId={aracId}&q=SISLI");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
            Assert.Equal(1, veri.GetProperty("kayitlar").GetArrayLength());
        }

        [Fact]
        public async Task MasrafTutaraGoreSiralanir()
        {
            var (client, aracId) = await HazirlaAsync("masrafsira", "34LS1017");
            await MasrafEkleAsync(client, aracId, 5, 250m, "a");
            await MasrafEkleAsync(client, aracId, 4, 120m, "b");

            var veri = await VeriAsync(client, $"/api/Expenses?vehicleId={aracId}&sirala=tutar:asc");

            Assert.Equal(120m, veri.GetProperty("kayitlar")[0].GetProperty("amount").GetDecimal());
        }

        [Fact]
        public async Task MasrafGecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("masrafkotu", "34LS1018");

            var cevap = await client.GetAsync($"/api/Expenses?vehicleId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        private static async Task HatirlatmaEkleAsync(HttpClient client, int aracId, int gunSonra, string not)
        {
            var cevap = await client.PostAsJsonAsync("/api/Reminders", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                dueDate = DateTime.UtcNow.Date.AddDays(gunSonra),
                note = not
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task HatirlatmaParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("hatduz", "34LS1019");
            await HatirlatmaEkleAsync(client, aracId, 30, "muayene");

            var veri = await VeriAsync(client, "/api/Reminders?vehicleId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task HatirlatmaAramaVeSayfalamaCalisir()
        {
            var (client, aracId) = await HazirlaAsync("hatara", "34LS1020");
            await HatirlatmaEkleAsync(client, aracId, 10, "Yağ değişimi");
            await HatirlatmaEkleAsync(client, aracId, 20, "lastik çevirme");

            var veri = await VeriAsync(client, $"/api/Reminders?vehicleId={aracId}&q=YAG");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task HatirlatmaTarihineGoreSiralanir()
        {
            var (client, aracId) = await HazirlaAsync("hatsira", "34LS1021");
            await HatirlatmaEkleAsync(client, aracId, 40, "uzak");
            await HatirlatmaEkleAsync(client, aracId, 5, "yakin");

            var veri = await VeriAsync(client, $"/api/Reminders?vehicleId={aracId}&sirala=tarih:asc");

            Assert.Equal("yakin", veri.GetProperty("kayitlar")[0].GetProperty("note").GetString());
        }

        [Fact]
        public async Task HatirlatmaGecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("hatkotu", "34LS1022");

            var cevap = await client.GetAsync($"/api/Reminders?vehicleId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        private static async Task EvrakEkleAsync(HttpClient client, int aracId, string tur, int gunSonra, string saglayici)
        {
            var cevap = await client.PostAsJsonAsync("/api/Evrak", new
            {
                vehicleId = aracId,
                evrakTuru = tur,
                bitisTarihi = DateTime.UtcNow.Date.AddDays(gunSonra),
                saglayici
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task EvrakParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("evrakduz", "34LS1023");
            await EvrakEkleAsync(client, aracId, "Muayene", 200, "TÜVTÜRK");

            var veri = await VeriAsync(client, "/api/Evrak?vehicleId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task EvrakSaglayiciyaGoreAranir()
        {
            var (client, aracId) = await HazirlaAsync("evrakara", "34LS1024");
            await EvrakEkleAsync(client, aracId, "Muayene", 200, "TÜVTÜRK");
            await EvrakEkleAsync(client, aracId, "Kasko", 150, "Anadolu Sigorta");

            var veri = await VeriAsync(client, $"/api/Evrak?vehicleId={aracId}&q=tuvturk");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task EvrakBitisTarihineGoreSiralanir()
        {
            var (client, aracId) = await HazirlaAsync("evraksira", "34LS1025");
            await EvrakEkleAsync(client, aracId, "Muayene", 300, "Uzak");
            await EvrakEkleAsync(client, aracId, "Kasko", 20, "Yakin");

            var veri = await VeriAsync(client, $"/api/Evrak?vehicleId={aracId}&sirala=tarih:asc");

            Assert.Equal("Yakin", veri.GetProperty("kayitlar")[0].GetProperty("saglayici").GetString());
        }

        [Fact]
        public async Task EvrakGecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("evrakkotu", "34LS1026");

            var cevap = await client.GetAsync($"/api/Evrak?vehicleId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        private static async Task HasarEkleAsync(HttpClient client, int aracId, int gunOnce, string aciklama)
        {
            var cevap = await client.PostAsJsonAsync("/api/Hasar", new
            {
                vehicleId = aracId,
                olayTarihi = DateTime.UtcNow.Date.AddDays(-gunOnce),
                tur = "Kaza",
                konum = "Ön tampon",
                aciklama,
                tutanakTuru = "Anlasmali"
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task HasarParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("hasarduz", "34LS1027");
            await HasarEkleAsync(client, aracId, 5, "park halinde çizik");

            var veri = await VeriAsync(client, "/api/Hasar?aracId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task HasarAciklamadaAranir()
        {
            var (client, aracId) = await HazirlaAsync("hasarara", "34LS1028");
            await HasarEkleAsync(client, aracId, 5, "Şişli'de çarpma");
            await HasarEkleAsync(client, aracId, 4, "dolu hasarı");

            var veri = await VeriAsync(client, $"/api/Hasar?aracId={aracId}&q=sisli");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task HasarTarihineGoreSiralanir()
        {
            var (client, aracId) = await HazirlaAsync("hasarsira", "34LS1029");
            await HasarEkleAsync(client, aracId, 40, "eski olay");
            await HasarEkleAsync(client, aracId, 2, "yeni olay");

            var veri = await VeriAsync(client, $"/api/Hasar?aracId={aracId}&sirala=tarih:asc");

            Assert.Equal("eski olay", veri.GetProperty("kayitlar")[0].GetProperty("aciklama").GetString());
        }

        [Fact]
        public async Task HasarGecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("hasarkotu", "34LS1030");

            var cevap = await client.GetAsync($"/api/Hasar?aracId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        private static async Task YolculukEkleAsync(HttpClient client, int aracId, int gunOnce, int baslangicKm, int bitisKm, string nereye)
        {
            var cevap = await client.PostAsJsonAsync("/api/Yolculuk", new
            {
                vehicleId = aracId,
                tarih = DateTime.UtcNow.Date.AddDays(-gunOnce),
                baslangicKm,
                bitisKm,
                amac = "Is",
                nereden = "Kadıköy",
                nereye
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task YolculukParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("yolduz", "34LS1031");
            await YolculukEkleAsync(client, aracId, 3, 40000, 40120, "Ankara");

            var veri = await VeriAsync(client, "/api/Yolculuk?vehicleId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task YolculukTarihAraligiZarfsizCalismayaDevamEder()
        {
            var (client, aracId) = await HazirlaAsync("yoltarih", "34LS1032");
            await YolculukEkleAsync(client, aracId, 40, 40000, 40120, "Eski");
            await YolculukEkleAsync(client, aracId, 2, 40200, 40260, "Yeni");

            var bas = DateTime.UtcNow.Date.AddDays(-10).ToString("yyyy-MM-dd");
            var veri = await VeriAsync(client, $"/api/Yolculuk?vehicleId={aracId}&baslangic={bas}");

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task YolculukVarisNoktasindaAranir()
        {
            var (client, aracId) = await HazirlaAsync("yolara", "34LS1033");
            await YolculukEkleAsync(client, aracId, 5, 40000, 40120, "Şişli");
            await YolculukEkleAsync(client, aracId, 4, 40200, 40260, "Bursa");

            var veri = await VeriAsync(client, $"/api/Yolculuk?vehicleId={aracId}&q=sisli");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task YolculukMesafeyeGoreSiralanir()
        {
            var (client, aracId) = await HazirlaAsync("yolsira", "34LS1034");
            await YolculukEkleAsync(client, aracId, 5, 40000, 40120, "Uzun");
            await YolculukEkleAsync(client, aracId, 4, 40200, 40230, "Kisa");

            var veri = await VeriAsync(client, $"/api/Yolculuk?vehicleId={aracId}&sirala=mesafe:asc");

            Assert.Equal("Kisa", veri.GetProperty("kayitlar")[0].GetProperty("nereye").GetString());
        }

        [Fact]
        public async Task YolculukGecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("yolkotu", "34LS1035");

            var cevap = await client.GetAsync($"/api/Yolculuk?vehicleId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        private static async Task BelgeEkleAsync(HttpClient client, int aracId, string dosyaAdi)
        {
            using var icerik = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 });
            dosya.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            icerik.Add(dosya, "file", dosyaAdi);
            icerik.Add(new StringContent(aracId.ToString()), "vehicleId");

            var cevap = await client.PostAsync("/api/Documents", icerik);

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BelgeParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("belgeduz", "34LS1036");
            await BelgeEkleAsync(client, aracId, "ruhsat.pdf");

            var veri = await VeriAsync(client, "/api/Documents?vehicleId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task BelgeDosyaAdindaAranir()
        {
            var (client, aracId) = await HazirlaAsync("belgeara", "34LS1037");
            await BelgeEkleAsync(client, aracId, "muayene-raporu.pdf");
            await BelgeEkleAsync(client, aracId, "kasko-police.pdf");

            var veri = await VeriAsync(client, $"/api/Documents?vehicleId={aracId}&q=KASKO");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task BelgeAdaGoreSiralanir()
        {
            var (client, aracId) = await HazirlaAsync("belgesira", "34LS1038");
            await BelgeEkleAsync(client, aracId, "zeytin.pdf");
            await BelgeEkleAsync(client, aracId, "ada.pdf");

            var veri = await VeriAsync(client, $"/api/Documents?vehicleId={aracId}&sirala=ad:asc");

            Assert.Equal("ada.pdf", veri.GetProperty("kayitlar")[0].GetProperty("originalName").GetString());
        }

        [Fact]
        public async Task BelgeGecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("belgekotu", "34LS1039");

            var cevap = await client.GetAsync($"/api/Documents?vehicleId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task EvrakDurumSiralamasiAktifleriOneAlir()
        {
            var (client, aracId) = await HazirlaAsync("evrakdurum", "34LS1040");
            await EvrakEkleAsync(client, aracId, "Muayene", 300, "Aktif Uzak");
            await EvrakEkleAsync(client, aracId, "Kasko", 20, "Aktif Yakin");

            var liste = await VeriAsync(client, $"/api/Evrak?vehicleId={aracId}&sirala=durum:desc");

            Assert.Equal("Aktif Yakin", liste.GetProperty("kayitlar")[0].GetProperty("saglayici").GetString());
            Assert.Equal("Aktif Uzak", liste.GetProperty("kayitlar")[1].GetProperty("saglayici").GetString());
        }
    }
}
