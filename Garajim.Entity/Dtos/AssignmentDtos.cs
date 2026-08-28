namespace Garajim.Entity.Dtos
{
    public class AssignmentCreateDto
    {
        public int VehicleId { get; set; }
        public int UserId { get; set; }
    }

    public class AssignmentEndDto
    {
        public int VehicleId { get; set; }
    }

    public class AssignmentDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Plate { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int AssignedByUserId { get; set; }
        public bool IsActive { get; set; }
    }
}
