using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class TakvimAbonelik : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; }
        public bool Aktif { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
