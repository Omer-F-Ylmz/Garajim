using Garajim.API.Startup;
using Garajim.Business.Abstract;
using Garajim.Business.Jobs;
using Garajim.Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [Route("api/[controller]")]
    [ServiceFilter(typeof(YoneticiKapisi))]
    public class YonetimController : SecureControllerBase
    {
        private readonly IYonetimService _yonetimService;
        private readonly DemoSifirlamaJob _demoJob;

        public YonetimController(IYonetimService yonetimService, DemoSifirlamaJob demoJob)
        {
            _yonetimService = yonetimService;
            _demoJob = demoJob;
        }

        [HttpGet("ozet")]
        public async Task<IActionResult> Ozet()
        {
            return Ok(await _yonetimService.OzetAsync(BellekDurumu.Oku()));
        }

        [HttpPost("demo-sifirla")]
        public async Task<IActionResult> DemoSifirla()
        {
            await _demoJob.RunAsync();
            return Ok(new SuccessResult("Demo verisi yeniden kuruldu."));
        }
    }
}
