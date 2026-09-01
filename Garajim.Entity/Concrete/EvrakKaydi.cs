using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class EvrakKaydi : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int? VehicleId { get; set; }
        public int? UserId { get; set; }
        public EvrakTuru EvrakTuru { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public string Saglayici { get; set; }
        public string PoliceNo { get; set; }
        public string Not { get; set; }
        public int? DocumentId { get; set; }
        public bool Aktif { get; set; }
        public DateTime? LastNotifiedAt { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
