using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
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
                return;
            }

            if (SifreDegisimindenEskiMi(context, user.SifreDegisimTarihi))
            {
                context.Fail("Şifre değişti, yeniden giriş gerekiyor.");
            }
        }

        private static bool SifreDegisimindenEskiMi(TokenValidatedContext context, DateTime? sifreDegisimTarihi)
        {
            if (sifreDegisimTarihi == null)
            {
                return false;
            }

            var uretim = UretimZamani(context);
            if (uretim == null)
            {
                return true;
            }

            var kesim = new DateTimeOffset(DateTime.SpecifyKind(sifreDegisimTarihi.Value, DateTimeKind.Utc)).ToUnixTimeSeconds();
            return uretim.Value < kesim;
        }

        private static long? UretimZamani(TokenValidatedContext context)
        {
            var iddia = context.Principal?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;

            if (long.TryParse(iddia, NumberStyles.Integer, CultureInfo.InvariantCulture, out var saniye))
            {
                return saniye;
            }

            return context.SecurityToken is JwtSecurityToken jwt
                ? new DateTimeOffset(DateTime.SpecifyKind(jwt.ValidFrom, DateTimeKind.Utc)).ToUnixTimeSeconds()
                : (long?)null;
        }
    }
}
