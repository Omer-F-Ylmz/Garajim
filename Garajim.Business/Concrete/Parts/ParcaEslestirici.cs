using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete.Parts
{
    public static class ParcaEslestirici
    {
        private static readonly string[] AtlanacakKelimeler =
        {
            "iscilik", "isc.", "montaj", "kargo", "nakliye", "hizmet bedeli", "servis bedeli"
        };

        private static readonly (string[] Kelimeler, ParcaTuru Tur)[] Tablo =
        {
            (new[] { "yag filtresi", "yag filtre" }, ParcaTuru.YagFiltresi),
            (new[] { "hava filtresi", "hava filtre" }, ParcaTuru.HavaFiltresi),
            (new[] { "polen filtresi", "polen filtre", "kabin filtre" }, ParcaTuru.PolenFiltresi),
            (new[] { "yakit filtresi", "yakit filtre", "mazot filtre" }, ParcaTuru.YakitFiltresi),
            (new[] { "motor yagi", "5w30", "5w-30", "10w40", "10w-40", "0w20", "0w-20", "15w40", "sentetik yag" }, ParcaTuru.MotorYagi),
            (new[] { "on balata", "on fren balata", "onbalata" }, ParcaTuru.FrenBalatasiOn),
            (new[] { "arka balata", "arka fren balata" }, ParcaTuru.FrenBalatasiArka),
            (new[] { "on disk", "on fren disk" }, ParcaTuru.FrenDiskiOn),
            (new[] { "arka disk", "arka fren disk" }, ParcaTuru.FrenDiskiArka),
            (new[] { "buji" }, ParcaTuru.Buji),
            (new[] { "triger" }, ParcaTuru.TrigerSeti),
            (new[] { "v kayis", "vkayis", "alternator kayis" }, ParcaTuru.VKayisi),
            (new[] { "aku" }, ParcaTuru.Aku),
            (new[] { "lastik" }, ParcaTuru.Lastik),
            (new[] { "amortisor" }, ParcaTuru.Amortisor),
            (new[] { "silecek" }, ParcaTuru.Silecek),
            (new[] { "antifriz", "sogutma suyu" }, ParcaTuru.Antifriz),
            (new[] { "fren hidrolig", "dot4", "dot 4" }, ParcaTuru.FrenHidroligi),
            (new[] { "sanziman yagi", "vites yagi" }, ParcaTuru.SanzimanYagi),
            (new[] { "devirdaim", "su pompasi" }, ParcaTuru.Devirdaim),
            (new[] { "rot basi", "rotbasi" }, ParcaTuru.RotBasi),
            (new[] { "salincak" }, ParcaTuru.Salincak),
            (new[] { "debriyaj" }, ParcaTuru.Debriyaj),
            (new[] { "balata" }, ParcaTuru.FrenBalatasiOn),
            (new[] { "fren disk", "disk" }, ParcaTuru.FrenDiskiOn)
        };

        public static string Sadelestir(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
            {
                return string.Empty;
            }

            var kucuk = metin.Trim().ToLowerInvariant()
                .Replace('ı', 'i').Replace('İ', 'i')
                .Replace('ş', 's').Replace('Ş', 's')
                .Replace('ğ', 'g').Replace('Ğ', 'g')
                .Replace('ü', 'u').Replace('Ü', 'u')
                .Replace('ö', 'o').Replace('Ö', 'o')
                .Replace('ç', 'c').Replace('Ç', 'c');

            return kucuk;
        }

        public static bool AtlanmaliMi(string aciklama)
        {
            var sade = Sadelestir(aciklama);
            return sade.Length > 0 && AtlanacakKelimeler.Any(k => sade.Contains(k));
        }

        public static ParcaTuru Esle(string aciklama)
        {
            var sade = Sadelestir(aciklama);
            if (sade.Length == 0)
            {
                return ParcaTuru.Diger;
            }

            foreach (var (kelimeler, tur) in Tablo)
            {
                if (kelimeler.Any(k => sade.Contains(k)))
                {
                    return tur;
                }
            }

            return ParcaTuru.Diger;
        }

        public static List<MaintenancePartDto> Cevir(List<ReceiptItemResult> kalemler)
        {
            var parcalar = new List<MaintenancePartDto>();

            foreach (var kalem in kalemler ?? new List<ReceiptItemResult>())
            {
                if (string.IsNullOrWhiteSpace(kalem.Ad) || AtlanmaliMi(kalem.Ad))
                {
                    continue;
                }

                parcalar.Add(new MaintenancePartDto
                {
                    ParcaTuru = Esle(kalem.Ad),
                    Aciklama = kalem.Ad.Trim(),
                    Adet = 1,
                    Tutar = kalem.Tutar
                });
            }

            return parcalar;
        }
    }
}
