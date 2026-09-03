using Garajim.API.Controllers;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete;
using Garajim.ML.DataPrep;
using Garajim.ML.Models;
using Microsoft.Extensions.ML;

namespace Garajim.API.Startup
{
    public class MlDegerTahminEdici : IDegerTahminEdici
    {
        private readonly Lazy<PredictionEnginePool<CarPriceInput, CarPricePrediction>> _havuz;
        private readonly Lazy<FiyatModeliSozlugu> _sozluk;

        public MlDegerTahminEdici(Lazy<PredictionEnginePool<CarPriceInput, CarPricePrediction>> havuz, Lazy<FiyatModeliSozlugu> sozluk)
        {
            _havuz = havuz;
            _sozluk = sozluk;
        }

        public DegerTahminiSonucu Tahmin(string marka, string seri, int yil, int kilometre, string yakitTipi, string vitesTipi, string kasaTipi)
        {
            if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(seri) || string.IsNullOrWhiteSpace(kasaTipi))
            {
                return new DegerTahminiSonucu { KapsamDisi = true };
            }

            if (yil < CarCsvLoader.MinYear || yil > DegerSinirlari.EnYeniYil()
                || kilometre < 0 || kilometre > CarCsvLoader.MaxKilometre)
            {
                return new DegerTahminiSonucu { KapsamDisi = true };
            }

            var sozluk = _sozluk.Value;

            if (!sozluk.MarkaTaniniyor(marka) || !sozluk.SeriTaniniyor(seri) || !sozluk.KasaTaniniyor(kasaTipi))
            {
                return new DegerTahminiSonucu { KapsamDisi = true };
            }

            var girdi = new CarPriceInput
            {
                Marka = marka.Trim(),
                Seri = seri.Trim(),
                Yil = yil,
                Kilometre = kilometre,
                YakitTipi = (yakitTipi ?? string.Empty).Trim(),
                VitesTipi = (vitesTipi ?? string.Empty).Trim(),
                KasaTipi = kasaTipi.Trim()
            };

            var tahmin = _havuz.Value.Predict(PricePredictionController.ModelName, girdi);
            var fiyat = PriceScale.FromLog(tahmin.LogFiyat);

            if (float.IsNaN(fiyat) || float.IsInfinity(fiyat) || fiyat <= 0)
            {
                return new DegerTahminiSonucu { KapsamDisi = true };
            }

            return new DegerTahminiSonucu { Fiyat = Math.Round((decimal)fiyat) };
        }
    }
}
