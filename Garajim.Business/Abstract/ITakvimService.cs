using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface ITakvimService
    {
        Task<IDataResult<TakvimAbonelikDto>> AbonelikOlusturAsync(int userId);
        Task<IResult> AbonelikKapatAsync(int userId);
        Task<IDataResult<string>> IcsAsync(string token);
    }
}
