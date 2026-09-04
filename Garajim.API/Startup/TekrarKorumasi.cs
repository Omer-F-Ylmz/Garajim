using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Garajim.API.Startup
{
    public class TekrarKorumasi : IAsyncActionFilter
    {
        public static readonly TimeSpan Pencere = TimeSpan.FromSeconds(10);

        private readonly IMemoryCache _onbellek;

        public TekrarKorumasi(IMemoryCache onbellek)
        {
            _onbellek = onbellek;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var anahtar = Anahtar(context);

            if (anahtar != null && _onbellek.TryGetValue(anahtar, out object onceki))
            {
                context.Result = new ObjectResult(onceki) { StatusCode = StatusCodes.Status200OK };
                return;
            }

            var sonuc = await next();

            if (anahtar != null && sonuc.Result is ObjectResult nesne
                && nesne.StatusCode is null or >= 200 and < 300 && nesne.Value != null)
            {
                _onbellek.Set(anahtar, nesne.Value, Pencere);
            }
        }

        private static string Anahtar(ActionExecutingContext context)
        {
            var kullanici = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(kullanici))
            {
                return null;
            }

            string govde;

            try
            {
                govde = JsonSerializer.Serialize(context.ActionArguments);
            }
            catch (NotSupportedException)
            {
                return null;
            }

            var ham = kullanici + "|" + context.HttpContext.Request.Path + "|" + govde;
            var ozet = SHA256.HashData(Encoding.UTF8.GetBytes(ham));

            return "tekrar:" + Convert.ToHexString(ozet);
        }
    }
}
