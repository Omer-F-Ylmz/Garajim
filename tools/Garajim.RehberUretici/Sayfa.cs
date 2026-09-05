using System.Text;

namespace Garajim.RehberUretici
{
    public static class Sayfa
    {
        public static string Kacis(string metin)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return string.Empty;
            }

            return metin
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        public static string JsonKacis(string metin)
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
                    case '"': yazi.Append("\\\""); break;
                    case '\\': yazi.Append("\\\\"); break;
                    case '\n': yazi.Append("\\n"); break;
                    case '\r': yazi.Append("\\r"); break;
                    case '\t': yazi.Append("\\t"); break;
                    case '<': yazi.Append("\\u003C"); break;
                    case '>': yazi.Append("\\u003E"); break;
                    case '&': yazi.Append("\\u0026"); break;
                    default:
                        if (harf < ' ')
                        {
                            yazi.Append("\\u").Append(((int)harf).ToString("x4"));
                        }
                        else
                        {
                            yazi.Append(harf);
                        }
                        break;
                }
            }

            return yazi.ToString();
        }

        public static void Bas(StringBuilder sb, string baslik, string aciklama, string kanonikYol, string tabanAdres)
        {
            var tamAdres = tabanAdres + kanonikYol;

            sb.Append("<!DOCTYPE html>\n<html lang=\"tr\">\n<head>\n");
            sb.Append("    <meta charset=\"utf-8\">\n");
            sb.Append("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            sb.Append("    <title>").Append(Kacis(baslik)).Append("</title>\n");
            sb.Append("    <meta name=\"description\" content=\"").Append(Kacis(aciklama)).Append("\">\n");
            sb.Append("    <link rel=\"canonical\" href=\"").Append(Kacis(tamAdres)).Append("\">\n");
            sb.Append("    <meta property=\"og:type\" content=\"article\">\n");
            sb.Append("    <meta property=\"og:title\" content=\"").Append(Kacis(baslik)).Append("\">\n");
            sb.Append("    <meta property=\"og:description\" content=\"").Append(Kacis(aciklama)).Append("\">\n");
            sb.Append("    <meta property=\"og:image\" content=\"").Append(Kacis(tabanAdres)).Append("/img/og.png\">\n");
            sb.Append("    <meta property=\"og:url\" content=\"").Append(Kacis(tamAdres)).Append("\">\n");
            sb.Append("    <link rel=\"icon\" type=\"image/svg+xml\" href=\"/garajim-logo.svg\">\n");
            sb.Append("    <link rel=\"apple-touch-icon\" sizes=\"180x180\" href=\"/garajim-icon-180.png\">\n");
            sb.Append("    <link rel=\"stylesheet\" href=\"/rehber.css\">\n");
            sb.Append("</head>\n<body>\n");
        }

        public static void UstCubuk(StringBuilder sb)
        {
            sb.Append("<header class=\"rehber-ust\">\n");
            sb.Append("    <a class=\"rehber-marka\" href=\"/\"><span class=\"brand-mark\"></span><span>Garajım</span></a>\n");
            sb.Append("    <nav class=\"rehber-menu\">\n");
            sb.Append("        <a href=\"/rehber/\">Rehber</a>\n");
            sb.Append("        <a href=\"/yardim.html\">Yardım</a>\n");
            sb.Append("        <a href=\"/\">Giriş</a>\n");
            sb.Append("        <a class=\"rehber-cta-kucuk\" href=\"/?utm_source=rehber&amp;utm_medium=ust&amp;utm_content=menu\">Ücretsiz başla</a>\n");
            sb.Append("    </nav>\n");
            sb.Append("</header>\n");
        }

        public static void Breadcrumb(StringBuilder sb, string tabanAdres, params (string Ad, string Yol)[] adimlar)
        {
            sb.Append("<nav class=\"rehber-iz\" aria-label=\"Breadcrumb\">\n");

            for (var i = 0; i < adimlar.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append("    <span class=\"rehber-iz-ayrac\">›</span>\n");
                }

                if (adimlar[i].Yol == null)
                {
                    sb.Append("    <span>").Append(Kacis(adimlar[i].Ad)).Append("</span>\n");
                }
                else
                {
                    sb.Append("    <a href=\"").Append(Kacis(adimlar[i].Yol)).Append("\">")
                      .Append(Kacis(adimlar[i].Ad)).Append("</a>\n");
                }
            }

            sb.Append("</nav>\n");

            sb.Append("<script type=\"application/ld+json\">");
            sb.Append("{\"@context\":\"https://schema.org\",\"@type\":\"BreadcrumbList\",\"itemListElement\":[");

            for (var i = 0; i < adimlar.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append("{\"@type\":\"ListItem\",\"position\":").Append(i + 1)
                  .Append(",\"name\":\"").Append(JsonKacis(adimlar[i].Ad)).Append('"');

                if (adimlar[i].Yol != null)
                {
                    sb.Append(",\"item\":\"").Append(JsonKacis(tabanAdres + adimlar[i].Yol)).Append('"');
                }

                sb.Append('}');
            }

            sb.Append("]}</script>\n");
        }

        public static void Cta(StringBuilder sb, string slug)
        {
            sb.Append("<aside class=\"rehber-cta\">\n");
            sb.Append("    <h2>").Append(Sabitler.CtaBaslik).Append("</h2>\n");
            sb.Append("    <p>").Append(Sabitler.CtaMetin).Append("</p>\n");
            sb.Append("    <a class=\"rehber-dugme\" href=\"").Append(Sabitler.Cta(slug)).Append("\">")
              .Append(Sabitler.CtaDugme).Append("</a>\n");
            sb.Append("</aside>\n");
        }

        public static void Altbilgi(StringBuilder sb)
        {
            sb.Append("<footer class=\"rehber-alt\">\n");
            sb.Append("    <p class=\"rehber-uyari\">").Append(Kacis(Sabitler.Uyari)).Append("</p>\n");
            sb.Append("    <nav>\n");
            sb.Append("        <a href=\"/rehber/\">Rehber</a>\n");
            sb.Append("        <a href=\"/yardim.html\">Yardım</a>\n");
            sb.Append("        <a href=\"/yenilikler.html\">Yenilikler</a>\n");
            sb.Append("        <a href=\"/sartlar.html\">Kullanım Şartları</a>\n");
            sb.Append("    </nav>\n");
            sb.Append("</footer>\n");
        }

        public static void Kapat(StringBuilder sb, string script)
        {
            if (script != null)
            {
                sb.Append("<script defer src=\"").Append(script).Append("\"></script>\n");
            }

            sb.Append("</body>\n</html>\n");
        }
    }
}
