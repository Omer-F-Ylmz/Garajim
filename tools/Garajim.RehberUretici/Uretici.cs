using System.Text;
using System.Text.Json;

namespace Garajim.RehberUretici
{
    public static class Uretici
    {
        private static readonly JsonSerializerOptions Secenekler = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static UretimSonucu Uret(string bilgiKlasoru, string ciktiKlasoru, string tabanAdres)
        {
            return Uret(bilgiKlasoru, ciktiKlasoru, tabanAdres, null);
        }

        public static UretimSonucu Uret(string bilgiKlasoru, string ciktiKlasoru, string tabanAdres, string sitemapYolu)
        {
            var sonuc = new UretimSonucu();

            if (Directory.Exists(ciktiKlasoru))
            {
                Directory.Delete(ciktiKlasoru, true);
            }

            Directory.CreateDirectory(ciktiKlasoru);

            foreach (var bolum in Bolumler.Hepsi)
            {
                var dosya = Path.Combine(bilgiKlasoru, bolum.Dosya);

                if (!File.Exists(dosya))
                {
                    sonuc.Uyarilar.Add("Bilgi dosyası bulunamadı, atlandı: " + bolum.Dosya);
                    continue;
                }

                Oku(dosya, bolum, sonuc);
            }

            IlgiliSecici.Bagla(sonuc.Kayitlar);

            var kural = sonuc.Kayitlar.FirstOrDefault(k => k.Id == Sabitler.BakimKuralId);

            foreach (var kayit in sonuc.Kayitlar)
            {
                var yol = Path.Combine(ciktiKlasoru, kayit.Bolum, kayit.Slug + ".html");
                Directory.CreateDirectory(Path.GetDirectoryName(yol));
                File.WriteAllText(yol, KayitSayfasi(kayit, kural, tabanAdres), Utf8);
                sonuc.Dosyalar.Add(yol);
            }

            foreach (var bolum in Bolumler.Hepsi)
            {
                var kayitlar = sonuc.Kayitlar.Where(k => k.Bolum == bolum.Yol).ToList();

                if (kayitlar.Count == 0)
                {
                    continue;
                }

                var yol = Path.Combine(ciktiKlasoru, bolum.Yol, "index.html");
                Directory.CreateDirectory(Path.GetDirectoryName(yol));
                File.WriteAllText(yol, BolumHubu(bolum, kayitlar, tabanAdres), Utf8);
                sonuc.Dosyalar.Add(yol);
            }

            File.WriteAllText(Path.Combine(ciktiKlasoru, "index.html"), KokHub(sonuc.Kayitlar, tabanAdres), Utf8);
            File.WriteAllText(Path.Combine(ciktiKlasoru, "index.json"), AramaDizini(sonuc.Kayitlar), Utf8);
            var sitemap = string.IsNullOrWhiteSpace(sitemapYolu) ? Path.Combine(ciktiKlasoru, "sitemap.xml") : sitemapYolu;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sitemap)));
            File.WriteAllText(sitemap, Sitemap(sonuc.Kayitlar, tabanAdres), Utf8);

            return sonuc;
        }

        private static void Oku(string dosya, Bolum bolum, UretimSonucu sonuc)
        {
            List<RehberKaydi> okunan;

            try
            {
                okunan = JsonSerializer.Deserialize<List<RehberKaydi>>(File.ReadAllText(dosya), Secenekler);
            }
            catch (JsonException hata)
            {
                sonuc.Uyarilar.Add("Bilgi dosyası okunamadı, atlandı (" + bolum.Dosya + "): " + hata.Message);
                return;
            }

            if (okunan == null)
            {
                sonuc.Uyarilar.Add("Bilgi dosyası boş, atlandı: " + bolum.Dosya);
                return;
            }

            var kullanilan = new HashSet<string>(StringComparer.Ordinal);

            foreach (var kayit in okunan.OrderBy(k => k.Id, StringComparer.Ordinal))
            {
                kayit.Bolum = bolum.Yol;

                var eksik = Eksik(kayit);

                if (eksik != null)
                {
                    sonuc.Uyarilar.Add("Kayıt atlandı (" + bolum.Dosya + " / " + (kayit.Id ?? "id yok") + "): " + eksik);
                    continue;
                }

                kayit.Baslik = Basliklar.Uret(kayit);
                kayit.Aciklama = Basliklar.Aciklama(kayit);

                if (string.IsNullOrWhiteSpace(kayit.Baslik))
                {
                    sonuc.Uyarilar.Add("Kayıt atlandı (" + bolum.Dosya + " / " + kayit.Id + "): başlık türetilemedi.");
                    continue;
                }

                kayit.Slug = Slug.Tekil(kayit.Baslik, kayit.Id, kullanilan);
                sonuc.Kayitlar.Add(kayit);
            }
        }

        private static string Eksik(RehberKaydi kayit)
        {
            if (string.IsNullOrWhiteSpace(kayit.Id)) return "id boş.";
            if (string.IsNullOrWhiteSpace(kayit.Metin)) return "metin boş.";
            if (string.IsNullOrWhiteSpace(kayit.Kaynak)) return "kaynak boş.";
            if (string.IsNullOrWhiteSpace(kayit.Guncelleme)) return "guncelleme boş.";
            if (kayit.Anahtarlar == null || kayit.Anahtarlar.Count == 0) return "anahtarlar boş.";

            return null;
        }

        private static string KayitSayfasi(RehberKaydi kayit, RehberKaydi kural, string tabanAdres)
        {
            var bolum = Bolumler.Bul(kayit.Bolum);
            var sb = new StringBuilder(8192);

            Sayfa.Bas(sb, kayit.Baslik, kayit.Aciklama, kayit.Url, tabanAdres);
            Sayfa.UstCubuk(sb);

            sb.Append("<main class=\"rehber-govde\">\n");

            Sayfa.Breadcrumb(sb, tabanAdres,
                ("Rehber", Sabitler.Kok),
                (bolum.Etiket, "/rehber/" + bolum.Yol + "/"),
                (kayit.Baslik, null));

            sb.Append("<article>\n");
            sb.Append("<h1>").Append(Sayfa.Kacis(kayit.Baslik)).Append("</h1>\n");

            var aciliyet = MetinAyristirici.Aciliyet(kayit.Metin);

            if (aciliyet != null)
            {
                sb.Append("<p class=\"rehber-rozet\">Aciliyet: ").Append(Sayfa.Kacis(aciliyet)).Append("</p>\n");
            }

            if (kayit.Bolum == Bolumler.Bakim && kayit.Id != Sabitler.BakimKuralId && kural != null)
            {
                sb.Append("<aside class=\"rehber-kural\">\n");
                sb.Append("    <h2>").Append(Sabitler.BakimKuralBasligi).Append("</h2>\n");
                sb.Append("    <p>").Append(Sayfa.Kacis(MetinAyristirici.IlkCumle(kural.Metin.Substring(6)))).Append(".</p>\n");
                sb.Append("    <p><a href=\"").Append(kural.Url).Append("\">Genel bakım kuralını oku</a></p>\n");
                sb.Append("</aside>\n");
            }

            foreach (var parca in MetinAyristirici.Bolumler(kayit.Metin))
            {
                sb.Append(parca.Uyari ? "<section class=\"rehber-bolum rehber-kirmizi\">\n" : "<section class=\"rehber-bolum\">\n");

                if (parca.Baslik != null)
                {
                    sb.Append("    <h2>").Append(Sayfa.Kacis(parca.Baslik)).Append("</h2>\n");
                }

                sb.Append("    <p>").Append(Sayfa.Kacis(parca.Metin)).Append("</p>\n");
                sb.Append("</section>\n");
            }

            sb.Append("</article>\n");

            sb.Append("<section class=\"rehber-ilgili\">\n    <h2>İlgili</h2>\n    <ul>\n");

            foreach (var ilgili in kayit.Ilgili)
            {
                sb.Append("        <li><a href=\"").Append(ilgili.Url).Append("\">")
                  .Append(Sayfa.Kacis(ilgili.Baslik)).Append("</a></li>\n");
            }

            sb.Append("    </ul>\n</section>\n");

            Sayfa.Cta(sb, kayit.Slug);

            sb.Append("<p class=\"rehber-kaynak\">Kaynak: ").Append(Sayfa.Kacis(kayit.Kaynak))
              .Append(" · Güncelleme: ").Append(Sayfa.Kacis(kayit.Guncelleme)).Append("</p>\n");

            sb.Append("<script type=\"application/ld+json\">");
            sb.Append("{\"@context\":\"https://schema.org\",\"@type\":\"Article\",\"headline\":\"")
              .Append(Sayfa.JsonKacis(kayit.Baslik))
              .Append("\",\"description\":\"").Append(Sayfa.JsonKacis(kayit.Aciklama))
              .Append("\",\"datePublished\":\"").Append(Sayfa.JsonKacis(kayit.Guncelleme))
              .Append("\",\"dateModified\":\"").Append(Sayfa.JsonKacis(kayit.Guncelleme))
              .Append("\",\"inLanguage\":\"tr\",\"isAccessibleForFree\":true,")
              .Append("\"publisher\":{\"@type\":\"Organization\",\"name\":\"Garajım\"},")
              .Append("\"mainEntityOfPage\":\"").Append(Sayfa.JsonKacis(tabanAdres + kayit.Url)).Append("\"}");
            sb.Append("</script>\n");

            sb.Append("</main>\n");

            Sayfa.Altbilgi(sb);
            Sayfa.Kapat(sb, null);

            return sb.ToString();
        }

        private static string BolumHubu(Bolum bolum, List<RehberKaydi> kayitlar, string tabanAdres)
        {
            var yol = "/rehber/" + bolum.Yol + "/";
            var sb = new StringBuilder(16384);

            Sayfa.Bas(sb, Basliklar.Kirp(bolum.HubBaslik, Basliklar.BaslikSiniri),
                Basliklar.Kirp(bolum.HubAciklama, Basliklar.AciklamaSiniri), yol, tabanAdres);
            Sayfa.UstCubuk(sb);

            sb.Append("<main class=\"rehber-govde\">\n");

            Sayfa.Breadcrumb(sb, tabanAdres, ("Rehber", Sabitler.Kok), (bolum.Etiket, null));

            sb.Append("<h1>").Append(Sayfa.Kacis(bolum.HubBaslik)).Append("</h1>\n");
            sb.Append("<p class=\"rehber-giris\">").Append(Sayfa.Kacis(bolum.HubAciklama)).Append("</p>\n");

            foreach (var grup in kayitlar.GroupBy(Gruplayici(bolum.Yol)).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                sb.Append("<section class=\"rehber-grup\">\n");
                sb.Append("    <h2>").Append(Sayfa.Kacis(grup.Key)).Append("</h2>\n    <ul>\n");

                foreach (var kayit in grup.OrderBy(k => k.Baslik, StringComparer.Ordinal))
                {
                    sb.Append("        <li><a href=\"").Append(kayit.Url).Append("\">")
                      .Append(Sayfa.Kacis(kayit.Baslik)).Append("</a></li>\n");
                }

                sb.Append("    </ul>\n</section>\n");
            }

            Sayfa.Cta(sb, bolum.Yol);

            sb.Append("</main>\n");

            Sayfa.Altbilgi(sb);
            Sayfa.Kapat(sb, null);

            return sb.ToString();
        }

        private static Func<RehberKaydi, string> Gruplayici(string bolum)
        {
            if (bolum == Bolumler.Obd)
            {
                return k =>
                {
                    var kod = k.Baslik.Split(' ')[0];

                    if (kod.StartsWith("P0", StringComparison.OrdinalIgnoreCase)) return "P0 — motor ve emisyon";
                    if (kod.StartsWith("P2", StringComparison.OrdinalIgnoreCase)) return "P2 — motor ve emisyon (ek)";
                    if (kod.StartsWith("P", StringComparison.OrdinalIgnoreCase)) return "P1/P3 — üreticiye özel";
                    if (kod.StartsWith("C", StringComparison.OrdinalIgnoreCase)) return "C — şasi";
                    if (kod.StartsWith("B", StringComparison.OrdinalIgnoreCase)) return "B — gövde";
                    if (kod.StartsWith("U", StringComparison.OrdinalIgnoreCase)) return "U — ağ";

                    return "Diğer";
                };
            }

            if (bolum == Bolumler.Bakim)
            {
                return k => k.Id == Sabitler.BakimKuralId
                    ? "Genel kural"
                    : k.Baslik.Split(new[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Diğer";
            }

            if (bolum == Bolumler.Belirti)
            {
                return k => string.IsNullOrWhiteSpace(k.Kategori)
                    ? "Diğer"
                    : Basliklar.BuyukHarfeBasla(k.Kategori);
            }

            return k => "Tümü";
        }

        private static string KokHub(List<RehberKaydi> kayitlar, string tabanAdres)
        {
            var sb = new StringBuilder(8192);

            Sayfa.Bas(sb, "Garajım Rehber: belirti, arıza kodu, bakım",
                "Araç belirtileri, OBD arıza kodları, bakım aralıkları, TÜVTÜRK muayenesi ve Türkiye'ye özel kurallar tek yerde.",
                Sabitler.Kok, tabanAdres);
            Sayfa.UstCubuk(sb);

            sb.Append("<main class=\"rehber-govde\">\n");

            Sayfa.Breadcrumb(sb, tabanAdres, ("Rehber", null));

            sb.Append("<h1>Garajım Rehber</h1>\n");
            sb.Append("<p class=\"rehber-giris\">Aracının sesini, arıza kodunu ya da bakım aralığını arayın; ")
              .Append(kayitlar.Count).Append(" konu başlığı var.</p>\n");

            sb.Append("<label for=\"rehber-ara\">Ara</label>\n");
            sb.Append("<input id=\"rehber-ara\" type=\"search\" autocomplete=\"off\" placeholder=\"P0420, fren sesi, triger…\">\n");
            sb.Append("<p id=\"rehber-sayac\" class=\"rehber-giris\" role=\"status\"></p>\n");
            sb.Append("<ul id=\"rehber-sonuc\" class=\"rehber-sonuc\"></ul>\n");

            sb.Append("<section class=\"rehber-kartlar\">\n");

            foreach (var bolum in Bolumler.Hepsi)
            {
                var adet = kayitlar.Count(k => k.Bolum == bolum.Yol);

                sb.Append("    <a class=\"rehber-kart\" href=\"/rehber/").Append(bolum.Yol).Append("/\">\n");
                sb.Append("        <h2>").Append(Sayfa.Kacis(bolum.HubBaslik)).Append("</h2>\n");
                sb.Append("        <p>").Append(Sayfa.Kacis(bolum.HubAciklama)).Append("</p>\n");
                sb.Append("        <p class=\"rehber-adet\">").Append(adet).Append(" başlık</p>\n");
                sb.Append("    </a>\n");
            }

            sb.Append("</section>\n");

            Sayfa.Cta(sb, "hub");

            sb.Append("</main>\n");

            Sayfa.Altbilgi(sb);
            Sayfa.Kapat(sb, "/rehber.js");

            return sb.ToString();
        }

        private static string AramaDizini(List<RehberKaydi> kayitlar)
        {
            var sb = new StringBuilder(160 * 1024);
            sb.Append('[');

            var ilk = true;

            foreach (var kayit in kayitlar.OrderBy(k => k.Bolum, StringComparer.Ordinal)
                         .ThenBy(k => k.Id, StringComparer.Ordinal))
            {
                if (!ilk)
                {
                    sb.Append(',');
                }

                ilk = false;

                sb.Append("{\"slug\":\"").Append(Sayfa.JsonKacis(kayit.Bolum + "/" + kayit.Slug))
                  .Append("\",\"baslik\":\"").Append(Sayfa.JsonKacis(kayit.Baslik))
                  .Append("\",\"kategori\":\"").Append(Sayfa.JsonKacis(kayit.Bolum))
                  .Append("\",\"anahtarlar\":[");

                var anahtarlar = kayit.Anahtarlar.Take(8).ToList();

                for (var i = 0; i < anahtarlar.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append('"').Append(Sayfa.JsonKacis(anahtarlar[i])).Append('"');
                }

                sb.Append("]}");
            }

            sb.Append(']');

            return sb.ToString();
        }

        private static string Sitemap(List<RehberKaydi> kayitlar, string tabanAdres)
        {
            var enSonTarih = kayitlar.Count == 0
                ? "2026-01-01"
                : kayitlar.Select(k => k.Guncelleme).OrderBy(t => t, StringComparer.Ordinal).Last();

            var sb = new StringBuilder(64 * 1024);
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

            foreach (var yol in Sabitler.DurgunSayfalar)
            {
                Url(sb, tabanAdres + yol, enSonTarih, yol == "/" ? "1.0" : "0.7");
            }

            foreach (var bolum in Bolumler.Hepsi)
            {
                var bolumKayitlari = kayitlar.Where(k => k.Bolum == bolum.Yol).ToList();

                if (bolumKayitlari.Count == 0)
                {
                    continue;
                }

                Url(sb, tabanAdres + "/rehber/" + bolum.Yol + "/",
                    bolumKayitlari.Select(k => k.Guncelleme).OrderBy(t => t, StringComparer.Ordinal).Last(), "0.7");
            }

            foreach (var kayit in kayitlar.OrderBy(k => k.Bolum, StringComparer.Ordinal)
                         .ThenBy(k => k.Slug, StringComparer.Ordinal))
            {
                Url(sb, tabanAdres + kayit.Url, kayit.Guncelleme, "0.6");
            }

            sb.Append("</urlset>\n");

            return sb.ToString();
        }

        private static void Url(StringBuilder sb, string adres, string tarih, string oncelik)
        {
            sb.Append("  <url>\n    <loc>").Append(Sayfa.Kacis(adres)).Append("</loc>\n");
            sb.Append("    <lastmod>").Append(Sayfa.Kacis(tarih)).Append("</lastmod>\n");
            sb.Append("    <changefreq>monthly</changefreq>\n");
            sb.Append("    <priority>").Append(oncelik).Append("</priority>\n  </url>\n");
        }
    }
}
