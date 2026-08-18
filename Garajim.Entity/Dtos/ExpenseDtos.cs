using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class ExpenseCreateDto
    {
        public int VehicleId { get; set; }
        public ExpenseCategory Category { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
    }

    public class ExpenseDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public ExpenseCategory Category { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
    }
}
