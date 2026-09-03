using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class FuelRecord : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Liters { get; set; }
        public decimal TotalCost { get; set; }
        public int Km { get; set; }
        public decimal? Kwh { get; set; }
        public SarjTuru? SarjTuru { get; set; }
        public bool TamDolum { get; set; } = true;
        public bool SupheliKm { get; set; }
    }
}
