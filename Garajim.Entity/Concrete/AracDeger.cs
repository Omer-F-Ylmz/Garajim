using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class AracDeger : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public DateTime Tarih { get; set; }
        public decimal Deger { get; set; }
        public DegerKaynagi Kaynak { get; set; }
        public string Not { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
