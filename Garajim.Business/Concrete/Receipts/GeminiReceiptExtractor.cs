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

        protected override string VarsayilanModel => "gemini-2.5-flash";

        protected override HttpRequestMessage IstekOlustur(string model, string apiKey, byte[] imageBytes, string mimeType)
        {
            var govde = JsonSerializer.Serialize(new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = ReceiptResponseParser.Prompt },
                            new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(imageBytes) } }
                        }
                    }
                },
                generationConfig = new { temperature = 0, response_mime_type = "application/json" }
            });

            var istek = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
            {
                Content = new StringContent(govde, Encoding.UTF8, "application/json")
            };
            istek.Headers.Add("x-goog-api-key", apiKey);
            return istek;
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
