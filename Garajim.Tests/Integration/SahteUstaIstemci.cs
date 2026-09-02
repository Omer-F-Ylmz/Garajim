using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Garajim.Business.Usta;

namespace Garajim.Tests.Integration
{
    public sealed class SahteGeminiHandler : HttpMessageHandler
    {
        private readonly Func<string, HttpResponseMessage> _uretici;

        public List<string> Istekler { get; } = new List<string>();

        public SahteGeminiHandler(Func<string, HttpResponseMessage> uretici)
        {
            _uretici = uretici;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var govde = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Istekler.Add(govde);
            return _uretici(govde);
        }

        public static HttpResponseMessage Cevap(string yanitJson, int? tokenGiris = 1200, int? tokenCikis = 300)
        {
            var govde = JsonSerializer.Serialize(new
            {
                candidates = new[]
                {
                    new { content = new { parts = new[] { new { text = yanitJson } } } }
                },
                usageMetadata = new { promptTokenCount = tokenGiris, candidatesTokenCount = tokenCikis }
            });

            var cevap = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(govde, Encoding.UTF8, "application/json")
            };
            cevap.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return cevap;
        }

        public static HttpResponseMessage Hata(HttpStatusCode durum)
        {
            return new HttpResponseMessage(durum) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        }

        public static string GecerliYanit(string ozet = "Fren balatası aşınmış olabilir.")
        {
            return JsonSerializer.Serialize(new
            {
                ozet,
                kirmiziCizgi = false,
                kademeler = new[]
                {
                    new
                    {
                        kademe = "EnSik",
                        neden = "Ön fren balatası",
                        belirtiUyumu = "Frende ses tarif ediyorsun",
                        evdeKontrol = "Jant arasından balata kalınlığına bak",
                        maliyetTl = new[] { 1500, 3500 },
                        aciliyet = "BuHafta"
                    },
                    new
                    {
                        kademe = "Sik",
                        neden = "Fren diski salgılı",
                        belirtiUyumu = "Titreme de varsa uyumlu",
                        evdeKontrol = "Disk yüzeyinde iz var mı bak",
                        maliyetTl = new[] { 3000, 7000 },
                        aciliyet = "Bakimda"
                    }
                },
                aracVerisindenNotlar = new[] { "Bu araçta son bakım 118.000 km'de yapılmış." },
                ustayaBoyleAnlat = "Frende ses ve titreme var. Ön balata ve disk kontrol edilsin.",
                takipSorulari = new[] { "Ses yalnız frende mi çıkıyor?" },
                uyari = "Bu bir tahmindir, teşhis değildir."
            });
        }
    }

    public sealed class SahteUstaIstemci : IUstaIstemci
    {
        public List<(string SabitBlok, string AracBaglami, List<(string Rol, string Metin)> Gecmis, string Soru)> Cagrilar { get; } = new();

        public Func<string, UstaIstemciSonucu> Uretici { get; set; }

        public Task<UstaIstemciSonucu> SorAsync(string sabitBlok, string aracBaglami, IReadOnlyList<(string Rol, string Metin)> gecmis, string soru, CancellationToken ct)
        {
            Cagrilar.Add((sabitBlok, aracBaglami, gecmis.ToList(), soru));
            return Task.FromResult(Uretici != null ? Uretici(soru) : Varsayilan());
        }

        public static UstaIstemciSonucu Varsayilan()
        {
            return new UstaIstemciSonucu
            {
                Yanit = JsonSerializer.Deserialize<Garajim.Entity.Dtos.UstaYanitDto>(
                    SahteGeminiHandler.GecerliYanit(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                TokenGiris = 1200,
                TokenCikis = 300,
                SureMs = 42
            };
        }
    }
}
