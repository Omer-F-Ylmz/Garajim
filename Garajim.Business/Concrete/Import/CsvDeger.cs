using System.Globalization;

namespace Garajim.Business.Concrete.Import
{
    public static class CsvDeger
    {
        private static readonly string[] TarihBicimleri =
        {
            "dd.MM.yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy",
            "dd.MM.yyyy HH:mm", "yyyy-MM-dd HH:mm", "dd/MM/yyyy HH:mm",
            "yyyy-MM-ddTHH:mm:ss", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss"
        };

        public static DateTime? Tarih(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return null;
            }

            var temiz = metin.Trim();

            if (DateTime.TryParseExact(temiz, TarihBicimleri, CultureInfo.InvariantCulture, DateTimeStyles.None, out var kesin))
            {
                return kesin.Date;
            }

            return DateTime.TryParse(temiz, CultureInfo.InvariantCulture, DateTimeStyles.None, out var genel)
                ? genel.Date
                : null;
        }

        public static decimal? Sayi(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return null;
            }

            var temiz = metin.Trim().Replace(" ", string.Empty);
            if (temiz.Length == 0)
            {
                return null;
            }

            var sonNokta = temiz.LastIndexOf('.');
            var sonVirgul = temiz.LastIndexOf(',');

            string normal;
            if (sonNokta >= 0 && sonVirgul >= 0)
            {
                normal = sonVirgul > sonNokta
                    ? temiz.Replace(".", string.Empty).Replace(',', '.')
                    : temiz.Replace(",", string.Empty);
            }
            else if (sonVirgul >= 0)
            {
                normal = BinlikMi(temiz, ',') ? temiz.Replace(",", string.Empty) : temiz.Replace(',', '.');
            }
            else if (sonNokta >= 0)
            {
                normal = BinlikMi(temiz, '.') ? temiz.Replace(".", string.Empty) : temiz;
            }
            else
            {
                normal = temiz;
            }

            return decimal.TryParse(normal, NumberStyles.Number, CultureInfo.InvariantCulture, out var sonuc)
                ? sonuc
                : null;
        }

        private static bool BinlikMi(string metin, char ayrac)
        {
            var son = metin.LastIndexOf(ayrac);
            var basamak = metin.Length - son - 1;
            return basamak == 3 && metin.Count(k => k == ayrac) >= 1 && son > 0;
        }

        public static int? Tamsayi(string metin)
        {
            var sayi = Sayi(metin);
            return sayi == null || sayi < int.MinValue || sayi > int.MaxValue ? null : (int)sayi.Value;
        }
    }
}
