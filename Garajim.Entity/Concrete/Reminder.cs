using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class Reminder : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int VehicleId { get; set; }
        public ReminderType Type { get; set; }
        public DateTime? DueDate { get; set; }
        public int? DueKm { get; set; }
        public string Note { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastNotifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
