using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Garajim.Business.Concrete.Receipts
{
    public class OpenAiReceiptExtractor : ReceiptExtractorBase
    {
        public OpenAiReceiptExtractor(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OpenAiReceiptExtractor> logger)
            : base(httpClientFactory, configuration, logger)
        {
        }

        protected override string VarsayilanModel => "gpt-4.1-nano";

        protected override HttpRequestMessage IstekOlustur(string model, string apiKey, byte[] imageBytes, string mimeType)
        {
            var tampon = IstekGovdesi.Tampon(imageBytes.Length);

            using (var yazici = new Utf8JsonWriter(tampon))
            {
                yazici.WriteStartObject();
                yazici.WriteString("model", model);
                yazici.WriteNumber("temperature", 0);

                yazici.WriteStartObject("response_format");
                yazici.WriteString("type", "json_object");
                yazici.WriteEndObject();

                yazici.WriteStartArray("messages");
                yazici.WriteStartObject();
                yazici.WriteString("role", "user");
                yazici.WriteStartArray("content");

                yazici.WriteStartObject();
                yazici.WriteString("type", "text");
                yazici.WriteString("text", ReceiptResponseParser.Prompt);
                yazici.WriteEndObject();

                yazici.WriteStartObject();
                yazici.WriteString("type", "image_url");
                yazici.WriteStartObject("image_url");
                yazici.WritePropertyName("url");
                yazici.WriteRawValue(IstekGovdesi.VeriUrl(mimeType, imageBytes), skipInputValidation: true);
                yazici.WriteEndObject();
                yazici.WriteEndObject();

                yazici.WriteEndArray();
                yazici.WriteEndObject();
                yazici.WriteEndArray();

                yazici.WriteEndObject();
            }

            var istek = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = IstekGovdesi.Icerik(tampon)
            };
            istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return istek;
        }

        protected override (int Giris, int Cikis) TokenSayilari(JsonElement kok)
        {
            if (!kok.TryGetProperty("usage", out var kullanim))
            {
                return (0, 0);
            }

            var giris = kullanim.TryGetProperty("prompt_tokens", out var g) ? g.GetInt32() : 0;
            var cikis = kullanim.TryGetProperty("completion_tokens", out var c) ? c.GetInt32() : 0;

            return (giris, cikis);
        }

        protected override string MetniCikar(JsonElement kok)
        {
            return kok.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
    }
}
