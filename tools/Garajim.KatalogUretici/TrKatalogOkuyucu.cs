using System.Text.Json;

namespace Garajim.KatalogUretici
{
    public sealed class TrKatalog
    {
        public TrKatalog(string surum, Dictionary<string, List<string>> markalar)
        {
            Surum = surum;
            Markalar = markalar;
        }

        public string Surum { get; }

        public Dictionary<string, List<string>> Markalar { get; }

        public bool MarkaVar(string ad) => ad != null && Markalar.ContainsKey(ad);

        public bool SeriVar(string marka, string seri)
        {
            if (marka == null || seri == null || !Markalar.TryGetValue(marka, out var seriler))
            {
                return false;
            }

            return seriler.Any(s => string.Equals(s, seri, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class TrKatalogOkuyucu
    {
        public static TrKatalog Oku(string yol)
        {
            if (!File.Exists(yol))
            {
                throw new InvalidOperationException("Türkiye kataloğu bulunamadı: " + yol);
            }

            using var belge = JsonDocument.Parse(File.ReadAllText(yol));
            var kok = belge.RootElement;

            if (!kok.TryGetProperty("surum", out var surum) || !kok.TryGetProperty("markalar", out var markalar))
            {
                throw new InvalidOperationException("Türkiye kataloğu şeması beklenenden farklı: " + yol);
            }

            var sonuc = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var marka in markalar.EnumerateArray())
            {
                var ad = marka.GetProperty("ad").GetString();
                var seriler = marka.GetProperty("seriler").EnumerateArray()
                    .Select(s => s.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                sonuc[ad] = seriler;
            }

            return new TrKatalog(surum.GetString(), sonuc);
        }
    }
}
