using System.Text;

namespace Garajim.Business.Usta
{
    public class UstaBilgiDeposu
    {
        public UstaBilgiDeposu(IReadOnlyList<BilgiKaydi> kayitlar, string sistemPromptu)
        {
            Kayitlar = kayitlar;
            Secici = new BilgiSecici(kayitlar);
            SistemPromptu = sistemPromptu;
        }

        public IReadOnlyList<BilgiKaydi> Kayitlar { get; }

        public BilgiSecici Secici { get; }

        public string SistemPromptu { get; }

        public string SabitBlok(IReadOnlyList<BilgiKaydi> secilen, string garajimVerisi = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(SistemPromptu);
            sb.AppendLine();
            sb.AppendLine("BILGI TABANI");

            if (secilen == null || secilen.Count == 0)
            {
                sb.AppendLine("Bu soruya doğrudan uyan kayıt bulunamadı; bilmediğini açıkça söyle.");
            }
            else
            {
                foreach (var kayit in secilen)
                {
                    sb.AppendLine($"[{kayit.Id} · {kayit.Kategori} · kaynak: {kayit.Kaynak}]");
                    sb.AppendLine(kayit.Metin);
                    sb.AppendLine();
                }
            }

            if (!string.IsNullOrWhiteSpace(garajimVerisi))
            {
                sb.AppendLine(garajimVerisi);
            }

            return sb.ToString();
        }
    }
}
