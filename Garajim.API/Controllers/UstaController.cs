using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Garajim.API.Startup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    [EnableRateLimiting(PahaliUclar.RateLimitPolicy)]
    [Route("api/[controller]")]
    public class UstaController : SecureControllerBase
    {
        public const string OnayGerekliKodu = "ONAY_GEREKLI";
        public const string LimitKodu = "GUNLUK_LIMIT";

        private readonly IUstaService _ustaService;

        public UstaController(IUstaService ustaService)
        {
            _ustaService = ustaService;
        }

        [HttpGet("onay")]
        public async Task<IActionResult> OnayDurumu()
        {
            return Sonuc(await _ustaService.OnayDurumuAsync(CurrentUserId));
        }

        [HttpPost("onay")]
        public async Task<IActionResult> OnayVer(UstaOnayVerDto dto)
        {
            return Sonuc(await _ustaService.OnayVerAsync(CurrentUserId, dto));
        }

        [HttpPost("sohbet")]
        public async Task<IActionResult> SohbetOlustur(UstaSohbetOlusturDto dto)
        {
            return Sonuc(await _ustaService.SohbetOlusturAsync(CurrentUserId, dto));
        }

        [HttpPost("sohbet/{id}/mesaj")]
        public async Task<IActionResult> MesajGonder(int id, UstaMesajGonderDto dto, CancellationToken ct)
        {
            return Sonuc(await _ustaService.MesajGonderAsync(CurrentUserId, id, dto, ct));
        }

        [HttpGet("sohbet")]
        public async Task<IActionResult> SohbetListesi([FromQuery] int? aracId)
        {
            return Sonuc(await _ustaService.SohbetListesiAsync(CurrentUserId, aracId));
        }

        [HttpGet("sohbet/{id}")]
        public async Task<IActionResult> Sohbet(int id)
        {
            return Sonuc(await _ustaService.SohbetAsync(CurrentUserId, id));
        }

        [HttpGet("sohbet/{id}/bakimlar")]
        public async Task<IActionResult> CozumBakimlari(int id)
        {
            return Sonuc(await _ustaService.CozumBakimSecenekleriAsync(CurrentUserId, id));
        }

        [HttpDelete("sohbet/{id}")]
        public async Task<IActionResult> SohbetSil(int id)
        {
            return Sonuc(await _ustaService.SohbetSilAsync(CurrentUserId, id));
        }

        [HttpPost("mesaj/{id}/geri-bildirim")]
        public async Task<IActionResult> GeriBildirim(int id, UstaGeriBildirimDto dto)
        {
            return Sonuc(await _ustaService.GeriBildirimAsync(CurrentUserId, id, dto));
        }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            return Sonuc(await _ustaService.StatsAsync(CurrentUserId));
        }

        private IActionResult Sonuc(Core.Utilities.Results.IResult result)
        {
            if (result.Success)
                return Ok(result);

            if (result.Message == Messages.UstaOnayGerekli)
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = result.Message, kod = OnayGerekliKodu });

            if (result.Message == Messages.AuthorizationDenied)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            if (result.Message == Messages.UstaGunlukLimit)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = result.Message, kod = LimitKodu });

            if (result.Message == Messages.UstaYanitAlinamadi)
                return StatusCode(StatusCodes.Status502BadGateway, result);

            if (result.Message == Messages.VehicleNotFound || result.Message == Messages.UserNotFound ||
                result.Message == Messages.UstaSohbetBulunamadi || result.Message == Messages.UstaMesajBulunamadi)
                return NotFound(result);

            return BadRequest(result);
        }
    }
}
