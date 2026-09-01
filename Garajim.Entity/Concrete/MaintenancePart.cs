using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class MaintenancePart : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int MaintenanceRecordId { get; set; }
        public int VehicleId { get; set; }
        public ParcaTuru ParcaTuru { get; set; }
        public string Aciklama { get; set; }
        public int Adet { get; set; }
        public decimal? Tutar { get; set; }
        public string Marka { get; set; }
    }
}
