using System.Globalization;
using System.Text;
using Garajim.Entity.Dtos;

namespace Garajim.API.Controllers
{
    public static class TutanakSayfasi
    {
        public static string Olustur(HasarDto dosya, IReadOnlyDictionary<int, string> gomuluFotolar = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"tr\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.AppendLine("<title>Hasar dosyası özeti</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:system-ui,Segoe UI,Arial,sans-serif;max-width:820px;margin:0 auto;padding:24px 16px;color:#111;}");
            sb.AppendLine("h1{font-size:20px;margin:0 0 4px;} h2{font-size:15px;margin:20px 0 6px;border-bottom:1px solid #ddd;padding-bottom:4px;}");
            sb.AppendLine("dl{display:grid;grid-template-columns:190px 1fr;gap:4px 12px;margin:0;font-size:14px;}");
            sb.AppendLine("dt{color:#555;} dd{margin:0;}");
            sb.AppendLine(".fotolar{display:flex;flex-wrap:wrap;gap:8px;margin-top:8px;}");
            sb.AppendLine(".foto{border:1px solid #ddd;border-radius:6px;padding:6px;width:150px;font-size:11px;text-align:center;}");
            sb.AppendLine(".foto img{width:100%;height:100px;object-fit:cover;border-radius:4px;display:block;margin-bottom:4px;}");
            sb.AppendLine(".kutu{border:1px solid #999;border-radius:6px;padding:10px;margin-top:8px;}");
            sb.AppendLine(".satir{border-bottom:1px solid #bbb;height:26px;margin-top:12px;}");
            sb.AppendLine(".not{font-size:11px;color:#666;margin-top:24px;}");
            sb.AppendLine("@media print{ .yazdir{display:none;} }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<button class=\"yazdir\" type=\"button\" onclick=\"window.print()\">Yazdır</button>");
            sb.AppendLine($"<h1>Hasar dosyası özeti — {Kacir(dosya.Plaka)}</h1>");
            sb.AppendLine($"<p>Dosya no: {dosya.Id} · Oluşturma: {Tarih(dosya.OlusturmaTarihi)}</p>");

            sb.AppendLine("<h2>Olay</h2><dl>");
            Satir(sb, "Olay tarihi", Tarih(dosya.OlayTarihi));
            Satir(sb, "Tür", dosya.TurAdi);
            Satir(sb, "Durum", dosya.DurumAdi);
            Satir(sb, "Konum", dosya.Konum);
            Satir(sb, "Kilometre", dosya.OlayKm?.ToString("N0", new CultureInfo("tr-TR")));
            Satir(sb, "Tutanak", dosya.TutanakTuruAdi);
            Satir(sb, "Açıklama", dosya.Aciklama);
            sb.AppendLine("</dl>");

            sb.AppendLine("<h2>Karşı taraf ve sigorta</h2><dl>");
            Satir(sb, "Karşı araç plakası", dosya.KarsiTarafPlaka);
            Satir(sb, "Karşı taraf sigortası", dosya.KarsiTarafSigorta);
            Satir(sb, "Karşı taraf poliçe no", dosya.KarsiTarafPoliceNo);
            Satir(sb, "Kendi sigorta dosya no", dosya.SigortaDosyaNo);
            sb.AppendLine("</dl>");

            sb.AppendLine("<h2>Fotoğraflar</h2>");
            if (dosya.Fotograflar.Count == 0)
            {
                sb.AppendLine("<p>Bu dosyaya fotoğraf eklenmemiş.</p>");
            }
            else
            {
                sb.AppendLine("<div class=\"fotolar\">");
                foreach (var foto in dosya.Fotograflar)
                {
                    sb.AppendLine("<div class=\"foto\">");

                    if (gomuluFotolar != null && gomuluFotolar.TryGetValue(foto.DocumentId, out var veriUrl))
                    {
                        sb.AppendLine("<img src=\"" + veriUrl + "\" alt=\"" + Kacir(foto.EtiketAdi) + "\">");
                    }
                    else
                    {
                        sb.AppendLine("<p class=\"foto-yok\">Görsel yüklenemedi</p>");
                    }

                    sb.AppendLine(Kacir(foto.EtiketAdi));
                    sb.AppendLine("</div>");
                }
                sb.AppendLine("</div>");
            }

            sb.AppendLine("<h2>Bilgi değişim alanı</h2>");
            sb.AppendLine("<div class=\"kutu\">");
            sb.AppendLine("<p>Olay yerinde elle doldurulacak alanlar:</p>");
            foreach (var alan in new[] { "Karşı sürücü adı soyadı", "Telefon", "Sürücü belgesi no", "Tanık adı ve telefonu", "Polis tutanak no" })
            {
                sb.AppendLine($"<div>{Kacir(alan)}<div class=\"satir\"></div></div>");
            }
            sb.AppendLine("</div>");

            sb.AppendLine("<p class=\"not\">Bu belge Garajım tarafından kayıt özeti olarak üretilmiştir; resmî tutanak yerine geçmez. " +
                          "Karşı tarafın ad, telefon ve kimlik bilgileri uygulamada saklanmaz, yalnız bu çıktıda elle doldurulur.</p>");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static void Satir(StringBuilder sb, string etiket, string deger)
        {
            sb.AppendLine($"<dt>{Kacir(etiket)}</dt><dd>{(string.IsNullOrWhiteSpace(deger) ? "-" : Kacir(deger))}</dd>");
        }

        private static string Tarih(DateTime deger)
        {
            return deger.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        private static string Kacir(string deger)
        {
            if (string.IsNullOrEmpty(deger))
            {
                return string.Empty;
            }

            return deger
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}
