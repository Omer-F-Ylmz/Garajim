using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IDocumentService
    {
        Task<IDataResult<List<DocumentDto>>> GetListAsync(int userId, int? vehicleId, int? maintenanceRecordId);
        Task<IDataResult<DocumentDto>> UploadAsync(int userId, DocumentUploadDto dto);
        Task<IDataResult<DocumentContentDto>> DownloadAsync(int userId, int documentId);
        Task<IResult> DeleteAsync(int userId, int documentId);
    }
}
