using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class UstaSohbet : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public int UserId { get; set; }
        public string Baslik { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
