using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IDocumentService
    {
        Task<IDataResult<List<DocumentDto>>> GetListAsync(int userId, int? vehicleId, int? maintenanceRecordId);
        Task<IDataResult<SayfaliSonuc<DocumentDto>>> GetSayfaAsync(int userId, int? vehicleId, int? maintenanceRecordId, ListeSorgusu sorgu);
        Task<IDataResult<DocumentDto>> UploadAsync(int userId, DocumentUploadDto dto);
        Task<IDataResult<DocumentContentDto>> DownloadAsync(int userId, int documentId);
        Task<IDataResult<DocumentContentDto>> OnizlemeAsync(int userId, int documentId);
        Task<IDataResult<DocumentDto>> BaglaAsync(int userId, int documentId, int? maintenanceRecordId);
        Task<IResult> DeleteAsync(int userId, int documentId);
        Task<IDataResult<string>> SatirSilAsync(int userId, int documentId);
        void DosyaSil(string saklananAd);
    }
}
