using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IImportService
    {
        Task<IDataResult<ImportOnizlemeDto>> OnizleAsync(int userId, byte[] icerik, string kayitTuru);
        Task<IDataResult<ImportSonucDto>> UygulaAsync(int userId, ImportUygulaDto dto);
    }
}
