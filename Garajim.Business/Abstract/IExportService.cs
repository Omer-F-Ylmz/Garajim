using Garajim.Core.Utilities.Results;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Abstract
{
    public interface IExportService
    {
        Task<IDataResult<ExportSonucDto>> CsvAsync(int userId, string tur, int? vehicleId, DateTime? baslangic, DateTime? bitis);
    }
}
