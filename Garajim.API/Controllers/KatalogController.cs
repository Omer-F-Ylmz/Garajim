using Garajim.Business.Katalog;
using Garajim.Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class KatalogController : SecureControllerBase
    {
        public const int OnbellekSaniye = 86400;
        public const int SayfaBoyutu = 50;
        public const string SurumBasligi = "X-Katalog-Surum";

        public class KatalogSayfasi
        {
            public int Toplam { get; set; }

            public int Sayfa { get; set; }

            public int Boyut { get; set; }

            public bool DahaVar { get; set; }

            public List<KatalogGirdisi> Kayitlar { get; set; } = new List<KatalogGirdisi>();
        }

        private readonly AracKatalogu _katalog;

        public KatalogController(AracKatalogu katalog)
        {
            _katalog = katalog;
        }

        [HttpGet("markalar")]
        public IActionResult Markalar([FromQuery] string q, [FromQuery] int? sayfa)
        {
            var zarfIster = !string.IsNullOrWhiteSpace(q) || sayfa != null;

            if (Degismedi("markalar:" + Kapsam(q, sayfa)))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (!zarfIster)
            {
                return Ok(new SuccessDataResult<List<string>>(_katalog.MarkaAdlari.ToList(), _katalog.EtiketSurumu));
            }

            var istenen = GecerliSayfa(sayfa);

            return Ok(new SuccessDataResult<KatalogSayfasi>(
                Sayfala(_katalog.MarkaAra(q, SayfaBoyutu, istenen), istenen), _katalog.EtiketSurumu));
        }

        [HttpGet("seriler")]
        public IActionResult Seriler([FromQuery] string marka, [FromQuery] string q, [FromQuery] int? sayfa)
        {
            if (!_katalog.MarkaVar(marka))
            {
                return NotFound(new ErrorDataResult<List<string>>(Business.Constants.Messages.MarkaKatalogdaYok));
            }

            var zarfIster = !string.IsNullOrWhiteSpace(q) || sayfa != null;

            if (Degismedi("seriler:" + Uri.EscapeDataString(marka) + ":" + Kapsam(q, sayfa)))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            if (!zarfIster)
            {
                return Ok(new SuccessDataResult<List<string>>(_katalog.Seriler(marka).ToList(), _katalog.EtiketSurumu));
            }

            var istenen = GecerliSayfa(sayfa);

            return Ok(new SuccessDataResult<KatalogSayfasi>(
                Sayfala(_katalog.SeriAra(marka, q, SayfaBoyutu, istenen), istenen), _katalog.EtiketSurumu));
        }

        private static KatalogSayfasi Sayfala(KatalogAramaSonucu sonuc, int sayfa)
        {
            return new KatalogSayfasi
            {
                Toplam = sonuc.Toplam,
                Sayfa = sayfa,
                Boyut = SayfaBoyutu,
                DahaVar = sonuc.DahaVar,
                Kayitlar = sonuc.Kayitlar
            };
        }

        private static int GecerliSayfa(int? sayfa)
        {
            return sayfa == null || sayfa.Value < 1 ? 1 : sayfa.Value;
        }

        private static string Kapsam(string q, int? sayfa)
        {
            return Uri.EscapeDataString((q ?? string.Empty).Trim().ToLowerInvariant()) + "|" + GecerliSayfa(sayfa);
        }

        private bool Degismedi(string kapsam)
        {
            var etiket = "\"" + _katalog.EtiketSurumu + ":" + kapsam + "\"";

            Response.Headers.CacheControl = "private, max-age=" + OnbellekSaniye;
            Response.Headers.ETag = etiket;
            Response.Headers[SurumBasligi] = _katalog.EtiketSurumu;

            return Request.Headers.IfNoneMatch.Any(g => g == etiket);
        }
    }
}
