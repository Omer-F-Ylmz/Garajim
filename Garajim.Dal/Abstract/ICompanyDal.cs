using Garajim.Core.DataAccess;
using Garajim.Entity.Concrete;

namespace Garajim.Dal.Abstract
{
    public interface ICompanyDal : IEntityRepository<Company>
    {
        Task<Company> GetByDavetKoduAsync(string kod);
        Task<bool> DavetKoduVarMiAsync(string kod);
        Task SirketVerisiniSilAsync(int companyId);
        Task<List<Company>> GetDavetlilerAsync(int companyId);
        Task<int> DavetSayisiAsync(int companyId);
    }
}
