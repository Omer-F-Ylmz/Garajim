using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IReceiptExtractor
    {
        Task<ReceiptExtractionResult> ExtractAsync(byte[] imageBytes, string mimeType, CancellationToken ct);
    }
}
