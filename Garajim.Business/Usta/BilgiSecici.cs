using System.Text;
using System.Text.RegularExpressions;

namespace Garajim.Business.Usta
{
    public class BilgiSecici
    {
        public const int MaxKayit = 25;
        public const int MaxToken = 3000;

        private static readonly Regex DtcDeseni = new Regex(@"\b[PCBU][0-3][0-9A-F]{3}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IReadOnlyList<BilgiKaydi> _kayitlar;
        private readonly Dictionary<string, string[]> _anahtarlar;

        public BilgiSecici(IReadOnlyList<BilgiKaydi> kayitlar)
        {
            _kayitlar = kayitlar;
            _anahtarlar = kayitlar.ToDictionary(
                k => k.Id,
                k => k.Anahtarlar.Select(Normalize).Where(a => a.Length > 0).ToArray(),
                StringComparer.Ordinal);
        }

        public static string Normalize(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(metin.Length);
            var oncekiBosluk = true;

            foreach (var ham in metin)
            {
                var karakter = Katla(ham);

                if (char.IsLetterOrDigit(karakter))
                {
                    sb.Append(karakter);
                    oncekiBosluk = false;
                    continue;
                }

                if (!oncekiBosluk)
                {
                    sb.Append(' ');
                    oncekiBosluk = true;
                }
            }

            return sb.ToString().Trim();
        }

        private static char Katla(char karakter)
        {
            return karakter switch
            {
                'ı' or 'I' or 'İ' or 'i' or 'î' or 'Î' => 'i',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ü' or 'Ü' or 'û' or 'Û' => 'u',
                'ö' or 'Ö' or 'ô' or 'Ô' => 'o',
                'ç' or 'Ç' => 'c',
                'â' or 'Â' => 'a',
                'é' or 'É' or 'è' => 'e',
                _ => char.ToLowerInvariant(karakter)
            };
        }


        public static List<string> DtcKodlari(string soru)
        {
            return DtcDeseni.Matches(soru ?? string.Empty)
                .Select(e => e.Value.ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        public static int TokenTahmini(string metin)
        {
            return string.IsNullOrEmpty(metin) ? 0 : (metin.Length + 3) / 4;
        }

        public List<BilgiKaydi> Sec(string soru)
        {
            var normalSoru = Normalize(soru);
            var kelimeler = normalSoru.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(k => k.Length >= 3)
                .ToHashSet(StringComparer.Ordinal);

            var kodlar = DtcKodlari(soru).Select(Normalize).ToHashSet(StringComparer.Ordinal);
            var puanlar = new List<(BilgiKaydi Kayit, int Puan, int Sira)>();

            for (var i = 0; i < _kayitlar.Count; i++)
            {
                var kayit = _kayitlar[i];
                var anahtarlar = _anahtarlar[kayit.Id];
                var puan = 0;

                foreach (var anahtar in anahtarlar)
                {
                    if (kodlar.Contains(anahtar))
                    {
                        puan += 100;
                        continue;
                    }

                    if (anahtar.Contains(' '))
                    {
                        if (normalSoru.Contains(anahtar, StringComparison.Ordinal))
                        {
                            puan += 10;
                        }
                        continue;
                    }

                    if (kelimeler.Contains(anahtar))
                    {
                        puan += 5;
                    }
                }

                if (puan > 0)
                {
                    puanlar.Add((kayit, puan, i));
                }
            }

            var secilen = new List<BilgiKaydi>();
            var token = 0;

            foreach (var aday in puanlar.OrderByDescending(p => p.Puan).ThenBy(p => p.Sira))
            {
                if (secilen.Count >= MaxKayit)
                {
                    break;
                }

                var maliyet = TokenTahmini(aday.Kayit.Metin);
                if (token + maliyet > MaxToken)
                {
                    continue;
                }

                secilen.Add(aday.Kayit);
                token += maliyet;
            }

            return secilen;
        }
    }
}
