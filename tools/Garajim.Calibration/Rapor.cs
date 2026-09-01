using System.Text;

namespace Garajim.Calibration
{
    public class DosyaSonucu
    {
        public string Dosya { get; set; }
        public string Zorluk { get; set; } = "belirsiz";
        public string Tur { get; set; }
        public Dictionary<string, bool> AlanDogru { get; } = new Dictionary<string, bool>();
        public List<string> Farklar { get; } = new List<string>();
        public double GuvenSkoru { get; set; }
        public int SureMs { get; set; }
        public string Hata { get; set; }
    }

    public class RaporSonucu
    {
        public int DosyaSayisi { get; set; }
        public Dictionary<string, double> AlanDogruluk { get; } = new Dictionary<string, double>();
        public Dictionary<string, double> ZorlukDogruluk { get; } = new Dictionary<string, double>();
        public Dictionary<string, double> TurDogruluk { get; } = new Dictionary<string, double>();
        public double OrtalamaGuven { get; set; }
        public double OrtalamaSureMs { get; set; }
        public List<DosyaSonucu> Yanlislar { get; } = new List<DosyaSonucu>();
    }

    public static class Rapor
    {
        public static RaporSonucu Olustur(List<DosyaSonucu> sonuclar)
        {
            var rapor = new RaporSonucu { DosyaSayisi = sonuclar.Count };
            var basarililar = sonuclar.Where(s => s.Hata == null).ToList();

            if (basarililar.Count == 0)
            {
                return rapor;
            }

            foreach (var alan in basarililar.SelectMany(s => s.AlanDogru.Keys).Distinct())
            {
                var ilgili = basarililar.Where(s => s.AlanDogru.ContainsKey(alan)).ToList();
                rapor.AlanDogruluk[alan] = Yuzde(ilgili.Count(s => s.AlanDogru[alan]), ilgili.Count);
            }

            foreach (var zorluk in basarililar.Select(s => s.Zorluk).Distinct())
            {
                var ilgili = basarililar.Where(s => s.Zorluk == zorluk).ToList();
                rapor.ZorlukDogruluk[zorluk] = Yuzde(
                    ilgili.Sum(s => s.AlanDogru.Values.Count(v => v)),
                    ilgili.Sum(s => s.AlanDogru.Count));
            }

            foreach (var tur in basarililar.Where(s => s.Tur != null).Select(s => s.Tur).Distinct())
            {
                var ilgili = basarililar.Where(s => s.Tur == tur).ToList();
                rapor.TurDogruluk[tur] = Yuzde(
                    ilgili.Sum(s => s.AlanDogru.Values.Count(v => v)),
                    ilgili.Sum(s => s.AlanDogru.Count));
            }

            rapor.OrtalamaGuven = Math.Round(basarililar.Average(s => s.GuvenSkoru), 3);
            rapor.OrtalamaSureMs = Math.Round(basarililar.Average(s => (double)s.SureMs), 1);
            rapor.Yanlislar.AddRange(sonuclar.Where(s => s.Hata != null || s.Farklar.Count > 0));

            return rapor;
        }

        private static double Yuzde(int pay, int payda)
        {
            return payda == 0 ? 0 : Math.Round(pay * 100.0 / payda, 1);
        }

        public static string Markdown(RaporSonucu rapor, DateTime zaman)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Garajım fiş çıkarımı kalibrasyonu");
            sb.AppendLine();
            sb.AppendLine($"Tarih: {zaman:dd.MM.yyyy HH:mm}  ");
            sb.AppendLine($"Dosya sayısı: {rapor.DosyaSayisi}  ");
            sb.AppendLine($"Ortalama güven: {rapor.OrtalamaGuven}  ");
            sb.AppendLine($"Ortalama süre: {rapor.OrtalamaSureMs} ms");
            sb.AppendLine();

            sb.AppendLine("## Alan bazında doğruluk");
            sb.AppendLine();
            sb.AppendLine("| Alan | Doğruluk |");
            sb.AppendLine("|---|---|");
            foreach (var alan in rapor.AlanDogruluk.OrderBy(a => a.Key))
            {
                sb.AppendLine($"| {alan.Key} | %{alan.Value} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Zorluğa göre");
            sb.AppendLine();
            sb.AppendLine("| Zorluk | Doğruluk |");
            sb.AppendLine("|---|---|");
            foreach (var zorluk in rapor.ZorlukDogruluk.OrderBy(z => z.Key))
            {
                sb.AppendLine($"| {zorluk.Key} | %{zorluk.Value} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Türe göre");
            sb.AppendLine();
            sb.AppendLine("| Tür | Doğruluk |");
            sb.AppendLine("|---|---|");
            foreach (var tur in rapor.TurDogruluk.OrderBy(t => t.Key))
            {
                sb.AppendLine($"| {tur.Key} | %{tur.Value} |");
            }
            sb.AppendLine();

            if (rapor.Yanlislar.Count > 0)
            {
                sb.AppendLine("## Dosya bazında yanlışlar");
                sb.AppendLine();
                foreach (var yanlis in rapor.Yanlislar)
                {
                    sb.AppendLine($"### {yanlis.Dosya} ({yanlis.Zorluk})");
                    if (yanlis.Hata != null)
                    {
                        sb.AppendLine($"- HATA: {yanlis.Hata}");
                    }
                    foreach (var fark in yanlis.Farklar)
                    {
                        sb.AppendLine($"- {fark}");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
