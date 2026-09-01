using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class KarnePaylasimi : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public string TokenHash { get; set; }
        public bool BakimGecmisi { get; set; }
        public bool ParcaHafizasi { get; set; }
        public bool YakitOzeti { get; set; }
        public bool Belgeler { get; set; }
        public bool PlakaGoster { get; set; }
        public bool TutarGoster { get; set; }
        public bool AcilKart { get; set; }
        public DateTime? SonKullanma { get; set; }
        public bool Aktif { get; set; }
        public int GoruntulenmeSayisi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
