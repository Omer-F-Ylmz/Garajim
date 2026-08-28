namespace Garajim.Entity.Dtos
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public int? VehicleId { get; set; }
        public int? MaintenanceRecordId { get; set; }
        public string OriginalName { get; set; }
        public long SizeBytes { get; set; }
        public string ContentType { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DocumentUploadDto
    {
        public int? VehicleId { get; set; }
        public int? MaintenanceRecordId { get; set; }
        public string FileName { get; set; }
        public byte[] Content { get; set; }
    }

    public class DocumentContentDto
    {
        public string OriginalName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }
}
