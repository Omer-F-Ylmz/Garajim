using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class HasarDosyasi : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public DateTime OlayTarihi { get; set; }
        public HasarTuru Tur { get; set; }
        public string Konum { get; set; }
        public string Aciklama { get; set; }
        public int? OlayKm { get; set; }
        public TutanakTuru TutanakTuru { get; set; }
        public string KarsiTarafPlaka { get; set; }
        public string KarsiTarafSigorta { get; set; }
        public string KarsiTarafPoliceNo { get; set; }
        public string SigortaDosyaNo { get; set; }
        public decimal? HasarBedeli { get; set; }
        public HasarDurumu Durum { get; set; }
        public int OlusturanUserId { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
    }
}
