using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Garajim.Business.Concrete.Receipts
{
    public class GeminiReceiptExtractor : ReceiptExtractorBase
    {
        public GeminiReceiptExtractor(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiReceiptExtractor> logger)
            : base(httpClientFactory, configuration, logger)
        {
        }

        public const string VarsayilanModelAdi = "gemini-3.5-flash-lite";

        protected override string VarsayilanModel => VarsayilanModelAdi;

        protected override HttpRequestMessage IstekOlustur(string model, string apiKey, byte[] imageBytes, string mimeType)
        {
            var tampon = IstekGovdesi.Tampon(imageBytes.Length);

            using (var yazici = new Utf8JsonWriter(tampon))
            {
                yazici.WriteStartObject();

                yazici.WriteStartArray("contents");
                yazici.WriteStartObject();
                yazici.WriteStartArray("parts");

                yazici.WriteStartObject();
                yazici.WriteString("text", ReceiptResponseParser.Prompt);
                yazici.WriteEndObject();

                yazici.WriteStartObject();
                yazici.WriteStartObject("inline_data");
                yazici.WriteString("mime_type", mimeType);
                yazici.WriteBase64String("data", imageBytes);
                yazici.WriteEndObject();
                yazici.WriteEndObject();

                yazici.WriteEndArray();
                yazici.WriteEndObject();
                yazici.WriteEndArray();

                yazici.WriteStartObject("generationConfig");
                yazici.WriteNumber("temperature", 0);
                yazici.WriteString("response_mime_type", "application/json");
                yazici.WriteStartObject("thinkingConfig");
                yazici.WriteNumber("thinkingBudget", 0);
                yazici.WriteEndObject();
                yazici.WriteEndObject();

                yazici.WriteEndObject();
            }

            var istek = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
            {
                Content = IstekGovdesi.Icerik(tampon)
            };
            istek.Headers.Add("x-goog-api-key", apiKey);
            return istek;
        }

        protected override (int Giris, int Cikis) TokenSayilari(JsonElement kok)
        {
            if (!kok.TryGetProperty("usageMetadata", out var kullanim))
            {
                return (0, 0);
            }

            var giris = kullanim.TryGetProperty("promptTokenCount", out var g) ? g.GetInt32() : 0;
            var cikis = kullanim.TryGetProperty("candidatesTokenCount", out var c) ? c.GetInt32() : 0;

            return (giris, cikis);
        }

        protected override string MetniCikar(JsonElement kok)
        {
            var parts = kok.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
            var birlesik = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                {
                    birlesik.Append(text.GetString());
                }
            }

            return birlesik.ToString();
        }
    }
}
