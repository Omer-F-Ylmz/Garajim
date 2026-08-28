using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class Company : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public PlanType PlanType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
