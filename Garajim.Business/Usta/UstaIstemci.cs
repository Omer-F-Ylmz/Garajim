using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Garajim.Entity.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Garajim.Business.Usta
{
    public class UstaIstemciSonucu
    {
        public UstaYanitDto Yanit { get; set; }
        public string HamMetin { get; set; }
        public int TokenGiris { get; set; }
        public int TokenCikis { get; set; }
        public int SureMs { get; set; }
        public string Hata { get; set; }
    }

    public interface IUstaIstemci
    {
        Task<UstaIstemciSonucu> SorAsync(string sabitBlok, string aracBaglami, IReadOnlyList<(string Rol, string Metin)> gecmis, string soru, CancellationToken ct);
    }

    public class GeminiUstaIstemci : IUstaIstemci
    {
        public const string HttpClientName = "usta-istemci";
        private const int DenemeSayisi = 2;

        private static readonly JsonSerializerOptions Secenekler = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiUstaIstemci> _logger;

        public GeminiUstaIstemci(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiUstaIstemci> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<UstaIstemciSonucu> SorAsync(string sabitBlok, string aracBaglami, IReadOnlyList<(string Rol, string Metin)> gecmis, string soru, CancellationToken ct)
        {
            var apiKey = _configuration["Usta:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = _configuration["Receipts:ApiKey"];
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new UstaIstemciSonucu { Hata = "ANAHTAR_YOK" };
            }

            var model = _configuration["Usta:Model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                model = _configuration["Receipts:Model"];
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gemini-2.5-flash";
            }

            var parcalar = new List<object> { new { text = sabitBlok }, new { text = aracBaglami } };
            foreach (var mesaj in gecmis)
            {
                parcalar.Add(new { text = mesaj.Rol + ": " + mesaj.Metin });
            }
            parcalar.Add(new { text = "SORU: " + soru });

            var govde = JsonSerializer.Serialize(new
            {
                contents = new[] { new { parts = parcalar.ToArray() } },
                generationConfig = new { temperature = 0.3, response_mime_type = "application/json" }
            });

            var kronometre = Stopwatch.StartNew();
            string sonHata = null;

            for (var deneme = 1; deneme <= DenemeSayisi; deneme++)
            {
                try
                {
                    using var istek = new HttpRequestMessage(
                        HttpMethod.Post,
                        $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
                    {
                        Content = new StringContent(govde, Encoding.UTF8, "application/json")
                    };
                    istek.Headers.Add("x-goog-api-key", apiKey);

                    var client = _httpClientFactory.CreateClient(HttpClientName);
                    using var cevap = await client.SendAsync(istek, ct);
                    var icerik = await cevap.Content.ReadAsStringAsync(ct);

                    if (!cevap.IsSuccessStatusCode)
                    {
                        sonHata = "HTTP " + (int)cevap.StatusCode;
                        _logger.LogWarning("AI Usta çağrısı {Deneme}. denemede {Durum} döndü.", deneme, (int)cevap.StatusCode);
                        continue;
                    }

                    return Coz(icerik, kronometre);
                }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested)
                {
                    sonHata = "ZAMAN_ASIMI";
                    _logger.LogWarning("AI Usta çağrısı {Deneme}. denemede zaman aşımına uğradı.", deneme);
                }
                catch (HttpRequestException hata)
                {
                    sonHata = "AG_HATASI";
                    _logger.LogWarning(hata, "AI Usta çağrısı {Deneme}. denemede ağ hatası verdi.", deneme);
                }
            }

            kronometre.Stop();
            return new UstaIstemciSonucu { Hata = sonHata ?? "BILINMEYEN", SureMs = (int)kronometre.ElapsedMilliseconds };
        }

        private static UstaIstemciSonucu Coz(string icerik, Stopwatch kronometre)
        {
            kronometre.Stop();
            var sonuc = new UstaIstemciSonucu { SureMs = (int)kronometre.ElapsedMilliseconds };

            try
            {
                using var belge = JsonDocument.Parse(icerik);
                var kok = belge.RootElement;

                if (kok.TryGetProperty("usageMetadata", out var kullanim))
                {
                    if (kullanim.TryGetProperty("promptTokenCount", out var giris))
                    {
                        sonuc.TokenGiris = giris.GetInt32();
                    }
                    if (kullanim.TryGetProperty("candidatesTokenCount", out var cikis))
                    {
                        sonuc.TokenCikis = cikis.GetInt32();
                    }
                }

                var metin = new StringBuilder();
                foreach (var part in kok.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts").EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text))
                    {
                        metin.Append(text.GetString());
                    }
                }

                sonuc.HamMetin = metin.ToString();
                sonuc.Yanit = JsonSerializer.Deserialize<UstaYanitDto>(sonuc.HamMetin, Secenekler);
            }
            catch (Exception hata) when (hata is JsonException || hata is KeyNotFoundException || hata is InvalidOperationException || hata is IndexOutOfRangeException)
            {
                sonuc.Hata = "SEMA_BOZUK";
            }

            return sonuc;
        }
    }
}
