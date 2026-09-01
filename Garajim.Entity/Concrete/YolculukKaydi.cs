using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class YolculukKaydi : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public int UserId { get; set; }
        public DateTime Tarih { get; set; }
        public int BaslangicKm { get; set; }
        public int BitisKm { get; set; }
        public int MesafeKm { get; set; }
        public YolculukAmaci Amac { get; set; }
        public string Nereden { get; set; }
        public string Nereye { get; set; }
        public string Not { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
