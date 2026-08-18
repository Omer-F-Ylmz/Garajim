using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class MaintenanceCreateDto
    {
        public int VehicleId { get; set; }
        public MaintenanceType Type { get; set; }
        public DateTime Date { get; set; }
        public int Km { get; set; }
        public decimal Cost { get; set; }
        public string ServiceName { get; set; }
        public string Note { get; set; }
    }

    public class MaintenanceDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public MaintenanceType Type { get; set; }
        public DateTime Date { get; set; }
        public int Km { get; set; }
        public decimal Cost { get; set; }
        public string ServiceName { get; set; }
        public string Note { get; set; }
    }
}
