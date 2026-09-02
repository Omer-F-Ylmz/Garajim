using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.AspNetCore.Authorization;
using Garajim.API.Startup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garajim.API.Controllers
{
    [EnableRateLimiting(PahaliUclar.RateLimitPolicy)]
    [Route("api/[controller]")]
    public class ReceiptsController : SecureControllerBase
    {
        private readonly IReceiptService _receiptService;

        public ReceiptsController(IReceiptService receiptService)
        {
            _receiptService = receiptService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] ReceiptDraftStatus? durum)
        {
            var result = await _receiptService.GetListAsync(CurrentUserId, durum);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("stats")]
        [Authorize(Roles = CompanyRoles.Owner)]
        public async Task<IActionResult> GetStats()
        {
            var result = await _receiptService.GetStatsAsync(CurrentUserId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _receiptService.GetByIdAsync(CurrentUserId, id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] ReceiptUploadForm form, [FromQuery] bool otoOnay = false)
        {
            var file = form?.File;
            if (file == null || file.Length == 0)
                return BadRequest(new ErrorDataResult<ReceiptUploadResultDto>(Messages.InvalidValue));

            var result = await _receiptService.UploadAsync(CurrentUserId, new ReceiptUploadDto
            {
                FileName = file.FileName,
                Content = await YuklemeOkuyucu.OkuAsync(file, HttpContext.RequestAborted)
            }, otoOnay);

            if (!result.Success)
            {
                if (result.Message == Messages.ReceiptMonthlyLimitExceeded)
                    return StatusCode(StatusCodes.Status429TooManyRequests, result);
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> Confirm(int id, ReceiptConfirmDto dto)
        {
            var result = await _receiptService.ConfirmAsync(CurrentUserId, id, dto);
            if (!result.Success)
                return BulunamadiMi(result.Message) ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _receiptService.RejectAsync(CurrentUserId, id);
            if (!result.Success)
                return BulunamadiMi(result.Message) ? NotFound(result) : BadRequest(result);
            return Ok(result);
        }

        private static bool BulunamadiMi(string message)
        {
            return message == Messages.ReceiptNotFound
                || message == Messages.VehicleNotFound
                || message == Messages.UserNotFound;
        }
    }
}
