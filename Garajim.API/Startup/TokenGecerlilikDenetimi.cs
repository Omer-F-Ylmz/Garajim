using System.Security.Claims;
using Garajim.Dal.Abstract;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Garajim.API.Startup
{
    public static class TokenGecerlilikDenetimi
    {
        public static async Task DenetleAsync(TokenValidatedContext context)
        {
            var kimlik = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(kimlik, out var userId) || userId <= 0)
            {
                context.Fail("Token kullanıcı kimliği taşımıyor.");
                return;
            }

            var userDal = context.HttpContext.RequestServices.GetRequiredService<IUserDal>();
            var user = await userDal.GetForAuthenticationByIdAsync(userId);

            if (user == null || !user.IsActive || !user.EmailDogrulandi)
            {
                context.Fail("Hesap kapalı ya da doğrulanmamış.");
                return;
            }

            var tokendekiRol = context.Principal.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.Equals(tokendekiRol, user.Role.ToString(), StringComparison.Ordinal))
            {
                context.Fail("Rol değişti, yeniden giriş gerekiyor.");
            }
        }
    }
}
