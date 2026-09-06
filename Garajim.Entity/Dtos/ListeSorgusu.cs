namespace Garajim.Entity.Dtos
{
    public class SiralamaSonucu
    {
        public bool Gecerli { get; set; }
        public string Alan { get; set; }
        public bool Artan { get; set; }
    }

    public class SayfaliSonuc<T>
    {
        public SayfaliSonuc()
        {
        }

        public SayfaliSonuc(List<T> kayitlar, int toplam, int sayfa, int boyut)
        {
            Kayitlar = kayitlar;
            Toplam = toplam;
            Sayfa = sayfa;
            Boyut = boyut;
        }

        public int Toplam { get; set; }
        public int Sayfa { get; set; }
        public int Boyut { get; set; }
        public List<T> Kayitlar { get; set; } = new List<T>();
    }

    public class ListeSorgusu
    {
        public const int VarsayilanBoyut = 25;
        public const int EnBuyukBoyut = 100;
        public const int AramaUzunlugu = 200;

        public string Q { get; set; }
        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public string Sirala { get; set; }
        public int? Sayfa { get; set; }
        public int? Boyut { get; set; }

        public bool ZarfIster =>
            !string.IsNullOrWhiteSpace(Q)
            || !string.IsNullOrWhiteSpace(Sirala)
            || Sayfa != null
            || Boyut != null
            || Baslangic != null
            || Bitis != null;

        public int GecerliBoyut()
        {
            if (Boyut == null || Boyut.Value <= 0)
            {
                return VarsayilanBoyut;
            }

            return Boyut.Value > EnBuyukBoyut ? EnBuyukBoyut : Boyut.Value;
        }

        public int GecerliSayfa()
        {
            return Sayfa == null || Sayfa.Value < 1 ? 1 : Sayfa.Value;
        }

        public string GecerliQ()
        {
            if (string.IsNullOrWhiteSpace(Q))
            {
                return null;
            }

            var temiz = Q.Trim();

            return temiz.Length <= AramaUzunlugu ? temiz : temiz.Substring(0, AramaUzunlugu);
        }

        public SiralamaSonucu SiralamaCoz(IReadOnlyCollection<string> izinliAlanlar, string varsayilanAlan)
        {
            if (string.IsNullOrWhiteSpace(Sirala))
            {
                return new SiralamaSonucu { Gecerli = true, Alan = varsayilanAlan, Artan = false };
            }

            var parcalar = Sirala.Trim().Split(':');

            if (parcalar.Length > 2)
            {
                return new SiralamaSonucu { Gecerli = false };
            }

            var alan = parcalar[0].Trim().ToLowerInvariant();

            if (!izinliAlanlar.Contains(alan))
            {
                return new SiralamaSonucu { Gecerli = false };
            }

            if (parcalar.Length == 1)
            {
                return new SiralamaSonucu { Gecerli = true, Alan = alan, Artan = true };
            }

            var yon = parcalar[1].Trim().ToLowerInvariant();

            if (yon != "asc" && yon != "desc")
            {
                return new SiralamaSonucu { Gecerli = false };
            }

            return new SiralamaSonucu { Gecerli = true, Alan = alan, Artan = yon == "asc" };
        }
    }
}
