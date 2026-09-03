using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IAuthService
    {
        Task<IDataResult<KayitSonucuDto>> RegisterAsync(RegisterDto dto);
        Task<IDataResult<TokenDto>> LoginAsync(LoginDto dto);
        Task<IDataResult<TokenDto>> DogrulaAsync(DogrulaDto dto);
        Task<IResult> KodGonderAsync(KodGonderDto dto);
        Task<IResult> SifreSifirlamaKoduAsync(SifreSifirlamaKodDto dto);
        Task<IResult> SifreSifirlaAsync(SifreSifirlaDto dto);
    }
}
