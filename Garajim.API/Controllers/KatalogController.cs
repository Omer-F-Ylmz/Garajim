using Garajim.Business.Katalog;
using Garajim.Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class KatalogController : SecureControllerBase
    {
        public const int OnbellekSaniye = 3600;

        private readonly AracKatalogu _katalog;

        public KatalogController(AracKatalogu katalog)
        {
            _katalog = katalog;
        }

        [HttpGet("markalar")]
        public IActionResult Markalar()
        {
            Onbellekle();
            return Ok(new SuccessDataResult<List<string>>(_katalog.MarkaAdlari.ToList(), _katalog.Surum));
        }

        [HttpGet("seriler")]
        public IActionResult Seriler([FromQuery] string marka)
        {
            if (!_katalog.MarkaVar(marka))
            {
                return NotFound(new ErrorDataResult<List<string>>(Business.Constants.Messages.MarkaKatalogdaYok));
            }

            Onbellekle();
            return Ok(new SuccessDataResult<List<string>>(_katalog.Seriler(marka).ToList(), _katalog.Surum));
        }

        private void Onbellekle()
        {
            Response.Headers.CacheControl = "private, max-age=" + OnbellekSaniye;
        }
    }
}
