using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class VehicleCreateDto
    {
        public string Plate { get; set; }
        public bool YabanciPlaka { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public int CurrentKm { get; set; }
        public FuelType FuelType { get; set; }
        public KullanimTuru KullanimTuru { get; set; } = KullanimTuru.Hususi;
        public DateTime? IlkTescilTarihi { get; set; }
        public KasaTipi? KasaTipi { get; set; }
        public string Vites { get; set; }
        public string Motor { get; set; }
        public string AcilKisiAd { get; set; }
        public string AcilKisiTelefon { get; set; }
        public string AcilNot { get; set; }
    }

    public class VehicleUpdateDto
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public int CurrentKm { get; set; }
        public FuelType FuelType { get; set; }
        public KullanimTuru? KullanimTuru { get; set; }
        public DateTime? IlkTescilTarihi { get; set; }
        public KasaTipi? KasaTipi { get; set; }
        public string Vites { get; set; }
        public string Motor { get; set; }
        public string AcilKisiAd { get; set; }
        public string AcilKisiTelefon { get; set; }
        public string AcilNot { get; set; }
    }

    public class VehicleDto
    {
        public int Id { get; set; }
        public string Plate { get; set; }
        public bool YabanciPlaka { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public int CurrentKm { get; set; }
        public FuelType FuelType { get; set; }
        public KullanimTuru KullanimTuru { get; set; } = KullanimTuru.Hususi;
        public DateTime? IlkTescilTarihi { get; set; }
        public KasaTipi? KasaTipi { get; set; }
        public string Vites { get; set; }
        public string Motor { get; set; }
        public string AcilKisiAd { get; set; }
        public string AcilKisiTelefon { get; set; }
        public string AcilNot { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
