namespace Garajim.Calibration
{
    public static class Karsilastirici
    {
        private const decimal Tolerans = 0.01m;

        public static bool OndalikEsit(decimal? beklenen, decimal? gelen)
        {
            if (beklenen == null && gelen == null)
            {
                return true;
            }

            if (beklenen == null || gelen == null)
            {
                return false;
            }

            return Math.Abs(beklenen.Value - gelen.Value) <= Tolerans;
        }

        public static bool TamsayiEsit(int? beklenen, int? gelen)
        {
            return beklenen == gelen;
        }

        public static bool PlakaEsit(string beklenen, string gelen)
        {
            return CevapAnahtari.PlakaNormalize(beklenen) == CevapAnahtari.PlakaNormalize(gelen);
        }

        public static bool TarihEsit(DateTime? beklenen, DateTime? gelen)
        {
            return beklenen?.Date == gelen?.Date;
        }

        public static bool MetinEsit(string beklenen, string gelen)
        {
            return string.Equals(beklenen, gelen, StringComparison.OrdinalIgnoreCase);
        }
    }
}
