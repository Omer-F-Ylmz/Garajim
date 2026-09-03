namespace Garajim.Business.Concrete
{
    public static class DegerSinirlari
    {
        public const int EnEskiYil = 1950;
        public const int MaxKm = 2_000_000;
        public const decimal MaxTutar = 5_000_000m;
        public const decimal MaxLitre = 1_500m;
        public const decimal MaxKwh = 500m;

        public static int EnYeniYil()
        {
            return DateTime.UtcNow.Year + 1;
        }

        public static bool YilGecerli(int yil)
        {
            return yil >= EnEskiYil && yil <= EnYeniYil();
        }

        public static bool KmGecerli(int km)
        {
            return km >= 0 && km <= MaxKm;
        }

        public static bool TutarGecerli(decimal tutar)
        {
            return tutar >= 0 && tutar <= MaxTutar;
        }

        public static bool LitreGecerli(decimal litre)
        {
            return litre >= 0 && litre <= MaxLitre;
        }

        public static bool KwhGecerli(decimal? kwh)
        {
            return kwh == null || (kwh.Value >= 0 && kwh.Value <= MaxKwh);
        }

        public static bool GecmisTarih(DateTime tarih)
        {
            return tarih != default
                && tarih.Year >= EnEskiYil
                && tarih.Date <= TarihToleransi.EnGecGun();
        }
    }
}
