using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class VehicleAssignment : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int AssignedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
