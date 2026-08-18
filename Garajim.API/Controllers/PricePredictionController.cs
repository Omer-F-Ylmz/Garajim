using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;
using Garajim.ML.DataPrep;
using Garajim.ML.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;

namespace Garajim.API.Controllers
{
    [Route("api/price")]
    public class PricePredictionController : SecureControllerBase
    {
        public const string ModelName = "PriceModel";

        private readonly PredictionEnginePool<CarPriceInput, CarPricePrediction> _predictionEnginePool;

        public PricePredictionController(PredictionEnginePool<CarPriceInput, CarPricePrediction> predictionEnginePool)
        {
            _predictionEnginePool = predictionEnginePool;
        }

        [HttpPost("estimate")]
        public IActionResult Estimate(PriceEstimateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Marka)
                || string.IsNullOrWhiteSpace(dto.Seri)
                || string.IsNullOrWhiteSpace(dto.YakitTipi)
                || string.IsNullOrWhiteSpace(dto.VitesTipi)
                || string.IsNullOrWhiteSpace(dto.KasaTipi))
            {
                return BadRequest(new ErrorDataResult<PriceEstimateResultDto>(Messages.PriceInputRequired));
            }

            if (dto.Yil < CarCsvLoader.MinYear || dto.Yil > DateTime.Now.Year + 1)
            {
                return BadRequest(new ErrorDataResult<PriceEstimateResultDto>(Messages.PriceYearOutOfRange));
            }

            if (dto.Kilometre < 0 || dto.Kilometre > CarCsvLoader.MaxKilometre)
            {
                return BadRequest(new ErrorDataResult<PriceEstimateResultDto>(Messages.PriceKilometreOutOfRange));
            }

            var input = new CarPriceInput
            {
                Marka = dto.Marka.Trim(),
                Seri = dto.Seri.Trim(),
                Yil = dto.Yil,
                Kilometre = dto.Kilometre,
                YakitTipi = dto.YakitTipi.Trim(),
                VitesTipi = dto.VitesTipi.Trim(),
                KasaTipi = dto.KasaTipi.Trim()
            };

            var prediction = _predictionEnginePool.Predict(ModelName, input);
            var tahminiFiyat = PriceScale.FromLog(prediction.LogFiyat);

            if (float.IsNaN(tahminiFiyat) || float.IsInfinity(tahminiFiyat) || tahminiFiyat <= 0)
            {
                return BadRequest(new ErrorDataResult<PriceEstimateResultDto>(Messages.PriceEstimateFailed));
            }

            var result = new PriceEstimateResultDto
            {
                TahminiFiyat = Math.Round((decimal)tahminiFiyat),
                ParaBirimi = "TL",
                Marka = input.Marka,
                Seri = input.Seri,
                Yil = dto.Yil,
                Kilometre = dto.Kilometre
            };

            return Ok(new SuccessDataResult<PriceEstimateResultDto>(result, Messages.PriceEstimated));
        }
    }
}
