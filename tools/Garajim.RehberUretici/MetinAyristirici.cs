using System.Text.RegularExpressions;

namespace Garajim.RehberUretici
{
    public class MetinBolumu
    {
        public string Baslik { get; set; }
        public string Metin { get; set; }
        public bool Uyari { get; set; }
    }

    public static class MetinAyristirici
    {
        private static readonly string[] Etiketler = { "En sık", "Belirti", "Kırmızı", "Ustaya", "Nadir", "Sık" };

        private static readonly Regex AciliyetDeseni =
            new Regex("aciliyet\\s*:\\s*([A-Za-zÇĞİÖŞÜçğıöşü]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<MetinBolumu> Bolumler(string metin)
        {
            var bolumler = new List<MetinBolumu>();

            if (string.IsNullOrWhiteSpace(metin))
            {
                return bolumler;
            }

            foreach (var parca in metin.Split('|'))
            {
                var govde = parca.Trim();

                if (govde.Length == 0)
                {
                    continue;
                }

                var etiket = Etiketler.FirstOrDefault(e =>
                    govde.StartsWith(e + ":", StringComparison.OrdinalIgnoreCase));

                if (etiket == null)
                {
                    bolumler.Add(new MetinBolumu { Baslik = null, Metin = govde });
                    continue;
                }

                bolumler.Add(new MetinBolumu
                {
                    Baslik = etiket,
                    Metin = govde.Substring(etiket.Length + 1).Trim(),
                    Uyari = etiket == "Kırmızı"
                });
            }

            return bolumler;
        }

        public static string Aciliyet(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return null;
            }

            var eslesme = AciliyetDeseni.Match(metin);

            return eslesme.Success ? eslesme.Groups[1].Value : null;
        }

        public static string IlkCumle(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return string.Empty;
            }

            var govde = metin.Trim();
            var derinlik = 0;

            for (var i = 0; i < govde.Length; i++)
            {
                var harf = govde[i];

                if (harf == '(' || harf == '[')
                {
                    derinlik++;
                }
                else if (harf == ')' || harf == ']')
                {
                    derinlik = Math.Max(0, derinlik - 1);
                }
                else if (harf == '|' && derinlik == 0)
                {
                    return govde.Substring(0, i).Trim().TrimEnd('.', ';', ',').Trim();
                }
                else if (harf == '.' && derinlik == 0)
                {
                    if (i + 1 >= govde.Length || char.IsWhiteSpace(govde[i + 1]))
                    {
                        return govde.Substring(0, i).Trim();
                    }
                }
            }

            return govde.TrimEnd('.', ';', ',').Trim();
        }
    }
}
