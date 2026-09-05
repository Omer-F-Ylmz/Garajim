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
        private readonly IWebHostEnvironment _ortam;

        public YonetimController(IYonetimService yonetimService, DemoSifirlamaJob demoJob, IWebHostEnvironment ortam)
        {
            _yonetimService = yonetimService;
            _demoJob = demoJob;
            _ortam = ortam;
        }

        [HttpGet("ozet")]
        public async Task<IActionResult> Ozet()
        {
            return Ok(await _yonetimService.OzetAsync(BellekDurumu.Oku(), RehberSayfaSayisi()));
        }

        private int RehberSayfaSayisi()
        {
            var klasor = Path.Combine(_ortam.WebRootPath ?? string.Empty, "rehber");

            return Directory.Exists(klasor)
                ? Directory.GetFiles(klasor, "*.html", SearchOption.AllDirectories).Length
                : 0;
        }

        [HttpPost("demo-sifirla")]
        public async Task<IActionResult> DemoSifirla()
        {
            await _demoJob.RunAsync();
            return Ok(new SuccessResult("Demo verisi yeniden kuruldu."));
        }
    }
}
