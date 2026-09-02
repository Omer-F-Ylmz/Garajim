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
        public DbSet<EvrakKaydi> EvrakKayitlari { get; set; }
        public DbSet<TakvimAbonelik> TakvimAbonelikleri { get; set; }
        public DbSet<ImportKaydi> ImportKayitlari { get; set; }
        public DbSet<YolculukKaydi> YolculukKayitlari { get; set; }
        public DbSet<LastikSeti> LastikSetleri { get; set; }
        public DbSet<UstaSohbet> UstaSohbetleri { get; set; }
        public DbSet<UstaMesaj> UstaMesajlari { get; set; }
        public DbSet<UstaOnay> UstaOnaylari { get; set; }
        public DbSet<UstaCozumOzeti> UstaCozumOzetleri { get; set; }
        public DbSet<HasarDosyasi> HasarDosyalari { get; set; }
        public DbSet<HasarFoto> HasarFotograflari { get; set; }
        public DbSet<AracDeger> AracDegerleri { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(c => c.Name).HasMaxLength(150).IsRequired();
                entity.Property(c => c.DavetKodu).HasMaxLength(12);
                entity.HasIndex(c => c.DavetKodu).IsUnique().HasFilter("[DavetKodu] IS NOT NULL");
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
                entity.Property(v => v.Motor).HasMaxLength(AracAlanUzunluklari.Motor);
                entity.Property(v => v.Vites).HasMaxLength(AracAlanUzunluklari.Vites);
                entity.Property(v => v.Plate).HasMaxLength(AracAlanUzunluklari.Plaka).IsRequired();
                entity.Property(v => v.Brand).HasMaxLength(AracAlanUzunluklari.Marka).IsRequired();
                entity.Property(v => v.Model).HasMaxLength(AracAlanUzunluklari.Model).IsRequired();
                entity.Property(v => v.AcilKisiAd).HasMaxLength(AracAlanUzunluklari.AcilKisiAd);
                entity.Property(v => v.AcilKisiTelefon).HasMaxLength(AracAlanUzunluklari.AcilKisiTelefon);
                entity.Property(v => v.AcilNot).HasMaxLength(AracAlanUzunluklari.AcilNot);
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
                entity.Property(f => f.Kwh).HasPrecision(9, 2);
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

            modelBuilder.Entity<ImportKaydi>(entity =>
            {
                entity.Property(i => i.SatirHash).HasMaxLength(64).IsRequired();
                entity.Property(i => i.KayitTuru).HasMaxLength(20).IsRequired();
                entity.HasIndex(i => new { i.VehicleId, i.SatirHash }).IsUnique();
                entity.HasIndex(i => i.CompanyId);
                entity.HasOne<Company>().WithMany().HasForeignKey(i => i.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(i => i.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<YolculukKaydi>(entity =>
            {
                entity.Property(y => y.Nereden).HasMaxLength(150);
                entity.Property(y => y.Nereye).HasMaxLength(150);
                entity.Property(y => y.Not).HasMaxLength(500);
                entity.HasIndex(y => y.CompanyId);
                entity.HasIndex(y => new { y.VehicleId, y.Tarih });
                entity.HasOne<Company>().WithMany().HasForeignKey(y => y.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(y => y.VehicleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(y => y.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("CK_YolculukKaydi_Mesafe", "[BitisKm] > [BaslangicKm] AND [MesafeKm] = [BitisKm] - [BaslangicKm]"));
            });

            modelBuilder.Entity<LastikSeti>(entity =>
            {
                entity.Property(l => l.Ad).HasMaxLength(100).IsRequired();
                entity.Property(l => l.Marka).HasMaxLength(100);
                entity.Property(l => l.Ebat).HasMaxLength(50);
                entity.Property(l => l.DisDerinligiMm).HasPrecision(4, 1);
                entity.HasIndex(l => l.CompanyId);
                entity.HasIndex(l => new { l.VehicleId, l.Takili });
                entity.HasIndex(l => l.VehicleId).IsUnique().HasFilter("[Takili] = 1");
                entity.HasOne<Company>().WithMany().HasForeignKey(l => l.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(l => l.VehicleId).OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t => t.HasCheckConstraint("CK_LastikSeti_Mesafe",
                    "([SokulmeKm] IS NULL AND [Takili] = 1 AND [ToplamKm] = 0) OR ([SokulmeKm] IS NOT NULL AND [Takili] = 0 AND [SokulmeKm] >= [TakilmaKm] AND [ToplamKm] = [SokulmeKm] - [TakilmaKm])"));
            });

            modelBuilder.Entity<UstaSohbet>(entity =>
            {
                entity.Property(s => s.Baslik).HasMaxLength(120).IsRequired();
                entity.HasIndex(s => s.CompanyId);
                entity.HasIndex(s => new { s.VehicleId, s.OlusturmaTarihi });
                entity.HasOne<Company>().WithMany().HasForeignKey(s => s.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UstaMesaj>(entity =>
            {
                entity.Property(m => m.Metin).HasMaxLength(4000).IsRequired();
                entity.Property(m => m.BilgiKategorisi).HasMaxLength(60);
                entity.HasIndex(m => m.Ozetlendi);
                entity.HasIndex(m => m.CompanyId);
                entity.HasIndex(m => new { m.SohbetId, m.Id });
                entity.HasOne<Company>().WithMany().HasForeignKey(m => m.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<UstaSohbet>().WithMany().HasForeignKey(m => m.SohbetId).OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t => t.HasCheckConstraint("CK_UstaMesaj_Token", "[TokenGiris] >= 0 AND [TokenCikis] >= 0 AND [SureMs] >= 0"));
            });

            modelBuilder.Entity<UstaOnay>(entity =>
            {
                entity.Property(o => o.MetinSurumu).HasMaxLength(20).IsRequired();
                entity.HasIndex(o => o.CompanyId);
                entity.HasIndex(o => new { o.UserId, o.MetinSurumu }).IsUnique();
                entity.HasOne<Company>().WithMany().HasForeignKey(o => o.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UstaCozumOzeti>(entity =>
            {
                entity.Property(o => o.Marka).HasMaxLength(60).IsRequired();
                entity.Property(o => o.Model).HasMaxLength(60).IsRequired();
                entity.Property(o => o.Motor).HasMaxLength(40);
                entity.Property(o => o.BelirtiKategori).HasMaxLength(60).IsRequired();
                entity.Property(o => o.ParcaTuru).HasMaxLength(60).IsRequired();
                entity.HasIndex(o => new { o.Marka, o.Model, o.BelirtiKategori, o.ParcaTuru });
                entity.ToTable(t => t.HasCheckConstraint("CK_UstaCozumOzeti_Sayi", "[Sayi] > 0"));
            });

            modelBuilder.Entity<HasarDosyasi>(entity =>
            {
                entity.Property(h => h.Konum).HasMaxLength(200);
                entity.Property(h => h.Aciklama).HasMaxLength(1000).IsRequired();
                entity.Property(h => h.KarsiTarafPlaka).HasMaxLength(15);
                entity.Property(h => h.KarsiTarafSigorta).HasMaxLength(100);
                entity.Property(h => h.KarsiTarafPoliceNo).HasMaxLength(50);
                entity.Property(h => h.SigortaDosyaNo).HasMaxLength(50);
                entity.Property(h => h.HasarBedeli).HasPrecision(18, 2);
                entity.HasIndex(h => h.CompanyId);
                entity.HasIndex(h => new { h.VehicleId, h.OlayTarihi });
                entity.HasOne<Company>().WithMany().HasForeignKey(h => h.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(h => h.VehicleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(h => h.OlusturanUserId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("CK_HasarDosyasi_Bedel", "[HasarBedeli] IS NULL OR [HasarBedeli] >= 0"));
            });

            modelBuilder.Entity<HasarFoto>(entity =>
            {
                entity.HasIndex(f => f.CompanyId);
                entity.HasIndex(f => new { f.HasarDosyasiId, f.Sira });
                entity.HasIndex(f => f.DocumentId).IsUnique();
                entity.HasOne<Company>().WithMany().HasForeignKey(f => f.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<HasarDosyasi>().WithMany().HasForeignKey(f => f.HasarDosyasiId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Document>().WithMany().HasForeignKey(f => f.DocumentId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("CK_HasarFoto_Sira", "[Sira] > 0"));
            });

            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.Property(u => u.DogrulamaKodHash).HasMaxLength(64);
            });

            modelBuilder.Entity<AracDeger>(entity =>
            {
                entity.Property(d => d.Deger).HasPrecision(18, 2);
                entity.Property(d => d.Not).HasMaxLength(200);
                entity.HasIndex(d => d.CompanyId);
                entity.HasIndex(d => new { d.VehicleId, d.Tarih });
                entity.HasOne<Company>().WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(d => d.VehicleId).OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t => t.HasCheckConstraint("CK_AracDeger_Deger", "[Deger] > 0"));
            });

            modelBuilder.Entity<TakvimAbonelik>(entity =>

            {
                entity.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
                entity.HasIndex(t => t.TokenHash).IsUnique();
                entity.HasIndex(t => new { t.UserId, t.Aktif });
                entity.HasOne<Company>().WithMany().HasForeignKey(t => t.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EvrakKaydi>(entity =>
            {
                entity.Property(e => e.Saglayici).HasMaxLength(100);
                entity.Property(e => e.PoliceNo).HasMaxLength(50);
                entity.Property(e => e.Not).HasMaxLength(300);
                entity.HasIndex(e => new { e.CompanyId, e.BitisTarihi });
                entity.HasIndex(e => new { e.VehicleId, e.Aktif });
                entity.HasIndex(e => new { e.UserId, e.Aktif });
                entity.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("CK_EvrakKaydi_TekSahip",
                    "([VehicleId] IS NOT NULL AND [UserId] IS NULL) OR ([VehicleId] IS NULL AND [UserId] IS NOT NULL)"));
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
            modelBuilder.Entity<EvrakKaydi>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<TakvimAbonelik>().HasQueryFilter(t => t.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<ImportKaydi>().HasQueryFilter(i => i.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<YolculukKaydi>().HasQueryFilter(y => y.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<LastikSeti>().HasQueryFilter(l => l.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<UstaSohbet>().HasQueryFilter(s => s.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<UstaMesaj>().HasQueryFilter(m => m.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<UstaOnay>().HasQueryFilter(o => o.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<HasarDosyasi>().HasQueryFilter(h => h.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<HasarFoto>().HasQueryFilter(f => f.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<AracDeger>().HasQueryFilter(d => d.CompanyId == CurrentCompanyId);
        }
    }
}
