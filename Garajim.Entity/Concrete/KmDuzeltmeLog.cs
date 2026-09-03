using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class KmDuzeltmeLog : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public int UserId { get; set; }
        public int EskiKm { get; set; }
        public int YeniKm { get; set; }
        public string Neden { get; set; }
        public DateTime Tarih { get; set; }
    }
}
