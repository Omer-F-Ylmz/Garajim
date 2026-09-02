using System.Text;
using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    public class HasarFotoForm
    {
        public IFormFile File { get; set; }
        public HasarFotoEtiketi Etiket { get; set; }
    }

    [Route("api/[controller]")]
    public class HasarController : SecureControllerBase
    {
        private readonly IHasarService _hasarService;

        public HasarController(IHasarService hasarService)
        {
            _hasarService = hasarService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int? aracId)
        {
            return Sonuc(await _hasarService.GetListAsync(CurrentUserId, aracId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            return Sonuc(await _hasarService.GetAsync(CurrentUserId, id));
        }

        [HttpPost]
        public async Task<IActionResult> Olustur(HasarOlusturDto dto)
        {
            return Sonuc(await _hasarService.OlusturAsync(CurrentUserId, dto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Guncelle(int id, HasarGuncelleDto dto)
        {
            return Sonuc(await _hasarService.GuncelleAsync(CurrentUserId, id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Sil(int id)
        {
            return Sonuc(await _hasarService.SilAsync(CurrentUserId, id));
        }

        [HttpPost("{id}/foto")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> FotoEkle(int id, [FromForm] HasarFotoForm form)
        {
            if (form?.File == null || form.File.Length == 0)
                return BadRequest(new Core.Utilities.Results.ErrorDataResult<HasarFotoDto>(Messages.InvalidValue));

            using var akis = new MemoryStream();
            await form.File.CopyToAsync(akis);

            return Sonuc(await _hasarService.FotoEkleAsync(CurrentUserId, id, form.Etiket, form.File.FileName, akis.ToArray()));
        }

        [HttpDelete("{id}/foto/{fotoId}")]
        public async Task<IActionResult> FotoSil(int id, int fotoId)
        {
            return Sonuc(await _hasarService.FotoSilAsync(CurrentUserId, id, fotoId));
        }

        [HttpGet("{id}/tutanak.html")]
        public async Task<IActionResult> Tutanak(int id)
        {
            var result = await _hasarService.GetAsync(CurrentUserId, id);
            if (!result.Success)
            {
                return Sonuc(result);
            }

            return Content(TutanakSayfasi.Olustur(result.Data), "text/html; charset=utf-8");
        }

        private IActionResult Sonuc(Core.Utilities.Results.IResult result)
        {
            if (result.Success)
                return Ok(result);

            if (result.Message == Messages.AuthorizationDenied)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            if (result.Message == Messages.VehicleNotFound || result.Message == Messages.UserNotFound ||
                result.Message == Messages.HasarDosyasiBulunamadi || result.Message == Messages.HasarFotoBulunamadi)
                return NotFound(result);

            return BadRequest(result);
        }
    }
}
