using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class ImportKaydi : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public string SatirHash { get; set; }
        public string KayitTuru { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
