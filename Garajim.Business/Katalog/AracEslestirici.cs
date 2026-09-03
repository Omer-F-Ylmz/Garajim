using System.Globalization;
using System.Text;

namespace Garajim.Business.Katalog
{
    public class AracEslesmesi
    {
        public string Marka { get; set; }
        public string Seri { get; set; }
        public string Motor { get; set; }
    }

    public static class AracEslestirici
    {
        private static readonly Dictionary<string, string> MarkaTakmaAdlari = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["VW"] = "Volkswagen",
            ["V.W."] = "Volkswagen",
            ["Wolkswagen"] = "Volkswagen",
            ["Mercedes"] = "Mercedes - Benz",
            ["Mercedes-Benz"] = "Mercedes - Benz",
            ["Mercedes Benz"] = "Mercedes - Benz",
            ["MercedesBenz"] = "Mercedes - Benz",
            ["Benz"] = "Mercedes - Benz",
            ["Tofas"] = "Tofaş",
            ["Citroën"] = "Citroen",
            ["Skoda"] = "Skoda",
            ["Şkoda"] = "Skoda",
            ["Alfa"] = "Alfa Romeo",
            ["Land Rover"] = "Rover",
            ["DS"] = "DS Automobiles",
            ["Mini Cooper"] = "Mini"
        };

        public static AracEslesmesi Esle(AracKatalogu katalog, string marka, string model)
        {
            if (katalog == null || string.IsNullOrWhiteSpace(model))
            {
                return null;
            }

            var markaAdayi = MarkaAdayi(katalog, marka);
            var modelMetni = model.Trim();

            var dogrudan = Dene(katalog, markaAdayi, modelMetni, null);
            if (dogrudan != null)
            {
                return dogrudan;
            }

            var parcalar = modelMetni.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var uzunluk = parcalar.Length - 1; uzunluk >= 1; uzunluk--)
            {
                var bas = string.Join(' ', parcalar.Take(uzunluk));
                var kalan = string.Join(' ', parcalar.Skip(uzunluk));

                var sonuc = Dene(katalog, markaAdayi, bas, kalan);
                if (sonuc != null)
                {
                    return sonuc;
                }
            }

            for (var atlanan = 1; atlanan < parcalar.Length; atlanan++)
            {
                var kalanMetin = string.Join(' ', parcalar.Skip(atlanan));
                var onEk = string.Join(' ', parcalar.Take(atlanan));

                if (!Ayni(onEk, marka) && katalog.MarkaYazimi(onEk) == null && MarkaAdayi(katalog, onEk) == null)
                {
                    continue;
                }

                var sonuc = Dene(katalog, MarkaAdayi(katalog, onEk) ?? markaAdayi, kalanMetin, null);
                if (sonuc != null)
                {
                    return sonuc;
                }
            }

            return null;
        }

        public static string Sadelestir(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return string.Empty;
            }

            var yazi = metin.Trim()
                .Replace('ç', 'c').Replace('Ç', 'C')
                .Replace('ğ', 'g').Replace('Ğ', 'G')
                .Replace('ı', 'i').Replace('İ', 'I')
                .Replace('ö', 'o').Replace('Ö', 'O')
                .Replace('ş', 's').Replace('Ş', 'S')
                .Replace('ü', 'u').Replace('Ü', 'U');

            var kaynak = yazi.Normalize(NormalizationForm.FormD);
            var temiz = new StringBuilder(kaynak.Length);

            foreach (var karakter in kaynak)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(karakter) != UnicodeCategory.NonSpacingMark)
                {
                    temiz.Append(karakter);
                }
            }

            return temiz.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }

        private static bool Ayni(string sol, string sag)
        {
            return Sadelestir(sol).Equals(Sadelestir(sag), StringComparison.Ordinal);
        }

        private static string MarkaAdayi(AracKatalogu katalog, string marka)
        {
            if (string.IsNullOrWhiteSpace(marka))
            {
                return null;
            }

            var dogrudan = katalog.MarkaYazimi(marka);
            if (dogrudan != null)
            {
                return dogrudan;
            }

            if (MarkaTakmaAdlari.TryGetValue(marka.Trim(), out var takma))
            {
                return katalog.MarkaYazimi(takma);
            }

            var sade = Sadelestir(marka);

            foreach (var ad in katalog.MarkaAdlari)
            {
                if (Sadelestir(ad).Equals(sade, StringComparison.Ordinal))
                {
                    return ad;
                }
            }

            foreach (var giris in MarkaTakmaAdlari)
            {
                if (Sadelestir(giris.Key).Equals(sade, StringComparison.Ordinal))
                {
                    return katalog.MarkaYazimi(giris.Value);
                }
            }

            return null;
        }

        private static AracEslesmesi Dene(AracKatalogu katalog, string marka, string seri, string motor)
        {
            if (string.IsNullOrWhiteSpace(seri))
            {
                return null;
            }

            var bulunanMarka = marka;
            var bulunanSeri = marka == null ? null : katalog.SeriYazimi(marka, seri);

            if (bulunanSeri == null)
            {
                bulunanSeri = SeriYazimiSade(katalog, marka, seri);
            }

            if (bulunanSeri == null && marka != null)
            {
                return null;
            }

            if (bulunanSeri == null)
            {
                bulunanMarka = katalog.SerininMarkasi(seri);
                bulunanSeri = bulunanMarka == null ? null : katalog.SeriYazimi(bulunanMarka, seri);

                if (bulunanSeri == null)
                {
                    foreach (var katalogMarkasi in katalog.Markalar)
                    {
                        var aday = SeriYazimiSade(katalog, katalogMarkasi.Ad, seri);
                        if (aday != null)
                        {
                            bulunanMarka = katalogMarkasi.Ad;
                            bulunanSeri = aday;
                            break;
                        }
                    }
                }
            }

            if (bulunanSeri == null || bulunanMarka == null)
            {
                return null;
            }

            return new AracEslesmesi
            {
                Marka = bulunanMarka,
                Seri = bulunanSeri,
                Motor = string.IsNullOrWhiteSpace(motor) ? null : motor.Trim()
            };
        }

        private static string SeriYazimiSade(AracKatalogu katalog, string marka, string seri)
        {
            if (marka == null)
            {
                return null;
            }

            var sade = Sadelestir(seri);

            return katalog.Seriler(marka).FirstOrDefault(s => Sadelestir(s).Equals(sade, StringComparison.Ordinal));
        }
    }
}
