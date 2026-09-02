using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class UstaOnay : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public string MetinSurumu { get; set; }
        public DateTime KabulTarihi { get; set; }
    }
}
