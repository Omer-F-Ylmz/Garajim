using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Garajim.Dal.Concrete.Context
{
    public class GarajimDbContext : DbContext
    {
        public GarajimDbContext(DbContextOptions<GarajimDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<FuelRecord> FuelRecords { get; set; }
        public DbSet<ExpenseRecord> ExpenseRecords { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
                entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.Property(v => v.Plate).HasMaxLength(20).IsRequired();
                entity.Property(v => v.Brand).HasMaxLength(100).IsRequired();
                entity.Property(v => v.Model).HasMaxLength(100).IsRequired();
                entity.HasIndex(v => new { v.UserId, v.Plate }).IsUnique();
                entity.HasOne<AppUser>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MaintenanceRecord>(entity =>
            {
                entity.Property(m => m.Cost).HasPrecision(18, 2);
                entity.Property(m => m.ServiceName).HasMaxLength(150);
                entity.Property(m => m.Note).HasMaxLength(500);
                entity.HasIndex(m => new { m.VehicleId, m.Date });
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(m => m.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FuelRecord>(entity =>
            {
                entity.Property(f => f.Liters).HasPrecision(9, 2);
                entity.Property(f => f.TotalCost).HasPrecision(18, 2);
                entity.HasIndex(f => new { f.VehicleId, f.Date });
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(f => f.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExpenseRecord>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.HasIndex(e => new { e.VehicleId, e.Date });
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(e => e.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.Property(r => r.Note).HasMaxLength(500);
                entity.HasIndex(r => new { r.VehicleId, r.IsCompleted, r.DueDate });
                entity.HasOne<Vehicle>().WithMany().HasForeignKey(r => r.VehicleId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
