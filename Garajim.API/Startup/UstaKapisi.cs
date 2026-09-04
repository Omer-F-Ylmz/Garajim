using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Garajim.API.Startup
{
    public class UstaKapisi : IAsyncActionFilter
    {
        public const string Mesaj = "AI Usta yakında.";

        private readonly IConfiguration _configuration;

        public UstaKapisi(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static bool Acik(IConfiguration configuration)
        {
            return configuration.GetValue("Usta:Enabled", true);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!Acik(_configuration))
            {
                context.Result = new ObjectResult(new Core.Utilities.Results.ErrorResult(Mesaj))
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };

                return;
            }

            await next();
        }
    }
}
