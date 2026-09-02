using System.Text.RegularExpressions;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Usta
{
    public static class UstaYanitDenetleyici
    {
        public const string VarsayilanUyari =
            "Bu bir tahmindir, teşhis değildir; kesin sonuç için aracı bir ustaya gösterin.";

        private static readonly string[] GecerliKademeler = { "EnSik", "Sik", "Nadir" };
        private static readonly string[] GecerliAciliyetler = { "Bugun", "BuHafta", "Bakimda" };

        private static readonly Regex YuzdeDeseni = new Regex(@"%\s*\d+(?:[.,]\d+)?|\b\d+(?:[.,]\d+)?\s*%", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> KademeSozu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["EnSik"] = "en sık görülen",
            ["Sik"] = "sık görülen",
            ["Nadir"] = "nadir görülen"
        };

        public static bool Gecerli(UstaYanitDto yanit, out string hata)
        {
            hata = null;

            if (yanit == null)
            {
                hata = "Yanıt boş.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(yanit.Ozet))
            {
                hata = "ozet alanı boş.";
                return false;
            }

            if (yanit.Kademeler == null || yanit.Kademeler.Count == 0)
            {
                hata = "kademeler listesi boş.";
                return false;
            }

            foreach (var kademe in yanit.Kademeler)
            {
                if (kademe == null || !GecerliKademeler.Contains(kademe.Kademe, StringComparer.OrdinalIgnoreCase))
                {
                    hata = "kademe değeri EnSik/Sik/Nadir dışında.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(kademe.Neden))
                {
                    hata = "kademe.neden boş.";
                    return false;
                }

                if (!GecerliAciliyetler.Contains(kademe.Aciliyet, StringComparer.OrdinalIgnoreCase))
                {
                    hata = "aciliyet değeri Bugun/BuHafta/Bakimda dışında.";
                    return false;
                }

                if (kademe.MaliyetTl == null || kademe.MaliyetTl.Count != 2 ||
                    kademe.MaliyetTl[0] < 0 || kademe.MaliyetTl[1] < kademe.MaliyetTl[0])
                {
                    hata = "maliyetTl [min,max] biçiminde değil.";
                    return false;
                }
            }

            return true;
        }

        public static UstaYanitDto SonFiltre(UstaYanitDto yanit)
        {
            yanit.Ozet = YuzdeSil(yanit.Ozet, null);
            yanit.UstayaBoyleAnlat = YuzdeSil(yanit.UstayaBoyleAnlat, null);

            foreach (var kademe in yanit.Kademeler)
            {
                var soz = KademeSozu.TryGetValue(kademe.Kademe ?? string.Empty, out var deger) ? deger : "olası";
                kademe.Neden = YuzdeSil(kademe.Neden, soz);
                kademe.BelirtiUyumu = YuzdeSil(kademe.BelirtiUyumu, soz);
                kademe.EvdeKontrol = YuzdeSil(kademe.EvdeKontrol, soz);
            }

            yanit.AracVerisindenNotlar = (yanit.AracVerisindenNotlar ?? new List<string>())
                .Select(n => YuzdeSil(n, null))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            yanit.TakipSorulari = (yanit.TakipSorulari ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(2)
                .ToList();

            if (string.IsNullOrWhiteSpace(yanit.Uyari))
            {
                yanit.Uyari = VarsayilanUyari;
            }
            else
            {
                yanit.Uyari = YuzdeSil(yanit.Uyari, null);
            }

            return yanit;
        }

        private static string YuzdeSil(string metin, string kademeSozu)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return metin;
            }

            var temiz = YuzdeDeseni.Replace(metin, kademeSozu ?? "olası");
            return Regex.Replace(temiz, @"\s{2,}", " ").Trim();
        }

        public static UstaYanitDto KirmiziCizgiYaniti(KirmiziCizgiBulgusu bulgu)
        {
            return new UstaYanitDto
            {
                Ozet = bulgu.Baslik,
                KirmiziCizgi = true,
                Kademeler = new List<UstaKademeDto>
                {
                    new UstaKademeDto
                    {
                        Kademe = "EnSik",
                        Neden = bulgu.Baslik,
                        BelirtiUyumu = "Anlattığın belirti doğrudan bu tabloya giriyor.",
                        EvdeKontrol = "Evde kontrol denemeyin; araç güvenli yerde durdurulmalı.",
                        MaliyetTl = new List<decimal> { 0m, 0m },
                        Aciliyet = "Bugun"
                    }
                },
                AracVerisindenNotlar = new List<string>(),
                UstayaBoyleAnlat = bulgu.Baslik + " diyorum; araç yola çıkmadan kontrol edilmeli.",
                TakipSorulari = new List<string>(),
                Uyari = KirmiziCizgiler.Cevap
            };
        }
    }
}
