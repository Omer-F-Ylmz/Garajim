using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class VehiclesController : SecureControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly IPartMemoryService _partMemoryService;
        private readonly IKarneService _karneService;
        private readonly IEvrakService _evrakService;
        private readonly IReportService _reportService;
        private readonly IHasarService _hasarService;
        private readonly IDegerService _degerService;

        public VehiclesController(IVehicleService vehicleService, IPartMemoryService partMemoryService, IKarneService karneService, IEvrakService evrakService, IReportService reportService, IHasarService hasarService, IDegerService degerService)
        {
            _vehicleService = vehicleService;
            _partMemoryService = partMemoryService;
            _karneService = karneService;
            _evrakService = evrakService;
            _reportService = reportService;
            _hasarService = hasarService;
            _degerService = degerService;
        }

        [HttpPost("{id}/karne")]
        [Authorize(Roles = CompanyRoles.OwnerOrManager)]
        public async Task<IActionResult> KarneOlustur(int id, KarneOlusturDto dto)
        {
            var result = await _karneService.OlusturAsync(CurrentUserId, id, dto);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}/karne")]
        [Authorize(Roles = CompanyRoles.OwnerOrManager)]
        public async Task<IActionResult> KarneKapat(int id)
        {
            var result = await _karneService.KapatAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("karne-stats")]
        [Authorize(Roles = CompanyRoles.Owner)]
        public async Task<IActionResult> KarneStats()
        {
            var result = await _karneService.StatsAsync(CurrentUserId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("{id}/maliyet")]
        public async Task<IActionResult> Maliyet(int id, [FromQuery] DateTime baslangic, [FromQuery] DateTime bitis)
        {
            var result = await _reportService.GetAracMaliyetAsync(CurrentUserId, id, baslangic, bitis);
            if (!result.Success)
            {
                if (result.Message == Garajim.Business.Constants.Messages.VehicleNotFound)
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet("{id}/evrak")]
        public async Task<IActionResult> AracEvraklari(int id)
        {
            var result = await _evrakService.GetListAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("{id}/hasar")]
        public async Task<IActionResult> AracHasarlari(int id)
        {
            var result = await _hasarService.GetListAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPut("{id}/kasa-tipi")]
        public async Task<IActionResult> KasaTipiSec(int id, [FromBody] KasaTipi kasaTipi)
        {
            var result = await _vehicleService.KasaTipiSecAsync(CurrentUserId, id, kasaTipi);
            if (!result.Success)
                return result.Message == Messages.VehicleNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{id}/deger")]
        public async Task<IActionResult> DegerSerisi(int id)
        {
            var result = await _degerService.GetSeriAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost("{id}/deger")]
        public async Task<IActionResult> DegerGir(int id, DegerGirDto dto)
        {
            var result = await _degerService.GirAsync(CurrentUserId, id, dto);
            if (!result.Success)
                return result.Message == Messages.VehicleNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/deger/tahmin")]
        public async Task<IActionResult> DegerTahmini(int id)
        {
            var result = await _degerService.TahminAsync(CurrentUserId, id);
            if (!result.Success)
            {
                if (result.Message == Messages.VehicleNotFound)
                    return NotFound(result);
                if (result.Message == Messages.DegerModelKapsamDisi || result.Message == Messages.DegerKasaTipiGerekli
                    || result.Message == Messages.DegerVitesGerekli)
                    return StatusCode(StatusCodes.Status422UnprocessableEntity, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet("{id}/parca-hafizasi")]
        public async Task<IActionResult> ParcaHafizasi(int id)
        {
            var result = await _partMemoryService.GetAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost("{id}/parca-hafizasi/{parcaTuru}/hatirlatma")]
        public async Task<IActionResult> ParcaHatirlatmasi(int id, ParcaTuru parcaTuru)
        {
            var result = await _partMemoryService.CreateReminderAsync(CurrentUserId, id, parcaTuru);
            if (!result.Success)
                return result.Message == Messages.VehicleNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _vehicleService.GetAllAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _vehicleService.GetByIdAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = CompanyRoles.OwnerOrManager)]
        public async Task<IActionResult> Add(VehicleCreateDto dto)
        {
            var result = await _vehicleService.AddAsync(CurrentUserId, dto);
            if (!result.Success)
            {
                if (result.Message == Messages.AracLimitiAsildi)
                    return StatusCode(StatusCodes.Status402PaymentRequired, result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = CompanyRoles.OwnerOrManager)]
        public async Task<IActionResult> Update(int id, VehicleUpdateDto dto)
        {
            var result = await _vehicleService.UpdateAsync(CurrentUserId, id, dto);
            if (!result.Success)
                return result.Message == Messages.VehicleNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = CompanyRoles.OwnerOrManager)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _vehicleService.DeleteAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }
    }
}
