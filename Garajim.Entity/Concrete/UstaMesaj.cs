using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class UstaMesaj : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int SohbetId { get; set; }
        public UstaRol Rol { get; set; }
        public string Metin { get; set; }
        public string YapiliYanit { get; set; }
        public bool KirmiziCizgi { get; set; }
        public int TokenGiris { get; set; }
        public int TokenCikis { get; set; }
        public int SureMs { get; set; }
        public UstaGeriBildirim GeriBildirim { get; set; }
        public int? CozumBakimId { get; set; }
        public string BilgiKategorisi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
