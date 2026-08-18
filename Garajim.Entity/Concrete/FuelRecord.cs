using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class FuelRecord : IEntity
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Liters { get; set; }
        public decimal TotalCost { get; set; }
        public int Km { get; set; }
    }
}
