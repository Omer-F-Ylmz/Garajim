namespace Garajim.Entity.Dtos
{
    public class FuelCreateDto
    {
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Liters { get; set; }
        public decimal TotalCost { get; set; }
        public int Km { get; set; }
    }

    public class FuelDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Liters { get; set; }
        public decimal TotalCost { get; set; }
        public int Km { get; set; }
    }
}
