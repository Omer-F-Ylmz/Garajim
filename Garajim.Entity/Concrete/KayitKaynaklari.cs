namespace Garajim.Entity.Concrete
{
    public static class KayitKaynaklari
    {
        public const int KaynakUzunluk = 50;
        public const int DetayUzunluk = 100;

        public const string Dogrudan = "dogrudan";
        public const string Davet = "davet";
        public const string Rehber = "rehber";
        public const string Tanitim = "tanitim";

        public static readonly string[] Kovalar = { Rehber, Tanitim, Davet, Dogrudan };

        public static string Normalize(string deger, int uzunluk)
        {
            if (string.IsNullOrWhiteSpace(deger))
            {
                return null;
            }

            var temiz = new string(deger.Trim().Where(h => !char.IsControl(h)).ToArray()).Trim();

            if (temiz.Length == 0)
            {
                return null;
            }

            return temiz.Length <= uzunluk ? temiz : temiz.Substring(0, uzunluk);
        }

        public static string Kova(string kaynak)
        {
            if (string.IsNullOrWhiteSpace(kaynak))
            {
                return Dogrudan;
            }

            var sade = kaynak.Trim().ToLowerInvariant();

            return Kovalar.Contains(sade) ? sade : "diger";
        }
    }
}
