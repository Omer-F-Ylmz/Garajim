using Garajim.Business.Abstract;
using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Tests.Integration
{
    public class SahteBelgeServisi : IDocumentService
    {
        public List<string> SilinenDosyalar { get; } = new List<string>();

        public Task<IDataResult<List<DocumentDto>>> GetListAsync(int userId, int? vehicleId, int? maintenanceRecordId)
            => Task.FromResult<IDataResult<List<DocumentDto>>>(new SuccessDataResult<List<DocumentDto>>(new List<DocumentDto>()));

        public Task<IDataResult<DocumentDto>> UploadAsync(int userId, DocumentUploadDto dto)
            => Task.FromResult<IDataResult<DocumentDto>>(new ErrorDataResult<DocumentDto>("test"));

        public Task<IDataResult<DocumentContentDto>> DownloadAsync(int userId, int documentId)
            => Task.FromResult<IDataResult<DocumentContentDto>>(new ErrorDataResult<DocumentContentDto>("test"));

        public Task<IResult> DeleteAsync(int userId, int documentId)
            => Task.FromResult<IResult>(new SuccessResult());

        public Task<IDataResult<string>> SatirSilAsync(int userId, int documentId)
            => Task.FromResult<IDataResult<string>>(new ErrorDataResult<string>("test"));

        public void DosyaSil(string saklananAd) => SilinenDosyalar.Add(saklananAd);
    }
}
