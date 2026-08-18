namespace Garajim.ML.Models
{
    public static class PriceScale
    {
        public static float ToLog(float fiyat)
        {
            return MathF.Log(fiyat);
        }

        public static float FromLog(float logFiyat)
        {
            return MathF.Exp(logFiyat);
        }
    }
}
