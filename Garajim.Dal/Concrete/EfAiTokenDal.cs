using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfAiTokenDal : EfEntityRepositoryBase<AiTokenSayaci, GarajimDbContext>, IAiTokenDal
    {
        public EfAiTokenDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<AiTokenSayaci> AyiAlAsync(int yil, int ay)
        {
            return await Context.AiTokenSayaclari
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Yil == yil && s.Ay == ay);
        }

        public async Task EkleAsync(int yil, int ay, int giris, int cikis)
        {
            if (giris <= 0 && cikis <= 0)
            {
                return;
            }

            var satir = await Context.AiTokenSayaclari.SingleOrDefaultAsync(s => s.Yil == yil && s.Ay == ay);

            if (satir == null)
            {
                satir = new AiTokenSayaci { Yil = yil, Ay = ay };
                Context.AiTokenSayaclari.Add(satir);
            }

            satir.TokenGiris += giris;
            satir.TokenCikis += cikis;

            await Context.SaveChangesAsync();
        }

        public async Task<bool> BildirimiIsaretleAsync(int yil, int ay)
        {
            var satir = await Context.AiTokenSayaclari.SingleOrDefaultAsync(s => s.Yil == yil && s.Ay == ay);

            if (satir == null || satir.BildirimGonderildi)
            {
                return false;
            }

            satir.BildirimGonderildi = true;
            await Context.SaveChangesAsync();

            return true;
        }
    }
}
