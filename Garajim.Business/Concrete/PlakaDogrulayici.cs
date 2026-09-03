using System.Globalization;
using System.Text.RegularExpressions;

namespace Garajim.Business.Concrete
{
    public static class PlakaDogrulayici
    {
        public const int YabanciEnAz = 5;
        public const int YabanciEnCok = 12;

        private const string TurkceHarfler = "çğıöşüÇĞİÖŞÜ";

        private static readonly Regex TurkKurali = new Regex(
            @"^(0[1-9]|[1-7][0-9]|8[01])(?:([A-Z])(\d{4,5})|([A-Z]{2})(\d{3,4})|([A-Z]{3})(\d{2,3}))$",
            RegexOptions.Compiled);

        private static readonly Regex Alfanumerik = new Regex(@"^[A-Z0-9]+$", RegexOptions.Compiled);

        public static string Normalize(string plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka))
            {
                return string.Empty;
            }

            var temiz = plaka.Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(".", string.Empty);

            return temiz.ToUpper(CultureInfo.InvariantCulture);
        }

        public static bool TurkceKarakterTasiyorMu(string plaka)
        {
            return !string.IsNullOrEmpty(plaka) && plaka.Any(k => TurkceHarfler.IndexOf(k) >= 0);
        }

        public static bool Gecerli(string plaka, bool yabanci)
        {
            if (string.IsNullOrWhiteSpace(plaka) || TurkceKarakterTasiyorMu(plaka))
            {
                return false;
            }

            var normal = Normalize(plaka);

            if (!Alfanumerik.IsMatch(normal))
            {
                return false;
            }

            if (yabanci)
            {
                return normal.Length >= YabanciEnAz && normal.Length <= YabanciEnCok;
            }

            return TurkKurali.IsMatch(normal);
        }
    }
}
