using System.Text;

namespace Garajim.Business.Concrete.Import
{
    public class CsvTablo
    {
        public char Ayrac { get; set; }
        public string Bolum { get; set; }
        public List<string> Basliklar { get; set; } = new List<string>();
        public List<string[]> Satirlar { get; set; } = new List<string[]>();
        public List<int> SatirNolari { get; set; } = new List<int>();
        public List<string> HamSatirlar { get; set; } = new List<string>();
    }

    public static class CsvOkuyucu
    {
        private const string BolumIsareti = "## ";

        private static readonly char[] Adaylar = { ';', ',', '\t' };

        private static readonly Dictionary<string, string[]> BolumAnahtarlari = new Dictionary<string, string[]>
        {
            ["Yakit"] = new[] { "log", "fuel", "yakit", "refuel" },
            ["Bakim"] = new[] { "service", "servis", "bakim", "maintenance" },
            ["Masraf"] = new[] { "cost", "expense", "masraf", "gider" }
        };

        public static CsvTablo Oku(byte[] icerik, string kayitTuru = null)
        {
            var metin = MetneCevir(icerik);
            var tumSatirlar = metin.Replace("\r\n", "\n").Split('\n');

            var dolular = new List<(int No, string Metin)>();
            for (var i = 0; i < tumSatirlar.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(tumSatirlar[i]))
                {
                    dolular.Add((i + 1, tumSatirlar[i]));
                }
            }

            var tablo = new CsvTablo { HamSatirlar = dolular.Select(s => s.Metin).ToList() };
            if (dolular.Count == 0)
            {
                tablo.Ayrac = ';';
                return tablo;
            }

            var secilen = BolumSec(dolular, kayitTuru, out var bolumAdi);
            tablo.Bolum = bolumAdi;

            if (secilen.Count == 0)
            {
                tablo.Ayrac = ';';
                return tablo;
            }

            tablo.Ayrac = AyracSez(secilen.Select(s => s.Metin).ToList());
            tablo.Basliklar = Bol(secilen[0].Metin, tablo.Ayrac).ToList();

            for (var i = 1; i < secilen.Count; i++)
            {
                tablo.Satirlar.Add(Bol(secilen[i].Metin, tablo.Ayrac));
                tablo.SatirNolari.Add(secilen[i].No);
            }

            return tablo;
        }

        private static List<(int No, string Metin)> BolumSec(List<(int No, string Metin)> satirlar, string kayitTuru, out string bolumAdi)
        {
            bolumAdi = null;

            if (!satirlar.Any(s => s.Metin.TrimStart().StartsWith(BolumIsareti, StringComparison.Ordinal)))
            {
                return satirlar;
            }

            var bolumler = new List<(string Ad, List<(int No, string Metin)> Satirlar)>();
            foreach (var satir in satirlar)
            {
                var kirpik = satir.Metin.TrimStart();
                if (kirpik.StartsWith(BolumIsareti, StringComparison.Ordinal))
                {
                    bolumler.Add((kirpik.Substring(BolumIsareti.Length).Trim(), new List<(int, string)>()));
                    continue;
                }

                if (bolumler.Count > 0)
                {
                    bolumler[bolumler.Count - 1].Satirlar.Add(satir);
                }
            }

            var uygunlar = bolumler.Where(b => b.Satirlar.Count >= 2).ToList();
            if (uygunlar.Count == 0)
            {
                return new List<(int, string)>();
            }

            if (kayitTuru != null && BolumAnahtarlari.TryGetValue(kayitTuru, out var anahtarlar))
            {
                var eslesen = uygunlar.FirstOrDefault(b => anahtarlar.Any(a => ImportSablonlari.Sadelestir(b.Ad).Contains(a, StringComparison.Ordinal)));
                if (eslesen.Satirlar != null)
                {
                    bolumAdi = eslesen.Ad;
                    return eslesen.Satirlar;
                }
            }

            var enBuyuk = uygunlar.OrderByDescending(b => b.Satirlar.Count).First();
            bolumAdi = enBuyuk.Ad;
            return enBuyuk.Satirlar;
        }

        private static string MetneCevir(byte[] icerik)
        {
            if (icerik == null || icerik.Length == 0)
            {
                return string.Empty;
            }

            if (icerik.Length >= 3 && icerik[0] == 0xEF && icerik[1] == 0xBB && icerik[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(icerik, 3, icerik.Length - 3);
            }

            var kesin = new UTF8Encoding(false, true);
            try
            {
                return kesin.GetString(icerik);
            }
            catch (DecoderFallbackException)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1254).GetString(icerik);
            }
        }

        private static char AyracSez(List<string> satirlar)
        {
            var ornek = satirlar.Take(5).ToList();
            var enIyi = ';';
            var enYuksek = -1;

            foreach (var aday in Adaylar)
            {
                var sayilar = ornek.Select(s => Bol(s, aday).Length).ToList();
                if (sayilar.Count == 0 || sayilar[0] < 2)
                {
                    continue;
                }

                var tutarli = sayilar.All(s => s == sayilar[0]);
                var puan = sayilar[0] * (tutarli ? 10 : 1);

                if (puan > enYuksek)
                {
                    enYuksek = puan;
                    enIyi = aday;
                }
            }

            return enIyi;
        }

        public static string[] Bol(string satir, char ayrac)
        {
            var alanlar = new List<string>();
            var sb = new StringBuilder();
            var tirnakta = false;

            for (var i = 0; i < satir.Length; i++)
            {
                var karakter = satir[i];

                if (karakter == '"')
                {
                    if (tirnakta && i + 1 < satir.Length && satir[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        tirnakta = !tirnakta;
                    }
                    continue;
                }

                if (karakter == ayrac && !tirnakta)
                {
                    alanlar.Add(sb.ToString().Trim());
                    sb.Clear();
                    continue;
                }

                sb.Append(karakter);
            }

            alanlar.Add(sb.ToString().Trim());
            return alanlar.ToArray();
        }
    }
}
