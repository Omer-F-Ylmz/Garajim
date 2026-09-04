using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class GeriBildirim : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public GeriBildirimTuru Tur { get; set; }
        public string Mesaj { get; set; }
        public string Sayfa { get; set; }
        public string Surum { get; set; }
        public DateTime Tarih { get; set; }
    }
}
