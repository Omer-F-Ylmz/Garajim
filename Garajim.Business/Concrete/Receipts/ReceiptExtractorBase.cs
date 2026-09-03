using System.Text.Json;
using Garajim.Business.Abstract;
using Garajim.Entity.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Garajim.Business.Concrete.Receipts
{
    public abstract class ReceiptExtractorBase : IReceiptExtractor
    {
        public const string HttpClientName = "receipt-extractor";
        private const int DenemeSayisi = 2;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;

        protected readonly IConfiguration Configuration;

        protected ReceiptExtractorBase(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            Configuration = configuration;
            _logger = logger;
        }

        protected abstract string VarsayilanModel { get; }

        protected abstract HttpRequestMessage IstekOlustur(string model, string apiKey, byte[] imageBytes, string mimeType);

        protected abstract string MetniCikar(JsonElement kok);

        protected virtual (int Giris, int Cikis) TokenSayilari(JsonElement kok)
        {
            return (0, 0);
        }

        public async Task<ReceiptExtractionResult> ExtractAsync(byte[] imageBytes, string mimeType, CancellationToken ct)
        {
            var apiKey = Configuration["Receipts:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Fiş çıkarımı yapılandırılmadı (Receipts__ApiKey boş), boş sonuç dönüldü.");
                return ReceiptResponseParser.Bos(null);
            }

            var model = Configuration["Receipts:Model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                model = VarsayilanModel;
            }

            string sonHata = null;

            for (var deneme = 1; deneme <= DenemeSayisi; deneme++)
            {
                try
                {
                    using var istek = IstekOlustur(model, apiKey, imageBytes, mimeType);
                    var client = _httpClientFactory.CreateClient(HttpClientName);
                    using var cevap = await client.SendAsync(istek, ct);

                    var govde = await cevap.Content.ReadAsStringAsync(ct);

                    if (!cevap.IsSuccessStatusCode)
                    {
                        sonHata = $"HTTP {(int)cevap.StatusCode}";
                        _logger.LogWarning("Fiş çıkarımı {Deneme}. denemede {Durum} döndü.", deneme, (int)cevap.StatusCode);
                        continue;
                    }

                    string metin;
                    var tokenler = (Giris: 0, Cikis: 0);
                    try
                    {
                        using var belge = JsonDocument.Parse(govde);
                        metin = MetniCikar(belge.RootElement);
                        tokenler = TokenSayilari(belge.RootElement);
                    }
                    catch (Exception zarfHatasi) when (zarfHatasi is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
                    {
                        _logger.LogWarning(zarfHatasi, "Fiş çıkarımı sağlayıcı zarfı çözümlenemedi.");
                        return ReceiptResponseParser.Bos(govde);
                    }

                    var sonuc = ReceiptResponseParser.Parse(metin);
                    sonuc.TokenGiris = tokenler.Giris;
                    sonuc.TokenCikis = tokenler.Cikis;
                    return sonuc;
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    sonHata = ex.GetType().Name;
                    _logger.LogWarning(ex, "Fiş çıkarımı {Deneme}. denemede başarısız oldu.", deneme);
                }
            }

            return ReceiptResponseParser.Bos(sonHata);
        }
    }
}
