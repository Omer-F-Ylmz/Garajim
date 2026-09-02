using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class HasarHttpTests : IDisposable
    {
        private static readonly byte[] PngBaslik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private sealed class HasarFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;
            private readonly long _kota;

            public HasarFactory(string klasor, long kota)
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
        private readonly HasarFactory _factory;

        public HasarHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-hasar-" + Guid.NewGuid().ToString("N"));
            _factory = new HasarFactory(_klasor, 10 * 1024 * 1024);
        }

        public void Dispose()
        {
            _factory.Dispose();
            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => "34HS" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

        private async Task<HttpClient> SahipOlusturAsync(HasarFactory factory = null)
        {
            var f = factory ?? _factory;
            var client = f.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("hasar"), fullName = "Hasar Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("hasardriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Hasar Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Fiat",
                model = "Egea",
                year = 2020,
                currentKm = 60000,
                fuelType = "Dizel"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static Task<HttpResponseMessage> DosyaAcAsync(HttpClient client, int aracId, string tarih = "2026-08-01", string tur = "Kaza", string tutanak = "Anlasmali")
        {
            return client.PostAsJsonAsync("/api/Hasar", new
            {
                vehicleId = aracId,
                olayTarihi = tarih,
                tur,
                konum = "Ankara Eskişehir Yolu 12. km",
                aciklama = "Kavşakta arkadan çarpma, arka tampon çöktü.",
                olayKm = 60500,
                tutanakTuru = tutanak,
                karsiTarafPlaka = "06AB123",
                karsiTarafSigorta = "Örnek Sigorta",
                karsiTarafPoliceNo = "P-9911",
                sigortaDosyaNo = "S-4455",
                hasarBedeli = 18500.0
            });
        }

        private static async Task<int> DosyaIdAsync(HttpResponseMessage cevap)
        {
            var govde = await cevap.Content.ReadAsStringAsync();
            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static byte[] Foto(int boyut = 2048)
        {
            var icerik = new byte[Math.Max(PngBaslik.Length, boyut)];
            Array.Copy(PngBaslik, icerik, PngBaslik.Length);
            return icerik;
        }

        private static async Task<HttpResponseMessage> FotoEkleAsync(HttpClient client, int dosyaId, string etiket = "Genel", int boyut = 2048)
        {
            using var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(Foto(boyut));
            dosya.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(dosya, "file", "hasar.png");
            form.Add(new StringContent(etiket), "etiket");
            return await client.PostAsync($"/api/Hasar/{dosyaId}/foto", form);
        }

        [Fact]
        public async Task DosyaAcilirVeAracListesindeGorunur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var acilis = await DosyaAcAsync(sahip, aracId);
            Assert.Equal(HttpStatusCode.OK, acilis.StatusCode);
            var dosyaId = await DosyaIdAsync(acilis);

            var arac = await sahip.GetStringAsync($"/api/Vehicles/{aracId}/hasar");
            var liste = JsonDocument.Parse(arac).RootElement.GetProperty("data");

            Assert.Equal(1, liste.GetArrayLength());
            Assert.Equal(dosyaId, liste[0].GetProperty("id").GetInt32());
            Assert.Equal("Kaza", liste[0].GetProperty("turAdi").GetString());
            Assert.Equal("Açık", liste[0].GetProperty("durumAdi").GetString());
        }

        [Fact]
        public async Task BaskaSirketinDosyasiGorulemezVeDegistirilemez()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci);
            var dosyaId = await DosyaIdAsync(await DosyaAcAsync(birinci, aracId));

            var ikinci = await SahipOlusturAsync();

            var oku = await ikinci.GetAsync($"/api/Hasar/{dosyaId}");
            var liste = JsonDocument.Parse(await ikinci.GetStringAsync("/api/Hasar")).RootElement.GetProperty("data");
            var sil = await ikinci.DeleteAsync($"/api/Hasar/{dosyaId}");
            var tutanak = await ikinci.GetAsync($"/api/Hasar/{dosyaId}/tutanak.html");
            var foto = await FotoEkleAsync(ikinci, dosyaId);

            Assert.Equal(HttpStatusCode.NotFound, oku.StatusCode);
            Assert.Equal(0, liste.GetArrayLength());
            Assert.Equal(HttpStatusCode.NotFound, sil.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, tutanak.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, foto.StatusCode);
        }

        [Fact]
        public async Task SurucuZimmetliAracaDosyaAcarFotoEklerAmaDuzenleyemez()
        {
            var sahip = await SahipOlusturAsync();
            var zimmetli = await AracEkleAsync(sahip);
            var zimmetsiz = await AracEkleAsync(sahip);
            var surucu = await SurucuOlusturAsync(sahip);

            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = zimmetli, userId = surucu.UserId, startDate = "2026-01-01" });

            var acilis = await DosyaAcAsync(surucu.Client, zimmetli);
            Assert.Equal(HttpStatusCode.OK, acilis.StatusCode);
            var dosyaId = await DosyaIdAsync(acilis);

            var foto = await FotoEkleAsync(surucu.Client, dosyaId, "HasarYakin");
            var yasakArac = await DosyaAcAsync(surucu.Client, zimmetsiz);

            var guncelle = await surucu.Client.PutAsJsonAsync($"/api/Hasar/{dosyaId}", new
            {
                olayTarihi = "2026-08-01",
                tur = "Kaza",
                konum = "Ankara",
                aciklama = "Sürücü düzenlemesi",
                tutanakTuru = "Anlasmali",
                durum = "Kapandi"
            });
            var sil = await surucu.Client.DeleteAsync($"/api/Hasar/{dosyaId}");

            Assert.Equal(HttpStatusCode.OK, foto.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, yasakArac.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, guncelle.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, sil.StatusCode);
        }

        [Fact]
        public async Task YirmiBirinciFotoReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var dosyaId = await DosyaIdAsync(await DosyaAcAsync(sahip, aracId));

            for (var i = 0; i < 20; i++)
            {
                var cevap = await FotoEkleAsync(sahip, dosyaId);
                Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            }

            var fazla = await FotoEkleAsync(sahip, dosyaId);
            var govde = await fazla.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, fazla.StatusCode);
            Assert.Contains("20", govde);

            var dosya = JsonDocument.Parse(await sahip.GetStringAsync($"/api/Hasar/{dosyaId}")).RootElement.GetProperty("data");
            Assert.Equal(20, dosya.GetProperty("fotoSayisi").GetInt32());
        }

        [Theory]
        [InlineData("Uydurma", "Anlasmali")]
        [InlineData("Kaza", "Uydurma")]
        public async Task TanimsizEnumDegeriReddedilir(string tur, string tutanak)
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var cevap = await DosyaAcAsync(sahip, aracId, "2026-08-01", tur, tutanak);

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task TanimsizFotoEtiketiReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var dosyaId = await DosyaIdAsync(await DosyaAcAsync(sahip, aracId));

            var cevap = await FotoEkleAsync(sahip, dosyaId, "Uydurma");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task GelecekTarihliOlayReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var yarin = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
            var bugun = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            var gelecek = await DosyaAcAsync(sahip, aracId, yarin);
            var cokEski = await DosyaAcAsync(sahip, aracId, "1949-12-31");
            var gecerli = await DosyaAcAsync(sahip, aracId, bugun);

            Assert.Equal(HttpStatusCode.BadRequest, gelecek.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, cokEski.StatusCode);
            Assert.Equal(HttpStatusCode.OK, gecerli.StatusCode);
        }

        [Fact]
        public async Task TutanakSayfasiOlayiVeBilgiDegisimAlaniniTasir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var dosyaId = await DosyaIdAsync(await DosyaAcAsync(sahip, aracId));
            await FotoEkleAsync(sahip, dosyaId, "Plakalar");

            var cevap = await sahip.GetAsync($"/api/Hasar/{dosyaId}/tutanak.html");
            var html = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains("text/html", cevap.Content.Headers.ContentType.ToString());
            Assert.Contains("01.08.2026", html);
            Assert.Contains("06AB123", html);
            Assert.Contains("Anlaşmalı tutanak", html);
            Assert.Contains("Bilgi değişim alanı", html);
            Assert.Contains("Karşı sürücü adı soyadı", html);
            Assert.Contains("Plakalar", html);
        }

        [Fact]
        public async Task FotoYalnizKendiSirketininBelgesineBaglanir()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci);
            var dosyaId = await DosyaIdAsync(await DosyaAcAsync(birinci, aracId));
            await FotoEkleAsync(birinci, dosyaId);

            var dosya = JsonDocument.Parse(await birinci.GetStringAsync($"/api/Hasar/{dosyaId}")).RootElement.GetProperty("data");
            var belgeId = dosya.GetProperty("fotograflar")[0].GetProperty("documentId").GetInt32();

            var ikinci = await SahipOlusturAsync();
            var yabanciIndir = await ikinci.GetAsync($"/api/Documents/{belgeId}/download");
            var sahipIndir = await birinci.GetAsync($"/api/Documents/{belgeId}/download");

            Assert.Equal(HttpStatusCode.NotFound, yabanciIndir.StatusCode);
            Assert.Equal(HttpStatusCode.OK, sahipIndir.StatusCode);
        }

        [Fact]
        public async Task DosyaSilininceFotolarVeBelgelerKaliciSilinirKotaDuser()
        {
            var klasor = Path.Combine(Path.GetTempPath(), "garajim-hasar-kota-" + Guid.NewGuid().ToString("N"));
            using var darFactory = new HasarFactory(klasor, 5 * 1024);

            var client = darFactory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("hasarkota"), fullName = "Kota Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await kayit.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var aracId = await AracEkleAsync(client);
            var dosyaId = await DosyaIdAsync(await DosyaAcAsync(client, aracId));

            var ilk = await FotoEkleAsync(client, dosyaId, "Genel", 2048);
            var ikinci = await FotoEkleAsync(client, dosyaId, "HasarYakin", 2048);
            var kotaDolu = await FotoEkleAsync(client, dosyaId, "Yol", 2048);

            var dosya = JsonDocument.Parse(await client.GetStringAsync($"/api/Hasar/{dosyaId}")).RootElement.GetProperty("data");
            var belgeIdler = dosya.GetProperty("fotograflar").EnumerateArray().Select(f => f.GetProperty("documentId").GetInt32()).ToList();
            var diskteOnce = Directory.Exists(klasor) ? Directory.GetFiles(klasor).Length : 0;

            var sil = await client.DeleteAsync($"/api/Hasar/{dosyaId}");

            var yeniDosyaId = await DosyaIdAsync(await DosyaAcAsync(client, aracId));
            var silmeSonrasi = await FotoEkleAsync(client, yeniDosyaId, "Genel", 2048);
            var diskteSonra = Directory.GetFiles(klasor).Length;

            Assert.Equal(HttpStatusCode.OK, ilk.StatusCode);
            Assert.Equal(HttpStatusCode.OK, ikinci.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, kotaDolu.StatusCode);
            Assert.Equal(2, belgeIdler.Count);
            Assert.Equal(2, diskteOnce);

            Assert.Equal(HttpStatusCode.OK, sil.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Hasar/{dosyaId}")).StatusCode);

            foreach (var belgeId in belgeIdler)
            {
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/Documents/{belgeId}/download")).StatusCode);
            }

            Assert.Equal(HttpStatusCode.OK, silmeSonrasi.StatusCode);
            Assert.Equal(1, diskteSonra);

            if (Directory.Exists(klasor)) Directory.Delete(klasor, true);
        }

        [Fact]
        public async Task PanelAcikHasarDosyasiniSayarKapananiSaymaz()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var acik = await DosyaIdAsync(await DosyaAcAsync(sahip, aracId));
            var kapanacak = await DosyaIdAsync(await DosyaAcAsync(sahip, aracId));

            var oncePanel = JsonDocument.Parse(await sahip.GetStringAsync("/api/Reports/dashboard")).RootElement.GetProperty("data");
            Assert.Equal(2, oncePanel.GetProperty("acikHasarDosyasi").GetInt32());

            var kapat = await sahip.PutAsJsonAsync($"/api/Hasar/{kapanacak}", new
            {
                olayTarihi = "2026-08-01",
                tur = "Kaza",
                konum = "Ankara",
                aciklama = "Onarım tamamlandı.",
                tutanakTuru = "Anlasmali",
                durum = "Kapandi"
            });
            Assert.Equal(HttpStatusCode.OK, kapat.StatusCode);

            var sonraPanel = JsonDocument.Parse(await sahip.GetStringAsync("/api/Reports/dashboard")).RootElement.GetProperty("data");

            Assert.Equal(1, sonraPanel.GetProperty("acikHasarDosyasi").GetInt32());
            Assert.NotEqual(0, acik);
        }

        [Fact]
        public async Task HasarCsvDisaAktarimiTuruTasir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            await DosyaAcAsync(sahip, aracId, "2026-08-01", "Cam", "Yok");

            var cevap = await sahip.GetAsync("/api/Export/hasar.csv");
            var csv = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains("Plaka;OlayTarihi;Tur;Durum", csv);
            Assert.Contains("Cam", csv);
            Assert.Contains("Açık", csv);
            Assert.Contains("01.08.2026", csv);
        }

        [Fact]
        public async Task KarneBayragiKapaliykenHasarGorunmez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var dosyaId = await DosyaIdAsync(await DosyaAcAsync(sahip, aracId));
            await FotoEkleAsync(sahip, dosyaId);

            var kapali = await sahip.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new
            {
                kapsam = new { bakimGecmisi = true, hasarGecmisi = false }
            });
            var kapaliToken = Token(await kapali.Content.ReadAsStringAsync());

            var anonim = _factory.CreateClient();
            var kapaliKarne = JsonDocument.Parse(await anonim.GetStringAsync($"/api/karne/{kapaliToken}")).RootElement.GetProperty("data");

            Assert.Equal(0, kapaliKarne.GetProperty("hasarlar").GetArrayLength());

            var acik = await sahip.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new
            {
                kapsam = new { bakimGecmisi = true, hasarGecmisi = true }
            });
            var acikToken = Token(await acik.Content.ReadAsStringAsync());

            var acikKarne = JsonDocument.Parse(await anonim.GetStringAsync($"/api/karne/{acikToken}")).RootElement.GetProperty("data");
            var satir = acikKarne.GetProperty("hasarlar")[0];

            Assert.Equal(1, acikKarne.GetProperty("hasarlar").GetArrayLength());
            Assert.Equal("Kaza", satir.GetProperty("tur").GetString());
            Assert.Equal("Açık", satir.GetProperty("durum").GetString());
            Assert.False(satir.TryGetProperty("hasarBedeli", out _));
            Assert.False(satir.TryGetProperty("fotograflar", out _));
            Assert.False(satir.TryGetProperty("konum", out _));

            var govde = await anonim.GetStringAsync($"/api/karne/{acikToken}");
            Assert.DoesNotContain("18500", govde);
        }

        private static string Token(string govde)
        {
            var url = JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("url").GetString();
            return url.Substring(url.IndexOf("?t=", StringComparison.Ordinal) + 3);
        }
    }
}
