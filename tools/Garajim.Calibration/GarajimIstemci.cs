using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Garajim.Calibration
{
    public class LimitAsildiException : Exception
    {
        public LimitAsildiException(string mesaj, bool aylikLimit) : base(mesaj)
        {
            AylikLimit = aylikLimit;
        }

        public bool AylikLimit { get; }
    }

    public class TaslakOzeti
    {
        public DateTime? Tarih { get; set; }
        public decimal? ToplamTutar { get; set; }
        public decimal? Litre { get; set; }
        public string Plaka { get; set; }
        public int? Km { get; set; }
        public string TahminiTur { get; set; }
        public double GuvenSkoru { get; set; }
        public int SureMs { get; set; }
    }

    public class YuklemeSonucu
    {
        public bool HizmetDolu { get; set; }
        public int IstemciSureMs { get; set; }
        public int TaslakId { get; set; }
        public TaslakOzeti Taslak { get; set; }
    }

    public class GarajimIstemci
    {
        private readonly HttpClient _client;

        public GarajimIstemci(HttpClient client)
        {
            _client = client;
        }

        public const string AylikLimitIzi = "limitiniz doldu";

        public static bool AylikLimitMi(string govde)
        {
            return govde != null && govde.Contains(AylikLimitIzi, StringComparison.OrdinalIgnoreCase);
        }

        public async Task GirisYapAsync(string eposta, string sifre)
        {
            var govde = JsonSerializer.Serialize(new { email = eposta, password = sifre });
            using var cevap = await _client.PostAsync("/api/Auth/login",
                new StringContent(govde, Encoding.UTF8, "application/json"));

            var metin = await cevap.Content.ReadAsStringAsync();
            if (!cevap.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Giriş başarısız: " + metin);
            }

            using var belge = JsonDocument.Parse(metin);
            var token = belge.RootElement.GetProperty("data").GetProperty("token").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<YuklemeSonucu> FisYukleAsync(byte[] icerik, string dosyaAdi)
        {
            using var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(icerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue(TipTahmin(dosyaAdi));
            form.Add(dosya, "file", dosyaAdi);

            var kronometre = System.Diagnostics.Stopwatch.StartNew();
            using var cevap = await _client.PostAsync("/api/Receipts?otoOnay=false", form);
            var metin = await cevap.Content.ReadAsStringAsync();
            kronometre.Stop();

            if (cevap.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return new YuklemeSonucu { HizmetDolu = true, IstemciSureMs = (int)kronometre.ElapsedMilliseconds };
            }

            if (cevap.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var aylik = AylikLimitMi(metin);

                throw new LimitAsildiException(
                    aylik
                        ? "Aylık fiş okuma limiti doldu, kalibrasyon durduruldu."
                        : "Hız sınırına takıldı; --bekle değerini artırıp kalan dosyalarla devam edin.",
                    aylik);
            }

            if (!cevap.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Yükleme başarısız: " + metin);
            }

            using var belge = JsonDocument.Parse(metin);
            var veri = belge.RootElement.GetProperty("data");
            var taslak = veri.GetProperty("taslak");

            return new YuklemeSonucu
            {
                IstemciSureMs = (int)kronometre.ElapsedMilliseconds,
                TaslakId = veri.GetProperty("taslakId").GetInt32(),
                Taslak = new TaslakOzeti
                {
                    Tarih = Tarih(taslak, "tarih"),
                    ToplamTutar = Ondalik(taslak, "toplamTutar"),
                    Litre = Ondalik(taslak, "litre"),
                    Plaka = Metin(taslak, "plaka"),
                    Km = Tamsayi(taslak, "km"),
                    TahminiTur = Metin(taslak, "tahminiTur"),
                    GuvenSkoru = taslak.TryGetProperty("guvenSkoru", out var g) ? g.GetDouble() : 0,
                    SureMs = taslak.TryGetProperty("sureMs", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0
                }
            };
        }

        public async Task<int> AracIdBulAsync(string plaka)
        {
            using var cevap = await _client.GetAsync("/api/Vehicles");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());

            foreach (var arac in belge.RootElement.GetProperty("data").EnumerateArray())
            {
                if (CevapAnahtari.PlakaNormalize(arac.GetProperty("plate").GetString()) == CevapAnahtari.PlakaNormalize(plaka))
                {
                    return arac.GetProperty("id").GetInt32();
                }
            }

            return 0;
        }

        public async Task<int> IlkAracIdAsync()
        {
            using var cevap = await _client.GetAsync("/api/Vehicles");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            foreach (var arac in belge.RootElement.GetProperty("data").EnumerateArray())
            {
                return arac.GetProperty("id").GetInt32();
            }

            return 0;
        }

        public async Task OnaylaAsync(int taslakId, int aracId, string tur, DateTime? tarih, decimal? tutar, int? kilometre, decimal? litre)
        {
            var govde = JsonSerializer.Serialize(new
            {
                vehicleId = aracId,
                tur,
                tarih = (tarih ?? DateTime.UtcNow.Date).ToString("yyyy-MM-dd"),
                tutar = tutar ?? 0.01m,
                km = kilometre,
                litre
            });

            using var cevap = await _client.PostAsync($"/api/Receipts/{taslakId}/confirm",
                new StringContent(govde, Encoding.UTF8, "application/json"));

            if (!cevap.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Onay başarısız: " + await cevap.Content.ReadAsStringAsync());
            }
        }

        public async Task<string> StatsAsync()
        {
            using var cevap = await _client.GetAsync("/api/Receipts/stats");
            return await cevap.Content.ReadAsStringAsync();
        }

        private static string TipTahmin(string ad)
        {
            var uzanti = Path.GetExtension(ad).ToLowerInvariant();
            return uzanti == ".png" ? "image/png" : uzanti == ".pdf" ? "application/pdf" : "image/jpeg";
        }

        private static string Metin(JsonElement kok, string ad)
        {
            return kok.TryGetProperty(ad, out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
        }

        private static decimal? Ondalik(JsonElement kok, string ad)
        {
            return kok.TryGetProperty(ad, out var e) && e.ValueKind == JsonValueKind.Number ? e.GetDecimal() : null;
        }

        private static int? Tamsayi(JsonElement kok, string ad)
        {
            return kok.TryGetProperty(ad, out var e) && e.ValueKind == JsonValueKind.Number ? e.GetInt32() : null;
        }

        private static DateTime? Tarih(JsonElement kok, string ad)
        {
            return kok.TryGetProperty(ad, out var e) && e.ValueKind == JsonValueKind.String && DateTime.TryParse(e.GetString(), out var t)
                ? t.Date
                : null;
        }
    }
}
