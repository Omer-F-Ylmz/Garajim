using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IYonetimService
    {
        Task<IDataResult<YonetimOzetDto>> OzetAsync(object bellek, int rehberSayfaSayisi);
    }
}
