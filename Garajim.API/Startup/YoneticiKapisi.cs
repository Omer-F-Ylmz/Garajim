using System.Security.Claims;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Garajim.API.Startup
{
    public class YoneticiKapisi : IAsyncActionFilter
    {
        private readonly IConfiguration _configuration;
        private readonly IUserDal _userDal;

        public YoneticiKapisi(IConfiguration configuration, IUserDal userDal)
        {
            _configuration = configuration;
            _userDal = userDal;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!await YoneticiMiAsync(context))
            {
                context.Result = new ObjectResult(new ErrorResult(Messages.AuthorizationDenied))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }

        private async Task<bool> YoneticiMiAsync(ActionExecutingContext context)
        {
            var tanimli = (_configuration["App:YoneticiEposta"] ?? string.Empty).Trim();
            if (tanimli.Length == 0)
                return false;

            var kimlik = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(kimlik, out var userId))
                return false;

            var user = await _userDal.GetForAuthenticationByIdAsync(userId);

            return user != null && string.Equals(tanimli, user.Email, StringComparison.OrdinalIgnoreCase);
        }
    }
}
