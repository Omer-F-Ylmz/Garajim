using Garajim.API.Controllers;
using Garajim.Business.Abstract;
using Garajim.ML.DataPrep;
using Garajim.ML.Models;
using Microsoft.Extensions.ML;

namespace Garajim.API.Startup
{
    public class MlDegerTahminEdici : IDegerTahminEdici
    {
        private readonly PredictionEnginePool<CarPriceInput, CarPricePrediction> _havuz;
        private readonly FiyatModeliSozlugu _sozluk;

        public MlDegerTahminEdici(PredictionEnginePool<CarPriceInput, CarPricePrediction> havuz, FiyatModeliSozlugu sozluk)
        {
            _havuz = havuz;
            _sozluk = sozluk;
        }

        public DegerTahminiSonucu Tahmin(string marka, string seri, int yil, int kilometre, string yakitTipi, string vitesTipi)
        {
            if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(seri))
            {
                return new DegerTahminiSonucu { KapsamDisi = true };
            }

            if (yil < CarCsvLoader.MinYear || yil > DateTime.Now.Year + 1
                || kilometre < 0 || kilometre > CarCsvLoader.MaxKilometre)
            {
                return new DegerTahminiSonucu { KapsamDisi = true };
            }

            if (!_sozluk.MarkaTaniniyor(marka) || !_sozluk.SeriTaniniyor(seri))
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
                KasaTipi = string.Empty
            };

            var tahmin = _havuz.Predict(PricePredictionController.ModelName, girdi);
            var fiyat = PriceScale.FromLog(tahmin.LogFiyat);

            if (float.IsNaN(fiyat) || float.IsInfinity(fiyat) || fiyat <= 0)
            {
                return new DegerTahminiSonucu { KapsamDisi = true };
            }

            return new DegerTahminiSonucu { Fiyat = Math.Round((decimal)fiyat) };
        }
    }
}
