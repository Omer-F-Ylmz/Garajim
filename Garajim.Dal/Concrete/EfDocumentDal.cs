using Garajim.Core.DataAccess.EntityFramework;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Dal.Sorgular;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete
{
    public class EfDocumentDal : EfEntityRepositoryBase<Document, GarajimDbContext>, IDocumentDal
    {
        public EfDocumentDal(GarajimDbContext context) : base(context)
        {
        }

        public async Task<long> GetCompanyTotalSizeAsync()
        {
            return await Context.Documents.SumAsync(d => (long?)d.SizeBytes) ?? 0;
        }
        public Task<SayfaliSonuc<Document>> SayfaAsync(int? vehicleId, int? maintenanceRecordId, ListeSorgusu sorgu, SiralamaSonucu siralama)
        {
            var sorgulama = Context.Documents.AsNoTracking().AsQueryable();

            if (vehicleId != null)
            {
                sorgulama = sorgulama.Where(d => d.VehicleId == vehicleId);
            }

            if (maintenanceRecordId != null)
            {
                sorgulama = sorgulama.Where(d => d.MaintenanceRecordId == maintenanceRecordId);
            }

            var metin = TurkceArama.Iceren<Document>(sorgu.GecerliQ(), d => d.OriginalName);

            return Sayfalayici.UygulaAsync(sorgulama, sorgu, siralama, d => d.CreatedAt, metin, Sirala);
        }

        private static IQueryable<Document> Sirala(IQueryable<Document> sorgulama, SiralamaSonucu siralama)
        {
            return siralama.Alan switch
            {
                "ad" => siralama.Artan
                    ? sorgulama.OrderBy(d => d.OriginalName).ThenBy(d => d.Id)
                    : sorgulama.OrderByDescending(d => d.OriginalName).ThenByDescending(d => d.Id),
                "boyut" => siralama.Artan
                    ? sorgulama.OrderBy(d => d.SizeBytes).ThenBy(d => d.Id)
                    : sorgulama.OrderByDescending(d => d.SizeBytes).ThenByDescending(d => d.Id),
                _ => siralama.Artan
                    ? sorgulama.OrderBy(d => d.CreatedAt).ThenBy(d => d.Id)
                    : sorgulama.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)
            };
        }
        public Task<List<Document>> KarneBelgeleriAsync(int vehicleId, int limit)
        {
            return Context.Documents
                .AsNoTracking()
                .Where(d => d.VehicleId == vehicleId)
                .Where(d => !Context.HasarFotograflari.Any(f => f.DocumentId == d.Id))
                .OrderByDescending(d => d.CreatedAt)
                .ThenByDescending(d => d.Id)
                .Take(limit)
                .ToListAsync();
        }
    }
}


