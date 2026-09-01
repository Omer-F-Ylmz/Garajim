using Garajim.Core.Multitenancy;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete.Context
{
    public class GarajimDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public GarajimDbContext(DbContextOptions<GarajimDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public int? CurrentCompanyId => _tenantProvider?.CompanyId;

        public DbSet<Company> Companies { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<FuelRecord> FuelRecords { get; set; }
        public DbSet<ExpenseRecord> ExpenseRecords { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<VehicleAssignment> VehicleAssignments { get; set; }
        public DbSet<ReceiptDraft> ReceiptDrafts { get; set; }
        public DbSet<MaintenancePart> MaintenanceParts { get; set; }
        public DbSet<KarnePaylasimi> KarnePaylasimlari { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(c => c.Name).HasMaxLength(150).IsRequired();
            });

            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
                entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.CompanyId);
                entity.HasOne<Company>().WithMany().HasForeignKey(u => u.CompanyId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.Property(v => v.Plate).HasMaxLength(20).IsRequired();
                entity.Property(v => v.Brand).HasMaxLength(100).IsRequired();
                entity.Property(v => v.Model).HasMaxLength(100).IsRequired();
                entity.HasIndex(v => new { v.CompanyId, v.Plate }).IsUnique();
                entity.HasIndex(v => v.CompanyId);
                entity.HasOne<Company>().WithMany().HasForeignKey(v => v.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MaintenanceRecord>(entity =>
            {
                entity.Property(m => m.Cost).HasPrecision(18, 2);
                entity.Property(m => m.ServiceName).HasMaxLength(150);
                entity.Property(m => m.Note).HasMaxLength(500);
                entity.HasIndex(m => new { m.VehicleId, m.Date });
                entity.HasIndex(m => m.CompanyId);
                entity.HasOne<Company>().WithMany().HasForeignKey(m => m.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(m => m.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FuelRecord>(entity =>
            {
                entity.Property(f => f.Liters).HasPrecision(9, 2);
                entity.Property(f => f.TotalCost).HasPrecision(18, 2);
                entity.HasIndex(f => new { f.VehicleId, f.Date });
                entity.HasIndex(f => f.CompanyId);
                entity.HasOne<Company>().WithMany().HasForeignKey(f => f.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(f => f.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExpenseRecord>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.HasIndex(e => new { e.VehicleId, e.Date });
                entity.HasIndex(e => e.CompanyId);
                entity.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.Property(r => r.Note).HasMaxLength(500);
                entity.HasIndex(r => new { r.VehicleId, r.IsCompleted, r.DueDate });
                entity.HasIndex(r => r.CompanyId);
                entity.HasOne<Company>().WithMany().HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(r => r.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.Property(d => d.OriginalName).HasMaxLength(260).IsRequired();
                entity.Property(d => d.StoredName).HasMaxLength(100).IsRequired();
                entity.Property(d => d.ContentType).HasMaxLength(120).IsRequired();
                entity.HasIndex(d => d.CompanyId);
                entity.HasIndex(d => d.StoredName).IsUnique();
                entity.HasOne<Company>().WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VehicleAssignment>(entity =>
            {
                entity.HasIndex(a => a.CompanyId);
                entity.HasIndex(a => new { a.VehicleId, a.EndDate });
                entity.HasIndex(a => a.VehicleId).IsUnique().HasFilter("[EndDate] IS NULL");
                entity.HasIndex(a => new { a.UserId, a.EndDate });
                entity.HasOne<Company>().WithMany().HasForeignKey(a => a.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(a => a.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<KarnePaylasimi>(entity =>
            {
                entity.Property(k => k.TokenHash).HasMaxLength(64).IsRequired();
                entity.HasIndex(k => k.TokenHash).IsUnique();
                entity.HasIndex(k => k.CompanyId);
                entity.HasIndex(k => new { k.VehicleId, k.Aktif });
                entity.HasOne<Company>().WithMany().HasForeignKey(k => k.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(k => k.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MaintenancePart>(entity =>
            {
                entity.Property(p => p.Aciklama).HasMaxLength(200);
                entity.Property(p => p.Marka).HasMaxLength(100);
                entity.Property(p => p.Tutar).HasPrecision(18, 2);
                entity.HasIndex(p => p.CompanyId);
                entity.HasIndex(p => new { p.VehicleId, p.ParcaTuru });
                entity.HasOne<Company>().WithMany().HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<MaintenanceRecord>().WithMany().HasForeignKey(p => p.MaintenanceRecordId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReceiptDraft>(entity =>
            {
                entity.Property(r => r.DosyaYolu).HasMaxLength(200).IsRequired();
                entity.Property(r => r.OrijinalAd).HasMaxLength(260).IsRequired();
                entity.Property(r => r.IcerikTipi).HasMaxLength(120).IsRequired();
                entity.Property(r => r.Plaka).HasMaxLength(20);
                entity.Property(r => r.DuzeltilenAlanlar).HasMaxLength(400);
                entity.Property(r => r.Saglayici).HasMaxLength(40);
                entity.Property(r => r.AtlamaNedeni).HasMaxLength(200);
                entity.Property(r => r.ParcalarJson).HasMaxLength(4000);
                entity.Property(r => r.ToplamTutar).HasPrecision(18, 2);
                entity.Property(r => r.KdvTutari).HasPrecision(18, 2);
                entity.Property(r => r.Litre).HasPrecision(9, 2);
                entity.Property(r => r.BirimFiyat).HasPrecision(9, 2);
                entity.HasIndex(r => r.CompanyId);
                entity.HasIndex(r => new { r.CompanyId, r.Durum });
                entity.HasIndex(r => r.OlusturmaTarihi);
                entity.HasOne<Company>().WithMany().HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(r => r.VehicleId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AppUser>().HasQueryFilter(u => u.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<Vehicle>().HasQueryFilter(v => v.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<MaintenanceRecord>().HasQueryFilter(m => m.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<FuelRecord>().HasQueryFilter(f => f.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<ExpenseRecord>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<Reminder>().HasQueryFilter(r => r.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<Document>().HasQueryFilter(d => d.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<VehicleAssignment>().HasQueryFilter(a => a.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<ReceiptDraft>().HasQueryFilter(r => r.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<MaintenancePart>().HasQueryFilter(p => p.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<KarnePaylasimi>().HasQueryFilter(k => k.CompanyId == CurrentCompanyId);
        }
    }
}
