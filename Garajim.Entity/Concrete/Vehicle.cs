using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class Vehicle : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public string Plate { get; set; }
        public bool YabanciPlaka { get; set; }
        public bool ModelEslesmedi { get; set; }
        public bool Arsivli { get; set; }
        public DateTime? SonKmGuncelleme { get; set; }
        public DateTime? ArsivTarihi { get; set; }
        public ArsivNedeni? ArsivNedeni { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public int CurrentKm { get; set; }
        public FuelType FuelType { get; set; }
        public KullanimTuru KullanimTuru { get; set; }
        public DateTime? IlkTescilTarihi { get; set; }
        public string Motor { get; set; }
        public string Vites { get; set; }
        public KasaTipi? KasaTipi { get; set; }
        public string AcilKisiAd { get; set; }
        public string AcilKisiTelefon { get; set; }
        public string AcilNot { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
