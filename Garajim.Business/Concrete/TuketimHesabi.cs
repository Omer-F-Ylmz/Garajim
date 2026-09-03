using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class TuketimSonucu
    {
        public decimal? Litre100Km { get; set; }
        public decimal? Kwh100Km { get; set; }
        public int OlculenKm { get; set; }
        public decimal OlculenLitre { get; set; }
        public decimal OlculenKwh { get; set; }
        public decimal OlculenTutar { get; set; }
        public HashSet<int> SupheliKayitlar { get; } = new HashSet<int>();
    }

    public static class TuketimHesabi
    {
        public const decimal LitreEnAz = 2m;
        public const decimal LitreEnCok = 40m;
        public const decimal KwhEnAz = 8m;
        public const decimal KwhEnCok = 60m;

        public static TuketimSonucu Olcumlerden(IEnumerable<YakitOlcumDto> olcumler)
        {
            var sirali = (olcumler ?? Enumerable.Empty<YakitOlcumDto>())
                .Where(o => o.Km > 0)
                .OrderBy(o => o.Km)
                .Select((o, sira) => new Nokta(sira + 1, o.Km, o.Litre, o.Kwh, 0m, o.TamDolum))
                .ToList();

            return Coz(sirali);
        }

        public static TuketimSonucu Hesapla(IEnumerable<FuelRecord> kayitlar)
        {
            var sirali = (kayitlar ?? Enumerable.Empty<FuelRecord>())
                .Where(k => k.Km > 0)
                .OrderBy(k => k.Km)
                .ThenBy(k => k.Id)
                .Select(k => new Nokta(k.Id, k.Km, k.Liters, k.Kwh ?? 0m, k.TotalCost, k.TamDolum))
                .ToList();

            return Coz(sirali);
        }

        private readonly record struct Nokta(int Id, int Km, decimal Litre, decimal Kwh, decimal Tutar, bool TamDolum);

        private static TuketimSonucu Coz(List<Nokta> sirali)
        {
            var sonuc = new TuketimSonucu();

            var tamIndeksler = Enumerable.Range(0, sirali.Count).Where(i => sirali[i].TamDolum).ToList();

            var litreKm = 0;
            var litreToplam = 0m;
            var kwhKm = 0;
            var kwhToplam = 0m;

            for (var s = 1; s < tamIndeksler.Count; s++)
            {
                var bas = tamIndeksler[s - 1];
                var son = tamIndeksler[s];
                var aralik = sirali[son].Km - sirali[bas].Km;

                if (aralik <= 0)
                {
                    continue;
                }

                var litre = 0m;
                var kwh = 0m;
                var tutar = 0m;

                for (var i = bas + 1; i <= son; i++)
                {
                    litre += sirali[i].Litre;
                    kwh += sirali[i].Kwh;
                    tutar += sirali[i].Tutar;
                }

                var supheli = false;

                if (litre > 0)
                {
                    var tuketim = litre / aralik * 100m;
                    if (tuketim < LitreEnAz || tuketim > LitreEnCok)
                    {
                        supheli = true;
                    }
                }

                if (kwh > 0)
                {
                    var tuketim = kwh / aralik * 100m;
                    if (tuketim < KwhEnAz || tuketim > KwhEnCok)
                    {
                        supheli = true;
                    }
                }

                if (supheli)
                {
                    sonuc.SupheliKayitlar.Add(sirali[son].Id);
                    continue;
                }

                sonuc.OlculenKm += aralik;
                sonuc.OlculenTutar += tutar;

                if (litre > 0)
                {
                    litreKm += aralik;
                    litreToplam += litre;
                    sonuc.OlculenLitre += litre;
                }

                if (kwh > 0)
                {
                    kwhKm += aralik;
                    kwhToplam += kwh;
                    sonuc.OlculenKwh += kwh;
                }
            }

            if (litreKm > 0 && litreToplam > 0)
            {
                sonuc.Litre100Km = Math.Round(litreToplam / litreKm * 100m, 2);
            }

            if (kwhKm > 0 && kwhToplam > 0)
            {
                sonuc.Kwh100Km = Math.Round(kwhToplam / kwhKm * 100m, 2);
            }

            return sonuc;
        }
    }
}
