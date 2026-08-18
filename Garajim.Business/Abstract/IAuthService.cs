using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IAuthService
    {
        Task<IDataResult<TokenDto>> RegisterAsync(RegisterDto dto);
        Task<IDataResult<TokenDto>> LoginAsync(LoginDto dto);
    }
}
