using System.Globalization;
using System.Text;

namespace Garajim.Calibration
{
    public class CevapSatiri
    {
        public string Dosya { get; set; }
        public string Zorluk { get; set; }
        public string Tur { get; set; }
        public DateTime? Tarih { get; set; }
        public decimal? Tutar { get; set; }
        public int? Km { get; set; }
        public string Plaka { get; set; }
        public decimal? Litre { get; set; }
        public string Aciklama { get; set; }
    }

    public static class CevapAnahtari
    {
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

        public static List<CevapSatiri> Oku(string yol)
        {
            var satirlar = new List<CevapSatiri>();
            var tumu = File.ReadAllLines(yol, Encoding.UTF8);

            for (var i = 1; i < tumu.Length; i++)
            {
                var satir = tumu[i];
                if (string.IsNullOrWhiteSpace(satir))
                {
                    continue;
                }

                var p = satir.Split(';');
                satirlar.Add(new CevapSatiri
                {
                    Dosya = Al(p, 0),
                    Zorluk = (Al(p, 1) ?? "belirsiz").ToLowerInvariant(),
                    Tur = TuruNormalizeEt(Al(p, 2)),
                    Tarih = TarihOku(Al(p, 3)),
                    Tutar = SayiOku(Al(p, 4)),
                    Km = TamsayiOku(Al(p, 5)),
                    Plaka = PlakaNormalize(Al(p, 6)),
                    Litre = SayiOku(Al(p, 7)),
                    Aciklama = Al(p, 8)
                });
            }

            return satirlar;
        }

        private static string Al(string[] parcalar, int sira)
        {
            if (sira >= parcalar.Length)
            {
                return null;
            }

            var deger = parcalar[sira].Trim();
            return string.IsNullOrWhiteSpace(deger) ? null : deger;
        }

        public static string TuruNormalizeEt(string deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
            {
                return null;
            }

            var d = deger.Trim().ToLowerInvariant()
                .Replace('ı', 'i').Replace('İ', 'i')
                .Replace('ş', 's').Replace('ğ', 'g')
                .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');

            return d switch
            {
                "yakit" => "Yakit",
                "bakim" => "Bakim",
                "masraf" => "Masraf",
                _ => null
            };
        }

        public static decimal? SayiOku(string deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
            {
                return null;
            }

            return decimal.TryParse(deger.Trim(), NumberStyles.Number, Tr, out var sonuc) ? sonuc : null;
        }

        public static int? TamsayiOku(string deger)
        {
            var sayi = SayiOku(deger);
            return sayi == null ? null : (int)sayi.Value;
        }

        public static DateTime? TarihOku(string deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
            {
                return null;
            }

            return DateTime.TryParseExact(deger.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sonuc)
                ? sonuc.Date
                : null;
        }

        public static string PlakaNormalize(string plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka))
            {
                return null;
            }

            var temiz = new string(plaka.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            return temiz.Length == 0 ? null : temiz;
        }
    }
}
