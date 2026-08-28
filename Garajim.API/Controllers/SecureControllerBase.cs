using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garajim.API.Controllers
{
    [ApiController]
    [Authorize]
    public abstract class SecureControllerBase : ControllerBase
    {
        protected int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        protected string CurrentRole => User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
