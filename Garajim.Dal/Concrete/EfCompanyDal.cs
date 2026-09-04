using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfCompanyDal : EfEntityRepositoryBase<Company, GarajimDbContext>, ICompanyDal
    {
        public EfCompanyDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<Company> GetByDavetKoduAsync(string kod)
        {
            return await Context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.DavetKodu == kod);
        }

        public async Task SirketVerisiniSilAsync(int companyId)
        {
            await KayitlariSilAsync(companyId);
            await Context.Users.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.Companies.Where(c => c.Id == companyId).ExecuteDeleteAsync();
        }

        public Task SirketKayitlariniSilAsync(int companyId)
        {
            return KayitlariSilAsync(companyId);
        }

        private async Task KayitlariSilAsync(int companyId)
        {
            await Context.Companies
                .Where(c => c.DavetEdenCompanyId == companyId)
                .ExecuteUpdateAsync(k => k.SetProperty(c => c.DavetEdenCompanyId, (int?)null));

            await Context.HasarFotograflari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.HasarDosyalari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.AracDegerleri.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.UstaMesajlari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.UstaSohbetleri.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.UstaOnaylari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.KarnePaylasimlari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.TakvimAbonelikleri.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.ImportKayitlari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.ReceiptDrafts.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.MaintenanceParts.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.Documents.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.KmDuzeltmeLoglari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.YolculukKayitlari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.LastikSetleri.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.EvrakKayitlari.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.VehicleAssignments.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.Reminders.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.ExpenseRecords.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.FuelRecords.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.MaintenanceRecords.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
            await Context.Vehicles.Where(x => x.CompanyId == companyId).ExecuteDeleteAsync();
        }

        public async Task<bool> DavetKoduVarMiAsync(string kod)
        {
            return await Context.Companies
                .IgnoreQueryFilters()
                .AnyAsync(c => c.DavetKodu == kod);
        }

        public async Task<int> DavetSayisiAsync(int companyId)
        {
            return await DogrulanmisDavetliler(companyId).CountAsync();
        }

        public async Task<List<Company>> GetDavetlilerAsync(int companyId)
        {
            return await DogrulanmisDavetliler(companyId)
                .AsNoTracking()
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        private IQueryable<Company> DogrulanmisDavetliler(int companyId)
        {
            return Context.Companies
                .IgnoreQueryFilters()
                .Where(c => c.DavetEdenCompanyId == companyId
                            && Context.Users.IgnoreQueryFilters()
                                .Any(u => u.CompanyId == c.Id && u.EmailDogrulandi));
        }
    }
}
