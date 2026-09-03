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
        public bool? KmDusurmeOnayi { get; set; }
        public string KmDuzeltmeNedeni { get; set; }
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

    public class AracArsivDto
    {
        public ArsivNedeni Neden { get; set; }
    }

    public class KmDuzeltmeDto
    {
        public int EskiKm { get; set; }
        public int YeniKm { get; set; }
        public string Neden { get; set; }
        public DateTime Tarih { get; set; }
    }

    public class VehicleDto
    {
        public int Id { get; set; }
        public string Plate { get; set; }
        public bool YabanciPlaka { get; set; }
        public bool Arsivli { get; set; }
        public string ArsivNedeni { get; set; }
        public DateTime? ArsivTarihi { get; set; }
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
