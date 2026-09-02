using Garajim.Business.Abstract;

namespace Garajim.Tests.Integration
{
    public class SahteDegerTahminEdici : IDegerTahminEdici
    {
        public HashSet<string> KapsamdakiSeriler { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Clio", "Egea", "Corolla" };

        public decimal SabitFiyat { get; set; } = 725000m;

        public int CagriSayisi { get; private set; }

        public DegerTahminiSonucu Tahmin(string marka, string seri, int yil, int kilometre, string yakitTipi, string vitesTipi)
        {
            CagriSayisi++;

            if (string.IsNullOrWhiteSpace(seri) || !KapsamdakiSeriler.Contains(seri.Trim()))
            {
                return new DegerTahminiSonucu { KapsamDisi = true };
            }

            return new DegerTahminiSonucu { Fiyat = SabitFiyat };
        }
    }
}
