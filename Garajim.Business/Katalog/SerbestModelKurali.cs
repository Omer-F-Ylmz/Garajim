using System.Text.RegularExpressions;

namespace Garajim.Business.Katalog
{
    public static class SerbestModelKurali
    {
        public const int EnAz = 2;
        public const int EnCok = 40;
        public const int MotorEnCok = 30;

        private static readonly Regex IzinliKarakterler = new Regex(@"^[\p{L}\p{Nd} .\-]+$", RegexOptions.Compiled);

        private static readonly Regex DortTekrar = new Regex(@"(.)\1{3,}", RegexOptions.Compiled);

        public static bool Gecerli(string metin, int enCok = EnCok)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return false;
            }

            var temiz = metin.Trim();

            if (temiz.Length < EnAz || temiz.Length > enCok)
            {
                return false;
            }

            if (!IzinliKarakterler.IsMatch(temiz))
            {
                return false;
            }

            if (!temiz.Any(char.IsLetter))
            {
                return false;
            }

            return !DortTekrar.IsMatch(temiz);
        }

        public static bool MotorGecerli(string metin)
        {
            return string.IsNullOrWhiteSpace(metin) || Gecerli(metin, MotorEnCok);
        }
    }
}
