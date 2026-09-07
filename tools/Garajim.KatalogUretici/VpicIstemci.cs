using System.Text;

namespace Garajim.KatalogUretici
{
    public interface IVpicKaynagi
    {
        Task<string> MarkaYanitiAsync(string aracTipi, CancellationToken iptal);

        Task<string> ModelYanitiAsync(string marka, CancellationToken iptal);
    }

    public sealed class VpicIstemci : IVpicKaynagi, IDisposable
    {
        public const string TabanAdres = "https://vpic.nhtsa.dot.gov/api/vehicles/";

        private readonly HttpClient _istemci;
        private readonly int _denemeSayisi;
        private readonly int _beklemeMs;
        private readonly Action<string> _yaz;

        public VpicIstemci(int denemeSayisi, int beklemeMs, Action<string> yaz)
        {
            _istemci = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _istemci.DefaultRequestHeaders.Add("User-Agent", "Garajim-KatalogUretici/1.0");
            _denemeSayisi = denemeSayisi;
            _beklemeMs = beklemeMs;
            _yaz = yaz ?? (_ => { });
        }

        public Task<string> MarkaYanitiAsync(string aracTipi, CancellationToken iptal) =>
            GetirAsync(TabanAdres + "GetMakesForVehicleType/" + Uri.EscapeDataString(aracTipi) + "?format=json", iptal);

        public Task<string> ModelYanitiAsync(string marka, CancellationToken iptal) =>
            GetirAsync(TabanAdres + "GetModelsForMake/" + Uri.EscapeDataString(marka) + "?format=json", iptal);

        private async Task<string> GetirAsync(string adres, CancellationToken iptal)
        {
            Exception sonHata = null;

            for (var deneme = 1; deneme <= _denemeSayisi; deneme++)
            {
                try
                {
                    using var yanit = await _istemci.GetAsync(adres, iptal);

                    if ((int)yanit.StatusCode == 429 || (int)yanit.StatusCode >= 500)
                    {
                        throw new HttpRequestException("Sunucu " + (int)yanit.StatusCode + " dondu.");
                    }

                    yanit.EnsureSuccessStatusCode();

                    return await yanit.Content.ReadAsStringAsync(iptal);
                }
                catch (Exception hata) when (hata is HttpRequestException || hata is TaskCanceledException)
                {
                    sonHata = hata;

                    if (deneme < _denemeSayisi)
                    {
                        var bekleme = _beklemeMs * deneme * 4;
                        _yaz("  deneme " + deneme + " basarisiz (" + hata.Message + "), " + bekleme + " ms sonra tekrar");
                        await Task.Delay(bekleme, iptal);
                    }
                }
            }

            throw new InvalidOperationException("vPIC istegi basarisiz: " + adres, sonHata);
        }

        public void Dispose() => _istemci.Dispose();
    }

    public sealed class OnbellekliKaynak : IVpicKaynagi
    {
        private readonly IVpicKaynagi _ic;
        private readonly string _klasor;
        private readonly Action<string> _yaz;

        public OnbellekliKaynak(IVpicKaynagi ic, string klasor, Action<string> yaz)
        {
            _ic = ic;
            _klasor = klasor;
            _yaz = yaz ?? (_ => { });

            Directory.CreateDirectory(_klasor);
        }

        public Task<string> MarkaYanitiAsync(string aracTipi, CancellationToken iptal) =>
            OkuYaAlAsync("tip-" + Dosyalastir(aracTipi), () => _ic.MarkaYanitiAsync(aracTipi, iptal));

        public Task<string> ModelYanitiAsync(string marka, CancellationToken iptal) =>
            OkuYaAlAsync("marka-" + Dosyalastir(marka), () => _ic.ModelYanitiAsync(marka, iptal));

        private async Task<string> OkuYaAlAsync(string ad, Func<Task<string>> getir)
        {
            var yol = Path.Combine(_klasor, ad + ".json");

            if (File.Exists(yol))
            {
                return await File.ReadAllTextAsync(yol);
            }

            var icerik = await getir();

            await File.WriteAllTextAsync(yol, icerik);

            return icerik;
        }

        public static string Dosyalastir(string ad)
        {
            var yazi = new StringBuilder(ad.Length);

            foreach (var karakter in ad)
            {
                yazi.Append(char.IsLetterOrDigit(karakter) ? char.ToLowerInvariant(karakter) : '_');
            }

            return yazi.ToString();
        }
    }
}
