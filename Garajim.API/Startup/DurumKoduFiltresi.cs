using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Garajim.API.Startup
{
    public class DurumKoduFiltresi : IActionFilter
    {
        private static readonly Dictionary<string, int> Eslemeler = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [Messages.AracArsivli] = StatusCodes.Status409Conflict
        };

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is not ObjectResult sonuc || sonuc.Value is not Garajim.Core.Utilities.Results.IResult govde || govde.Success)
            {
                return;
            }

            if (govde.Message != null && Eslemeler.TryGetValue(govde.Message, out var kod))
            {
                sonuc.StatusCode = kod;
            }
        }
    }
}
