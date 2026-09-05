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
            if (Degismedi("markalar"))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(new SuccessDataResult<List<string>>(_katalog.MarkaAdlari.ToList(), _katalog.Surum));
        }

        [HttpGet("seriler")]
        public IActionResult Seriler([FromQuery] string marka)
        {
            if (!_katalog.MarkaVar(marka))
            {
                return NotFound(new ErrorDataResult<List<string>>(Business.Constants.Messages.MarkaKatalogdaYok));
            }

            if (Degismedi("seriler:" + marka))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(new SuccessDataResult<List<string>>(_katalog.Seriler(marka).ToList(), _katalog.Surum));
        }

        private bool Degismedi(string kapsam)
        {
            var etiket = "\"" + _katalog.Surum + ":" + kapsam + "\"";

            Response.Headers.CacheControl = "private, max-age=" + OnbellekSaniye;
            Response.Headers.ETag = etiket;

            return Request.Headers.IfNoneMatch.Any(g => g == etiket);
        }
    }
}
