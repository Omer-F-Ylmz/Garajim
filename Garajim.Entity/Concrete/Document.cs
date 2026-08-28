using Garajim.Core.Entities;

namespace Garajim.Entity.Concrete
{
    public class Document : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int? VehicleId { get; set; }
        public int? MaintenanceRecordId { get; set; }
        public string OriginalName { get; set; }
        public string StoredName { get; set; }
        public long SizeBytes { get; set; }
        public string ContentType { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
