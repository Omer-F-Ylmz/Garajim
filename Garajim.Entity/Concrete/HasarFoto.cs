using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class HasarFoto : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int HasarDosyasiId { get; set; }
        public int DocumentId { get; set; }
        public HasarFotoEtiketi Etiket { get; set; }
        public int Sira { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
