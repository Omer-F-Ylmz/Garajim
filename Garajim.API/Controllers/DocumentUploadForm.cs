namespace Garajim.API.Controllers
{
    public class DocumentUploadForm
    {
        public IFormFile File { get; set; }
        public int? VehicleId { get; set; }
        public int? MaintenanceRecordId { get; set; }
    }
}
