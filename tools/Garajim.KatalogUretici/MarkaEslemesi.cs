using System.Text;

namespace Garajim.KatalogUretici
{
    public static class MarkaEslemesi
    {
        private static readonly Dictionary<string, string> TakmaAdlar = new(StringComparer.OrdinalIgnoreCase)
        {
            ["mercedesbenz"] = "Mercedes - Benz",
            ["mercedes"] = "Mercedes - Benz",
            ["citroen"] = "Citroen",
            ["skoda"] = "Skoda",
            ["vw"] = "Volkswagen",
            ["volkswagen"] = "Volkswagen",
            ["tofas"] = "Tofaş",
            ["rollsroyce"] = "Rolls-Royce",
            ["ds"] = "DS Automobiles",
            ["dsautomobiles"] = "DS Automobiles",
            ["irankhodro"] = "Ikco",
            ["ikco"] = "Ikco",
            ["alfaromeo"] = "Alfa Romeo",
            ["landrover"] = "Land Rover",
            ["astonmartin"] = "Aston Martin",
            ["gm"] = "Chevrolet",
            ["chevy"] = "Chevrolet",
            ["hyundaimotorcompany"] = "Hyundai",
            ["kiamotors"] = "Kia",
            ["kia"] = "Kia",
        };

        public static string Kanonik(string vpicMarka, TrKatalog tr)
        {
            var temiz = MetinTemizleyici.Duzelt(vpicMarka);

            if (temiz == null)
            {
                return null;
            }

            var anahtar = Sadelestir(temiz);

            if (TakmaAdlar.TryGetValue(anahtar, out var takma) && (tr == null || tr.MarkaVar(takma)))
            {
                return takma;
            }

            if (tr != null)
            {
                foreach (var trMarka in tr.Markalar.Keys)
                {
                    if (Sadelestir(trMarka) == anahtar)
                    {
                        return trMarka;
                    }
                }
            }

            if (TakmaAdlar.TryGetValue(anahtar, out var takmaGlobal))
            {
                return takmaGlobal;
            }

            return temiz;
        }

        public static string Sadelestir(string metin)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return string.Empty;
            }

            var yazi = new StringBuilder(metin.Length);

            foreach (var karakter in metin)
            {
                if (!char.IsLetterOrDigit(karakter))
                {
                    continue;
                }

                yazi.Append(KatlanmisHarf(char.ToLowerInvariant(karakter)));
            }

            return yazi.ToString();
        }

        private static char KatlanmisHarf(char karakter)
        {
            switch (karakter)
            {
                case 'ı': return 'i';
                case 'ş': return 's';
                case 'ğ': return 'g';
                case 'ü': return 'u';
                case 'ö': return 'o';
                case 'ç': return 'c';
                case 'â': return 'a';
                case 'î': return 'i';
                case 'û': return 'u';
                case 'ë': return 'e';
                case 'é': return 'e';
                case 'è': return 'e';
                case 'á': return 'a';
                case 'à': return 'a';
                case 'ó': return 'o';
                case 'ú': return 'u';
                case 'ñ': return 'n';
                case 'š': return 's';
                case 'ž': return 'z';
                case 'č': return 'c';
                case 'ř': return 'r';
                case 'å': return 'a';
                case 'ø': return 'o';
                case 'æ': return 'a';
                default: return karakter;
            }
        }
    }
}
