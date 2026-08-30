using System.Net;
using System.Text;
using System.Text.Json;
using Garajim.Business.Concrete.Receipts;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Tests.Unit
{
    public class ReceiptExtractorTests
    {
        private sealed class SahteHandler : HttpMessageHandler
        {
            private readonly Queue<Func<HttpResponseMessage>> _cevaplar = new Queue<Func<HttpResponseMessage>>();

            public int CagriSayisi { get; private set; }

            public void Kuyrukla(HttpStatusCode kod, string govde)
            {
                _cevaplar.Enqueue(() => new HttpResponseMessage(kod)
                {
                    Content = new StringContent(govde, Encoding.UTF8, "application/json")
                });
            }

            public void KuyruklaTimeout()
            {
                _cevaplar.Enqueue(() => throw new TaskCanceledException("zaman aşımı"));
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CagriSayisi++;
                return Task.FromResult(_cevaplar.Dequeue()());
            }
        }

        private sealed class SahteFactory : IHttpClientFactory
        {
            private readonly HttpMessageHandler _handler;

            public SahteFactory(HttpMessageHandler handler)
            {
                _handler = handler;
            }

            public HttpClient CreateClient(string name)
            {
                return new HttpClient(_handler, disposeHandler: false);
            }
        }

        private static readonly byte[] OrnekGoruntu = { 0xFF, 0xD8, 0xFF, 0x01 };

        private static IConfiguration Yapilandirma(string provider = "Gemini")
        {
            return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Receipts:Provider"] = provider,
                ["Receipts:ApiKey"] = "test-anahtar",
                ["Receipts:Model"] = "test-model"
            }).Build();
        }

        private static string ModelJson()
        {
            return JsonSerializer.Serialize(new
            {
                tarih = "2026-08-15",
                toplamTutar = 1250.50,
                kdvTutari = 190.75,
                litre = 25.40,
                birimFiyat = 49.23,
                plaka = "34 abc 123",
                km = 123456,
                tahminiTur = "Yakit",
                kalemler = new[] { new { ad = "KURŞUNSUZ 95", tutar = 1250.50 } },
                guvenSkoru = 0.92
            });
        }

        private static string GeminiCevabi(string metin)
        {
            return JsonSerializer.Serialize(new
            {
                candidates = new[]
                {
                    new { content = new { parts = new[] { new { text = metin } } } }
                }
            });
        }

        private static string OpenAiCevabi(string metin)
        {
            return JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new { message = new { content = metin } }
                }
            });
        }

        private static GeminiReceiptExtractor GeminiOlustur(SahteHandler handler)
        {
            return new GeminiReceiptExtractor(new SahteFactory(handler), Yapilandirma(), NullLogger<GeminiReceiptExtractor>.Instance);
        }

        [Fact]
        public async Task BasariliYanitTumAlanlaraParseEdilir()
        {
            var handler = new SahteHandler();
            handler.Kuyrukla(HttpStatusCode.OK, GeminiCevabi(ModelJson()));

            var sonuc = await GeminiOlustur(handler).ExtractAsync(OrnekGoruntu, "image/jpeg", CancellationToken.None);

            Assert.Equal(new DateTime(2026, 8, 15), sonuc.Tarih);
            Assert.Equal(1250.50m, sonuc.ToplamTutar);
            Assert.Equal(190.75m, sonuc.KdvTutari);
            Assert.Equal(25.40m, sonuc.Litre);
            Assert.Equal(49.23m, sonuc.BirimFiyat);
            Assert.Equal("34ABC123", sonuc.Plaka);
            Assert.Equal(123456, sonuc.Km);
            Assert.Equal(ReceiptType.Yakit, sonuc.TahminiTur);
            Assert.Single(sonuc.KalemListesi);
            Assert.Equal("KURŞUNSUZ 95", sonuc.KalemListesi[0].Ad);
            Assert.Equal(0.92, sonuc.GuvenSkoru, 3);
        }

        [Fact]
        public async Task CitliJsonYanitiDaParseEdilir()
        {
            var handler = new SahteHandler();
            handler.Kuyrukla(HttpStatusCode.OK, GeminiCevabi("```json\n" + ModelJson() + "\n```"));

            var sonuc = await GeminiOlustur(handler).ExtractAsync(OrnekGoruntu, "image/jpeg", CancellationToken.None);

            Assert.Equal(1250.50m, sonuc.ToplamTutar);
        }

        [Fact]
        public async Task BozukJsonBosSonucVeSifirGuvenDoner()
        {
            var handler = new SahteHandler();
            handler.Kuyrukla(HttpStatusCode.OK, GeminiCevabi("Bu bir fiş değil, JSON da değil."));

            var sonuc = await GeminiOlustur(handler).ExtractAsync(OrnekGoruntu, "image/jpeg", CancellationToken.None);

            Assert.Equal(0, sonuc.GuvenSkoru);
            Assert.Null(sonuc.Tarih);
            Assert.Null(sonuc.ToplamTutar);
            Assert.Equal(ReceiptType.Bilinmiyor, sonuc.TahminiTur);
        }

        [Fact]
        public async Task TimeoutIstisnaFirlatmazBosSonucDoner()
        {
            var handler = new SahteHandler();
            handler.KuyruklaTimeout();
            handler.KuyruklaTimeout();

            var sonuc = await GeminiOlustur(handler).ExtractAsync(OrnekGoruntu, "image/jpeg", CancellationToken.None);

            Assert.Equal(0, sonuc.GuvenSkoru);
            Assert.Equal(2, handler.CagriSayisi);
        }

        [Fact]
        public async Task SunucuHatasindaTekTekrarSonraBosSonuc()
        {
            var handler = new SahteHandler();
            handler.Kuyrukla(HttpStatusCode.InternalServerError, "hata");
            handler.Kuyrukla(HttpStatusCode.TooManyRequests, "hata");

            var sonuc = await GeminiOlustur(handler).ExtractAsync(OrnekGoruntu, "image/jpeg", CancellationToken.None);

            Assert.Equal(0, sonuc.GuvenSkoru);
            Assert.Equal(2, handler.CagriSayisi);
        }

        [Fact]
        public async Task IlkDenemede429IkincideBasariliysaSonucDoner()
        {
            var handler = new SahteHandler();
            handler.Kuyrukla(HttpStatusCode.TooManyRequests, "hata");
            handler.Kuyrukla(HttpStatusCode.OK, GeminiCevabi(ModelJson()));

            var sonuc = await GeminiOlustur(handler).ExtractAsync(OrnekGoruntu, "image/jpeg", CancellationToken.None);

            Assert.Equal(1250.50m, sonuc.ToplamTutar);
            Assert.Equal(2, handler.CagriSayisi);
        }

        [Fact]
        public async Task AnahtarYapilandirilmamissaCagriYapilmazBosSonucDoner()
        {
            var handler = new SahteHandler();
            var yapilandirma = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
            var extractor = new GeminiReceiptExtractor(new SahteFactory(handler), yapilandirma, NullLogger<GeminiReceiptExtractor>.Instance);

            var sonuc = await extractor.ExtractAsync(OrnekGoruntu, "image/jpeg", CancellationToken.None);

            Assert.Equal(0, sonuc.GuvenSkoru);
            Assert.Equal(0, handler.CagriSayisi);
        }

        [Fact]
        public async Task OpenAiSaglayicisiAyniSemayiParseEder()
        {
            var handler = new SahteHandler();
            handler.Kuyrukla(HttpStatusCode.OK, OpenAiCevabi(ModelJson()));
            var extractor = new OpenAiReceiptExtractor(new SahteFactory(handler), Yapilandirma("OpenAI"), NullLogger<OpenAiReceiptExtractor>.Instance);

            var sonuc = await extractor.ExtractAsync(OrnekGoruntu, "image/jpeg", CancellationToken.None);

            Assert.Equal(1250.50m, sonuc.ToplamTutar);
            Assert.Equal("34ABC123", sonuc.Plaka);
            Assert.Equal(ReceiptType.Yakit, sonuc.TahminiTur);
        }

        [Theory]
        [InlineData("34 abc 123", "34ABC123")]
        [InlineData("06-DEF-45", "06DEF45")]
        [InlineData("  34 ab 1234  ", "34AB1234")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void PlakaNormalizeEdilir(string girdi, string beklenen)
        {
            Assert.Equal(beklenen, ReceiptResponseParser.PlakayiNormalizeEt(girdi));
        }

        [Theory]
        [InlineData("15.08.2026")]
        [InlineData("2026-08-15")]
        public void TarihIkiFormattanDaOkunur(string tarih)
        {
            var json = "{\"tarih\":\"" + tarih + "\",\"guvenSkoru\":0.5}";

            var sonuc = ReceiptResponseParser.Parse(json);

            Assert.Equal(new DateTime(2026, 8, 15), sonuc.Tarih);
        }
    }
}
