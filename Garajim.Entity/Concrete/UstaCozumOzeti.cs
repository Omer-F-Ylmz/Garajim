using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class UstaCozumOzeti : IEntity
    {
        public int Id { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public string Motor { get; set; }
        public string BelirtiKategori { get; set; }
        public string ParcaTuru { get; set; }
        public int Sayi { get; set; }
        public DateTime GuncellemeTarihi { get; set; }
    }
}
