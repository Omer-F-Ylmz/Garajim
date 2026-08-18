namespace Garajim.Entity.Dtos
{
    public class CategoryTotalDto
    {
        public string Category { get; set; }
        public decimal Total { get; set; }
    }

    public class ExpenseSummaryDto
    {
        public decimal TotalFuel { get; set; }
        public decimal TotalMaintenance { get; set; }
        public decimal TotalOtherExpense { get; set; }
        public decimal GrandTotal { get; set; }
        public List<CategoryTotalDto> Categories { get; set; }
    }

    public class MonthlyCostDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Total { get; set; }
    }

    public class FuelStatsDto
    {
        public int TotalKm { get; set; }
        public decimal TotalLiters { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageConsumptionPer100Km { get; set; }
        public decimal CostPerKm { get; set; }
    }
}
