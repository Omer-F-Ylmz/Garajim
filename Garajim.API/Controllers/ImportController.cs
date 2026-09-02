using System.Text.Json;
using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Garajim.API.Startup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    public class ImportOnizleForm
    {
        public IFormFile File { get; set; }
        public string KayitTuru { get; set; }
    }

    public class ImportUygulaForm
    {
        public IFormFile File { get; set; }
        public string KayitTuru { get; set; }
        public int VehicleId { get; set; }
        public string Eslesme { get; set; }
        public bool DryRun { get; set; }
    }

    [EnableRateLimiting(PahaliUclar.RateLimitPolicy)]
    [Route("api/[controller]")]
    public class ImportController : SecureControllerBase
    {
        private readonly IImportService _importService;

        public ImportController(IImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("onizle")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Onizle([FromForm] ImportOnizleForm form)
        {
            var icerik = await IcerikAsync(form?.File);
            if (icerik == null)
                return BadRequest(new Core.Utilities.Results.ErrorDataResult<ImportOnizlemeDto>(Messages.InvalidValue));

            var result = await _importService.OnizleAsync(CurrentUserId, icerik, form.KayitTuru);
            return Sonuc(result);
        }

        [HttpPost("uygula")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Uygula([FromForm] ImportUygulaForm form)
        {
            var icerik = await IcerikAsync(form?.File);
            if (icerik == null)
                return BadRequest(new Core.Utilities.Results.ErrorDataResult<ImportSonucDto>(Messages.InvalidValue));

            Dictionary<string, int> eslesme;
            try
            {
                eslesme = string.IsNullOrWhiteSpace(form.Eslesme)
                    ? new Dictionary<string, int>()
                    : JsonSerializer.Deserialize<Dictionary<string, int>>(form.Eslesme);
            }
            catch (JsonException)
            {
                return BadRequest(new Core.Utilities.Results.ErrorDataResult<ImportSonucDto>(Messages.InvalidValue));
            }

            var result = await _importService.UygulaAsync(CurrentUserId, new ImportUygulaDto
            {
                VehicleId = form.VehicleId,
                KayitTuru = form.KayitTuru,
                Eslesme = eslesme,
                DryRun = form.DryRun,
                DosyaAdi = form.File.FileName,
                Icerik = icerik
            });

            return Sonuc(result);
        }

        private static async Task<byte[]> IcerikAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            return stream.ToArray();
        }

        private IActionResult Sonuc(Core.Utilities.Results.IResult result)
        {
            if (result.Success)
                return Ok(result);

            if (result.Message == Messages.AuthorizationDenied)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            if (result.Message == Messages.VehicleNotFound || result.Message == Messages.UserNotFound)
                return NotFound(result);

            return BadRequest(result);
        }
    }
}
