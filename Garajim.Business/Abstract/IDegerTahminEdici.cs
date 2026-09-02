namespace Garajim.Business.Abstract
{
    public class DegerTahminiSonucu
    {
        public bool KapsamDisi { get; set; }
        public decimal? Fiyat { get; set; }
    }

    public interface IDegerTahminEdici
    {
        DegerTahminiSonucu Tahmin(string marka, string seri, int yil, int kilometre, string yakitTipi, string vitesTipi, string kasaTipi);
    }
}
