using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Garajim.Business.Katalog
{
    public class UygunsuzIfadeFiltresi
    {
        public const string KlasorAdi = "Katalog";
        public const string DosyaAdi = "uygunsuz-ifadeler.json";

        private static readonly Lazy<UygunsuzIfadeFiltresi> VarsayilanFiltre =
            new Lazy<UygunsuzIfadeFiltresi>(() => Yukle(Path.Combine(AppContext.BaseDirectory, KlasorAdi)));

        private readonly HashSet<string> _kokler;

        private UygunsuzIfadeFiltresi(HashSet<string> kokler)
        {
            _kokler = kokler;
        }

        public static UygunsuzIfadeFiltresi Varsayilan => VarsayilanFiltre.Value;

        public IReadOnlyCollection<string> Kokler => _kokler;

        public static UygunsuzIfadeFiltresi Yukle(string klasor)
        {
            var yol = Path.Combine(klasor, DosyaAdi);

            if (!File.Exists(yol))
            {
                throw new InvalidOperationException($"Uygunsuz ifade listesi bulunamadı: {yol}");
            }

            Liste liste;

            try
            {
                liste = JsonSerializer.Deserialize<Liste>(File.ReadAllText(yol), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException hata)
            {
                throw new InvalidOperationException($"Uygunsuz ifade listesi okunamadı: {yol}", hata);
            }

            if (liste?.Kokler == null || liste.Kokler.Count == 0)
            {
                throw new InvalidOperationException($"Uygunsuz ifade listesi boş: {yol}");
            }

            var kokler = new HashSet<string>(StringComparer.Ordinal);

            foreach (var kok in liste.Kokler)
            {
                var sade = Sadelestir(kok);

                if (sade.Length == 0)
                {
                    throw new InvalidOperationException($"Uygunsuz ifade listesinde boş sözcük var: {yol}");
                }

                kokler.Add(sade);
            }

            return new UygunsuzIfadeFiltresi(kokler);
        }

        public bool Temiz(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return true;
            }

            var sozcuk = new StringBuilder();

            foreach (var harf in metin)
            {
                if (char.IsLetterOrDigit(harf))
                {
                    sozcuk.Append(harf);
                    continue;
                }

                if (SozcukYasakli(sozcuk))
                {
                    return false;
                }

                sozcuk.Clear();
            }

            return !SozcukYasakli(sozcuk);
        }

        private bool SozcukYasakli(StringBuilder sozcuk)
        {
            return sozcuk.Length > 0 && _kokler.Contains(Sadelestir(sozcuk.ToString()));
        }

        private static string Sadelestir(string metin)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return string.Empty;
            }

            var katlanmis = metin
                .Replace('ı', 'i').Replace('İ', 'i')
                .Replace('ş', 's').Replace('Ş', 's')
                .Replace('ğ', 'g').Replace('Ğ', 'g')
                .Replace('ü', 'u').Replace('Ü', 'u')
                .Replace('ö', 'o').Replace('Ö', 'o')
                .Replace('ç', 'c').Replace('Ç', 'c')
                .ToLowerInvariant();

            var ayrilmis = katlanmis.Normalize(NormalizationForm.FormD);
            var sonuc = new StringBuilder(ayrilmis.Length);

            foreach (var harf in ayrilmis)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(harf) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(harf))
                {
                    sonuc.Append(harf);
                }
            }

            return sonuc.ToString().Normalize(NormalizationForm.FormC);
        }

        private sealed class Liste
        {
            public string Surum { get; set; }
            public List<string> Kokler { get; set; }
        }
    }
}
