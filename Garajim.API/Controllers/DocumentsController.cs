using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    public class DocumentsController : SecureControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentsController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int? vehicleId, [FromQuery] int? maintenanceRecordId)
        {
            var result = await _documentService.GetListAsync(CurrentUserId, vehicleId, maintenanceRecordId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] int? vehicleId, [FromForm] int? maintenanceRecordId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new Core.Utilities.Results.ErrorDataResult<DocumentDto>(Messages.InvalidValue));

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            var result = await _documentService.UploadAsync(CurrentUserId, new DocumentUploadDto
            {
                VehicleId = vehicleId,
                MaintenanceRecordId = maintenanceRecordId,
                FileName = file.FileName,
                Content = stream.ToArray()
            });

            if (!result.Success)
                return result.Message == Messages.VehicleNotFound ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var result = await _documentService.DownloadAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return File(result.Data.Content, result.Data.ContentType, result.Data.OriginalName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _documentService.DeleteAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }
    }
}
