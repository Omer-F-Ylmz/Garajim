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

        protected abstract HttpRequestMessage IstekOlustur(string model, string apiKey, byte[] imageBytes, string mimeType, bool dusunmeAyariyla);

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

                var anahtarsiz = ReceiptResponseParser.Bos(null);
                anahtarsiz.CikarimHatasi = "ANAHTAR_YOK";
                return anahtarsiz;
            }

            var model = Configuration["Receipts:Model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                model = VarsayilanModel;
            }

            string sonHata = null;
            var hizmetDolu = false;
            var dusunmeAyariyla = true;

            for (var deneme = 1; deneme <= DenemeSayisi; deneme++)
            {
                try
                {
                    using var istek = IstekOlustur(model, apiKey, imageBytes, mimeType, dusunmeAyariyla);
                    var client = _httpClientFactory.CreateClient(HttpClientName);
                    using var cevap = await client.SendAsync(istek, ct);

                    var govde = await cevap.Content.ReadAsStringAsync(ct);

                    if (!cevap.IsSuccessStatusCode)
                    {
                        sonHata = $"HTTP {(int)cevap.StatusCode}";
                        _logger.LogWarning("Fiş çıkarımı {Deneme}. denemede {Durum} döndü.", deneme, (int)cevap.StatusCode);

                        if (HizmetDoluMu(cevap.StatusCode, govde))
                        {
                            hizmetDolu = true;
                        }
                        else if (dusunmeAyariyla && DusunmeReddedildiMi(cevap.StatusCode, govde))
                        {
                            _logger.LogWarning("Model düşünme ayarını reddetti, ayarsız tekrar denenecek.");
                            dusunmeAyariyla = false;
                        }

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

                        var zarfsiz = ReceiptResponseParser.Bos(govde);
                        zarfsiz.CikarimHatasi = "ZARF_COZULEMEDI";
                        return zarfsiz;
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

            var bos = ReceiptResponseParser.Bos(sonHata);
            bos.HizmetDolu = hizmetDolu;
            bos.CikarimHatasi = sonHata;
            return bos;
        }
        private static bool DusunmeReddedildiMi(System.Net.HttpStatusCode durum, string govde)
        {
            if (durum != System.Net.HttpStatusCode.BadRequest || govde == null)
            {
                return false;
            }

            return govde.Contains("thinking", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HizmetDoluMu(System.Net.HttpStatusCode durum, string govde)
        {
            if (durum == System.Net.HttpStatusCode.TooManyRequests || durum == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                return true;
            }

            return govde != null && govde.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase);
        }

    }
}
