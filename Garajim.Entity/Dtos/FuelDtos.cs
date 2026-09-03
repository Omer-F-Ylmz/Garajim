using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class FuelCreateDto
    {
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Liters { get; set; }
        public decimal TotalCost { get; set; }
        public int Km { get; set; }
        public decimal? Kwh { get; set; }
        public SarjTuru? SarjTuru { get; set; }
        public bool? TamDolum { get; set; }
    }

    public class FuelDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Liters { get; set; }
        public decimal TotalCost { get; set; }
        public int Km { get; set; }
        public decimal? Kwh { get; set; }
        public string SarjTuru { get; set; }
        public bool TamDolum { get; set; }
        public bool SupheliKm { get; set; }
    }
}
