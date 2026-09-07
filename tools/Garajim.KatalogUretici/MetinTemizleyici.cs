using System.Globalization;
using System.Text;

namespace Garajim.KatalogUretici
{
    public static class MetinTemizleyici
    {
        public const int EnUzunAd = 50;
        public const int EnKisaAd = 2;

        private static readonly string[] Elenenler = { "NOT APPLICABLE", "N/A", "NA", "NONE", "UNKNOWN" };

        private static readonly HashSet<string> Kisaltmalar = new(StringComparer.OrdinalIgnoreCase)
        {
            "BMW", "GMC", "MG", "DS", "BYD", "VW", "SRT", "AMG", "KTM", "RAM", "KGM", "REE",
            "GT", "GTI", "TDI", "TSI", "XC", "XF", "XE", "XJ", "SQ", "RS",
            "CLA", "CLS", "EQA", "EQB", "EQC", "EQE", "EQS", "GLA", "GLB", "GLC", "GLE", "GLS",
            "IS", "RX", "NX", "UX", "LX", "GX", "ES", "LS", "LC", "RC",
        };

        public static string Duzelt(string ham)
        {
            if (string.IsNullOrWhiteSpace(ham))
            {
                return null;
            }

            var sade = BosluklariTekle(ham.Trim());

            if (sade.Length < EnKisaAd || sade.Length > EnUzunAd)
            {
                return null;
            }

            if (Array.Exists(Elenenler, e => string.Equals(e, sade, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            if (!KarakterSetiUygun(sade))
            {
                return null;
            }

            if (!sade.Any(char.IsLetter))
            {
                return null;
            }

            return BaslikBicimi(sade);
        }

        public static bool KarakterSetiUygun(string metin)
        {
            foreach (var karakter in metin)
            {
                var uygun = char.IsLetterOrDigit(karakter)
                    || karakter == ' '
                    || karakter == '.'
                    || karakter == '-'
                    || karakter == '/'
                    || karakter == '&';

                if (!uygun)
                {
                    return false;
                }
            }

            return true;
        }

        public static string BaslikBicimi(string metin)
        {
            var yazi = new StringBuilder();
            var parcalar = metin.Split(' ');

            for (var i = 0; i < parcalar.Length; i++)
            {
                if (i > 0)
                {
                    yazi.Append(' ');
                }

                yazi.Append(SozcukBicimi(parcalar[i]));
            }

            return yazi.ToString();
        }

        private static string SozcukBicimi(string sozcuk)
        {
            if (sozcuk.Length == 0)
            {
                return sozcuk;
            }

            if (Kisaltmalar.Contains(sozcuk))
            {
                return sozcuk.ToUpperInvariant();
            }

            if (!sozcuk.Any(char.IsLetter))
            {
                return sozcuk;
            }

            var parcalar = sozcuk.Split('-');

            for (var i = 0; i < parcalar.Length; i++)
            {
                parcalar[i] = TireParcasi(parcalar[i]);
            }

            return string.Join("-", parcalar);
        }

        private static string TireParcasi(string parca)
        {
            if (parca.Length == 0)
            {
                return parca;
            }

            if (Kisaltmalar.Contains(parca))
            {
                return parca.ToUpperInvariant();
            }

            if (!parca.Any(char.IsLetter))
            {
                return parca;
            }

            var bas = char.ToUpperInvariant(parca[0]);
            var kalan = parca.Substring(1);

            return bas + kalan.ToLower(CultureInfo.InvariantCulture);
        }

        private static string BosluklariTekle(string metin)
        {
            var yazi = new StringBuilder(metin.Length);
            var oncekiBosluk = false;

            foreach (var karakter in metin)
            {
                var bosluk = char.IsWhiteSpace(karakter);

                if (bosluk)
                {
                    if (!oncekiBosluk)
                    {
                        yazi.Append(' ');
                    }
                }
                else
                {
                    yazi.Append(karakter);
                }

                oncekiBosluk = bosluk;
            }

            return yazi.ToString().Trim();
        }
    }
}
