using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class ExportController : SecureControllerBase
    {
        private readonly IExportService _exportService;

        public ExportController(IExportService exportService)
        {
            _exportService = exportService;
        }

        [HttpGet("{tur}.csv")]
        public async Task<IActionResult> Csv(string tur, [FromQuery] int? vehicleId, [FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis)
        {
            var result = await _exportService.CsvAsync(CurrentUserId, tur, vehicleId, baslangic, bitis);
            if (!result.Success)
            {
                if (result.Message == Messages.ExportTuruBulunamadi || result.Message == Messages.VehicleNotFound)
                    return NotFound(result);
                return BadRequest(result);
            }

            return File(result.Data.Icerik, "text/csv", result.Data.DosyaAdi);
        }
    }
}
