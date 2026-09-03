using System.Text.Json;
using System.Text.Json.Serialization;

namespace Garajim.Business.Katalog
{
    public class KatalogMarkasi
    {
        [JsonPropertyName("ad")]
        public string Ad { get; set; }

        [JsonPropertyName("seriler")]
        public List<string> Seriler { get; set; } = new List<string>();
    }

    public class KatalogBelgesi
    {
        [JsonPropertyName("surum")]
        public string Surum { get; set; }

        [JsonPropertyName("kaynak")]
        public string Kaynak { get; set; }

        [JsonPropertyName("markalar")]
        public List<KatalogMarkasi> Markalar { get; set; } = new List<KatalogMarkasi>();
    }

    public class AracKatalogu
    {
        public const string DosyaAdi = "arac-katalogu.json";
        public const string KlasorAdi = "Katalog";

        private static readonly JsonSerializerOptions Secenekler = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Dictionary<string, KatalogMarkasi> _markalar;
        private readonly Dictionary<string, string> _seriMarkasi;

        private AracKatalogu(string surum, List<KatalogMarkasi> markalar)
        {
            Surum = surum;
            Markalar = markalar;

            _markalar = markalar.ToDictionary(m => m.Ad, m => m, StringComparer.OrdinalIgnoreCase);

            _seriMarkasi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var marka in markalar)
            {
                foreach (var seri in marka.Seriler)
                {
                    _seriMarkasi[seri] = marka.Ad;
                }
            }
        }

        public string Surum { get; }

        public IReadOnlyList<KatalogMarkasi> Markalar { get; }

        public IReadOnlyList<string> MarkaAdlari => Markalar.Select(m => m.Ad).ToList();

        public static AracKatalogu Yukle(string klasor)
        {
            var yol = Path.Combine(klasor ?? string.Empty, DosyaAdi);

            if (!File.Exists(yol))
            {
                throw new InvalidOperationException("Araç kataloğu bulunamadı: " + yol);
            }

            KatalogBelgesi belge;

            try
            {
                belge = JsonSerializer.Deserialize<KatalogBelgesi>(File.ReadAllText(yol), Secenekler);
            }
            catch (JsonException hata)
            {
                throw new InvalidOperationException("Araç kataloğu okunamadı: " + yol, hata);
            }

            Dogrula(belge, yol);

            return new AracKatalogu(belge.Surum, belge.Markalar);
        }

        public bool MarkaVar(string marka)
        {
            return !string.IsNullOrWhiteSpace(marka) && _markalar.ContainsKey(marka.Trim());
        }

        public bool SeriVar(string marka, string seri)
        {
            if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(seri))
            {
                return false;
            }

            return _markalar.TryGetValue(marka.Trim(), out var kayit)
                && kayit.Seriler.Any(s => string.Equals(s, seri.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public string MarkaYazimi(string marka)
        {
            return !string.IsNullOrWhiteSpace(marka) && _markalar.TryGetValue(marka.Trim(), out var kayit)
                ? kayit.Ad
                : null;
        }

        public string SeriYazimi(string marka, string seri)
        {
            if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(seri)
                || !_markalar.TryGetValue(marka.Trim(), out var kayit))
            {
                return null;
            }

            return kayit.Seriler.FirstOrDefault(s => string.Equals(s, seri.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public string SerininMarkasi(string seri)
        {
            return !string.IsNullOrWhiteSpace(seri) && _seriMarkasi.TryGetValue(seri.Trim(), out var marka)
                ? marka
                : null;
        }

        public IReadOnlyList<string> Seriler(string marka)
        {
            return !string.IsNullOrWhiteSpace(marka) && _markalar.TryGetValue(marka.Trim(), out var kayit)
                ? kayit.Seriler
                : Array.Empty<string>();
        }

        private static void Dogrula(KatalogBelgesi belge, string yol)
        {
            if (belge == null || string.IsNullOrWhiteSpace(belge.Surum) || belge.Markalar == null || belge.Markalar.Count == 0)
            {
                throw new InvalidOperationException("Araç kataloğu şeması geçersiz (sürüm ya da marka listesi eksik): " + yol);
            }

            var markaAdlari = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seriSahibi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var marka in belge.Markalar)
            {
                if (marka == null || string.IsNullOrWhiteSpace(marka.Ad))
                {
                    throw new InvalidOperationException("Araç kataloğunda adsız marka var: " + yol);
                }

                if (!markaAdlari.Add(marka.Ad))
                {
                    throw new InvalidOperationException("Araç kataloğunda marka iki kez geçiyor: " + marka.Ad);
                }

                if (marka.Seriler == null || marka.Seriler.Count == 0)
                {
                    throw new InvalidOperationException("Araç kataloğunda serisi olmayan marka var: " + marka.Ad);
                }

                foreach (var seri in marka.Seriler)
                {
                    if (string.IsNullOrWhiteSpace(seri))
                    {
                        throw new InvalidOperationException("Araç kataloğunda boş seri adı var: " + marka.Ad);
                    }

                    if (seriSahibi.TryGetValue(seri, out var oncekiMarka))
                    {
                        throw new InvalidOperationException(
                            $"Araç kataloğunda '{seri}' serisi iki markada geçiyor: {oncekiMarka} ve {marka.Ad}");
                    }

                    seriSahibi[seri] = marka.Ad;
                }
            }
        }
    }
}
