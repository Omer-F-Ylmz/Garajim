using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfUstaSohbetDal : EfEntityRepositoryBase<UstaSohbet, GarajimDbContext>, IUstaSohbetDal
    {
        public EfUstaSohbetDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<UstaSohbet>> GetListeAsync(int? vehicleId, int? userId)
        {
            var sorgu = Context.UstaSohbetleri.AsNoTracking().AsQueryable();

            if (vehicleId != null)
            {
                sorgu = sorgu.Where(s => s.VehicleId == vehicleId.Value);
            }

            if (userId != null)
            {
                sorgu = sorgu.Where(s => s.UserId == userId.Value);
            }

            return await sorgu.OrderByDescending(s => s.OlusturmaTarihi).ThenByDescending(s => s.Id).ToListAsync();
        }

        public async Task<List<int>> EskiSohbetIdleriAsync(DateTime sinir)
        {
            return await Context.UstaSohbetleri
                .AsNoTracking()
                .Where(s => s.OlusturmaTarihi < sinir)
                .Select(s => s.Id)
                .ToListAsync();
        }

        public async Task SohbetleriSilAsync(List<int> sohbetIds)
        {
            if (sohbetIds == null || sohbetIds.Count == 0)
            {
                return;
            }

            await Context.UstaSohbetleri.Where(s => sohbetIds.Contains(s.Id)).ExecuteDeleteAsync();
        }
    }


    public class EfUstaMesajDal : EfEntityRepositoryBase<UstaMesaj, GarajimDbContext>, IUstaMesajDal

    {
        public EfUstaMesajDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<UstaMesaj>> GetSohbetMesajlariAsync(int sohbetId)
        {
            return await Context.UstaMesajlari
                .AsNoTracking()
                .Where(m => m.SohbetId == sohbetId)
                .OrderBy(m => m.Id)
                .ToListAsync();
        }

        public async Task<int> SohbetMesajSayisiAsync(int sohbetId)
        {
            return await Context.UstaMesajlari.CountAsync(m => m.SohbetId == sohbetId && m.Rol == UstaRol.Kullanici);
        }

        public async Task<int> KullaniciGunlukSayisiAsync(int userId, DateTime gunBasi)
        {
            return await Context.UstaMesajlari
                .Where(m => m.Rol == UstaRol.Kullanici && m.OlusturmaTarihi >= gunBasi)
                .Join(Context.UstaSohbetleri, m => m.SohbetId, s => s.Id, (m, s) => s.UserId)
                .CountAsync(u => u == userId);
        }

        public async Task<List<UstaMesaj>> GetOzetlenmemisCozumluMesajlarAsync()
        {
            return await Context.UstaMesajlari
                .AsNoTracking()
                .Where(m => m.GeriBildirim == UstaGeriBildirim.Olumlu && m.CozumBakimId != null && !m.Ozetlendi)
                .ToListAsync();
        }

        public async Task OzetlendiIsaretleAsync(List<int> mesajIds)
        {
            if (mesajIds == null || mesajIds.Count == 0)
            {
                return;
            }

            await Context.UstaMesajlari
                .Where(m => mesajIds.Contains(m.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Ozetlendi, true));
        }

        public async Task DeleteBySohbetAsync(int sohbetId)
        {
            await Context.UstaMesajlari.Where(m => m.SohbetId == sohbetId).ExecuteDeleteAsync();
        }

        public async Task DeleteBySohbetlerAsync(List<int> sohbetIds)
        {
            if (sohbetIds == null || sohbetIds.Count == 0)
            {
                return;
            }

            await Context.UstaMesajlari.Where(m => sohbetIds.Contains(m.SohbetId)).ExecuteDeleteAsync();
        }
    }

    public class EfUstaOnayDal : EfEntityRepositoryBase<UstaOnay, GarajimDbContext>, IUstaOnayDal
    {
        public EfUstaOnayDal(GarajimDbContext context) : base(context)
        {
        }
    }

    public class EfUstaCozumOzetiDal : EfEntityRepositoryBase<UstaCozumOzeti, GarajimDbContext>, IUstaCozumOzetiDal
    {
        public EfUstaCozumOzetiDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<List<UstaCozumOzeti>> GetTumuAsync()
        {
            return await Context.UstaCozumOzetleri.AsNoTracking().ToListAsync();
        }

        public async Task<UstaCozumOzeti> BulAsync(string marka, string model, string motor, string kategori, string parca)
        {
            return await Context.UstaCozumOzetleri.FirstOrDefaultAsync(o =>
                o.Marka == marka && o.Model == model && o.Motor == motor &&
                o.BelirtiKategori == kategori && o.ParcaTuru == parca);
        }
    }
}
