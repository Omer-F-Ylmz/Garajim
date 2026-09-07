using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Garajim.KatalogUretici
{
    public static class JsonYazici
    {
        public const int EnBuyukBoyut = 4 * 1024 * 1024;

        private static readonly JsonSerializerOptions Secenekler = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };

        public static string Yaz(GlobalKatalogBelgesi belge)
        {
            return JsonSerializer.Serialize(belge, Secenekler);
        }

        public static int Boyut(GlobalKatalogBelgesi belge)
        {
            return Encoding.UTF8.GetByteCount(Yaz(belge));
        }

        public static bool SinirAsildi(GlobalKatalogBelgesi belge)
        {
            return Boyut(belge) > EnBuyukBoyut;
        }

        public static string BoyutRaporu(GlobalKatalogBelgesi belge, int kacMarka)
        {
            var boyut = Boyut(belge);
            var yazi = new StringBuilder();

            yazi.AppendLine("Katalog boyutu: " + boyut + " bayt (sinir " + EnBuyukBoyut + " bayt)");
            yazi.AppendLine("Marka: " + belge.Markalar.Count + ", seri: " + belge.Markalar.Sum(m => m.Seriler.Count));
            yazi.AppendLine("En kalabalik " + kacMarka + " marka:");

            foreach (var marka in belge.Markalar.OrderByDescending(m => m.Seriler.Count).ThenBy(m => m.Ad, StringComparer.Ordinal).Take(kacMarka))
            {
                yazi.AppendLine("  " + marka.Ad + ": " + marka.Seriler.Count);
            }

            return yazi.ToString();
        }
    }
}
