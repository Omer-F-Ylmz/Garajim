using System.Net.Http.Headers;
using System.Text;
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
            var govde = JsonSerializer.Serialize(new
            {
                model,
                temperature = 0,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = ReceiptResponseParser.Prompt },
                            new
                            {
                                type = "image_url",
                                image_url = new { url = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}" }
                            }
                        }
                    }
                }
            });

            var istek = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(govde, Encoding.UTF8, "application/json")
            };
            istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return istek;
        }

        protected override string MetniCikar(JsonElement kok)
        {
            return kok.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
    }
}
