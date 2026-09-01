using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class LastikSeti : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public string Ad { get; set; }
        public LastikMevsimi Mevsim { get; set; }
        public string Marka { get; set; }
        public string Ebat { get; set; }
        public decimal? DisDerinligiMm { get; set; }
        public DateTime TakilmaTarihi { get; set; }
        public int TakilmaKm { get; set; }
        public DateTime? SokulmeTarihi { get; set; }
        public int? SokulmeKm { get; set; }
        public int ToplamKm { get; set; }
        public bool Takili { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
