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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] DocumentUploadForm form)
        {
            var file = form?.File;
            if (file == null || file.Length == 0)
                return BadRequest(new Core.Utilities.Results.ErrorDataResult<DocumentDto>(Messages.InvalidValue));

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            var result = await _documentService.UploadAsync(CurrentUserId, new DocumentUploadDto
            {
                VehicleId = form.VehicleId,
                MaintenanceRecordId = form.MaintenanceRecordId,
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
