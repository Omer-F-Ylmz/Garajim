using System.Text;
using System.Text.Json;
using Garajim.Business.Concrete.Receipts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Tests.Unit
{
    public class FisIstekGovdesiTests
    {
        private sealed class BosFabrika : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new HttpClient();
        }

        private static IConfiguration Bos() => new ConfigurationBuilder().Build();

        private sealed class AcikGemini : GeminiReceiptExtractor
        {
            public AcikGemini()
                : base(new BosFabrika(), Bos(), NullLogger<GeminiReceiptExtractor>.Instance)
            {
            }

            public HttpRequestMessage Govde(byte[] ham, string tip) => IstekOlustur("model-x", "anahtar", ham, tip);
        }

        private sealed class AcikOpenAi : OpenAiReceiptExtractor
        {
            public AcikOpenAi()
                : base(new BosFabrika(), Bos(), NullLogger<OpenAiReceiptExtractor>.Instance)
            {
            }

            public HttpRequestMessage Govde(byte[] ham, string tip) => IstekOlustur("model-y", "anahtar", ham, tip);
        }

        private static byte[] SahteGoruntu(int uzunluk)
        {
            var veri = new byte[uzunluk];
            for (var i = 0; i < uzunluk; i++)
            {
                veri[i] = (byte)(i % 251);
            }

            return veri;
        }

        private static async Task<JsonDocument> GovdeyiCoz(HttpRequestMessage istek)
        {
            var metin = await istek.Content.ReadAsStringAsync();
            return JsonDocument.Parse(metin);
        }

        [Fact]
        public async Task GeminiGovdesiGorseliBase64OlarakTasir()
        {
            var ham = SahteGoruntu(300);
            using var istek = new AcikGemini().Govde(ham, "image/png");
            using var belge = await GovdeyiCoz(istek);

            var parts = belge.RootElement.GetProperty("contents")[0].GetProperty("parts");

            Assert.False(string.IsNullOrWhiteSpace(parts[0].GetProperty("text").GetString()));
            Assert.Equal("image/png", parts[1].GetProperty("inline_data").GetProperty("mime_type").GetString());
            Assert.Equal(Convert.ToBase64String(ham), parts[1].GetProperty("inline_data").GetProperty("data").GetString());
            Assert.Equal("application/json", belge.RootElement.GetProperty("generationConfig").GetProperty("response_mime_type").GetString());
        }

        [Fact]
        public async Task OpenAiGovdesiVeriUrlUretir()
        {
            var ham = SahteGoruntu(300);
            using var istek = new AcikOpenAi().Govde(ham, "image/jpeg");
            using var belge = await GovdeyiCoz(istek);

            var icerik = belge.RootElement.GetProperty("messages")[0].GetProperty("content");

            Assert.False(string.IsNullOrWhiteSpace(icerik[0].GetProperty("text").GetString()));
            Assert.Equal(
                "data:image/jpeg;base64," + Convert.ToBase64String(ham),
                icerik[1].GetProperty("image_url").GetProperty("url").GetString());
            Assert.Equal("model-y", belge.RootElement.GetProperty("model").GetString());
        }

        [Fact]
        public async Task GovdelerUtf8ByteDizisiOlarakGonderilir()
        {
            var ham = SahteGoruntu(300);

            using var gemini = new AcikGemini().Govde(ham, "image/png");
            using var openAi = new AcikOpenAi().Govde(ham, "image/png");

            Assert.IsNotType<StringContent>(gemini.Content);
            Assert.IsNotType<StringContent>(openAi.Content);
            Assert.Equal("application/json", gemini.Content.Headers.ContentType.MediaType);
            Assert.Equal("application/json", openAi.Content.Headers.ContentType.MediaType);

            await Task.CompletedTask;
        }

        [Theory]
        [InlineData("gemini", 2)]
        [InlineData("openai", 3)]
        public void GovdeKurulumuGoruntuBoyununKatiniAsmaz(string saglayici, int katsayi)
        {
            var ham = SahteGoruntu(3 * 1024 * 1024);
            var gemini = new AcikGemini();
            var openAi = new AcikOpenAi();

            var ayrilan = long.MaxValue;

            for (var deneme = 0; deneme < 4; deneme++)
            {
                var once = GC.GetAllocatedBytesForCurrentThread();
                using (var istek = saglayici == "gemini" ? gemini.Govde(ham, "image/png") : openAi.Govde(ham, "image/png"))
                {
                    Assert.NotNull(istek.Content);
                }

                ayrilan = Math.Min(ayrilan, GC.GetAllocatedBytesForCurrentThread() - once);
            }
            var tavan = ham.Length * (long)katsayi;

            Assert.True(
                ayrilan < tavan,
                $"{saglayici} gövdesi {ayrilan / 1024 / 1024} MB ayırdı, {ham.Length / 1024 / 1024} MB görüntü için üst sınır {tavan / 1024 / 1024} MB.");
        }
    }
}
