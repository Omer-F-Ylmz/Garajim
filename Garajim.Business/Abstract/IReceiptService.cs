using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Abstract
{
    public interface IReceiptService
    {
        Task<IDataResult<List<ReceiptDraftDto>>> GetListAsync(int userId, ReceiptDraftStatus? durum);
        Task<IDataResult<ReceiptDraftDto>> GetByIdAsync(int userId, int id);
        Task<IDataResult<ReceiptDraftDto>> UploadAsync(int userId, ReceiptUploadDto dto);
        Task<IDataResult<ReceiptDraftDto>> ConfirmAsync(int userId, int id, ReceiptConfirmDto dto);
        Task<IResult> RejectAsync(int userId, int id);
    }
}
