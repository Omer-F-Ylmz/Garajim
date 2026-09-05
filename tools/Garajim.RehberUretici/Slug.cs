using System.Text;

namespace Garajim.RehberUretici
{
    public static class Slug
    {
        public const int EnCokKarakter = 80;

        public static string Asciile(string metin)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return string.Empty;
            }

            var yazi = new StringBuilder(metin.Length);

            foreach (var harf in metin)
            {
                switch (harf)
                {
                    case 'ç': case 'Ç': yazi.Append('c'); break;
                    case 'ğ': case 'Ğ': yazi.Append('g'); break;
                    case 'ı': case 'I': yazi.Append('i'); break;
                    case 'İ': case 'i': yazi.Append('i'); break;
                    case 'ö': case 'Ö': yazi.Append('o'); break;
                    case 'ş': case 'Ş': yazi.Append('s'); break;
                    case 'ü': case 'Ü': yazi.Append('u'); break;
                    case 'â': case 'Â': yazi.Append('a'); break;
                    case 'î': case 'Î': yazi.Append('i'); break;
                    case 'û': case 'Û': yazi.Append('u'); break;
                    default: yazi.Append(char.ToLowerInvariant(harf)); break;
                }
            }

            return yazi.ToString();
        }

        public static string Uret(string baslik)
        {
            return Uret(baslik, null);
        }

        public static string Uret(string baslik, string yedek)
        {
            var ascii = Asciile(baslik);
            var yazi = new StringBuilder(ascii.Length);

            foreach (var harf in ascii)
            {
                if (harf >= 'a' && harf <= 'z' || harf >= '0' && harf <= '9')
                {
                    yazi.Append(harf);
                }
                else if (yazi.Length > 0 && yazi[yazi.Length - 1] != '-')
                {
                    yazi.Append('-');
                }
            }

            var slug = yazi.ToString().Trim('-');

            if (slug.Length > EnCokKarakter)
            {
                slug = slug.Substring(0, EnCokKarakter);

                var sonTire = slug.LastIndexOf('-');
                if (sonTire >= EnCokKarakter / 2)
                {
                    slug = slug.Substring(0, sonTire);
                }

                slug = slug.Trim('-');
            }

            if (slug.Length == 0)
            {
                return string.IsNullOrWhiteSpace(yedek) ? string.Empty : Uret(yedek, null);
            }

            return slug;
        }

        public static string Tekil(string baslik, string id, HashSet<string> kullanilan)
        {
            var taban = Uret(baslik, id);
            var aday = taban;

            if (kullanilan.Add(aday))
            {
                return aday;
            }

            var idSlug = Uret(id, null);
            aday = Kirp(taban + "-" + idSlug);

            var sayac = 2;
            while (!kullanilan.Add(aday))
            {
                aday = Kirp(taban + "-" + idSlug + "-" + sayac);
                sayac++;
            }

            return aday;
        }

        private static string Kirp(string slug)
        {
            return slug.Length <= EnCokKarakter ? slug : slug.Substring(0, EnCokKarakter).Trim('-');
        }
    }
}
