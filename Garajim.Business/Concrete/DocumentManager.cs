using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Documents;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class DocumentManager : IDocumentService
    {
        private readonly IDocumentDal _documentDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IConfiguration _configuration;

        public DocumentManager(IDocumentDal documentDal, IMaintenanceDal maintenanceDal, IVehicleAccessService vehicleAccess, IConfiguration configuration)
        {
            _documentDal = documentDal;
            _maintenanceDal = maintenanceDal;
            _vehicleAccess = vehicleAccess;
            _configuration = configuration;
        }

        public async Task<IDataResult<List<DocumentDto>>> GetListAsync(int userId, int? vehicleId, int? maintenanceRecordId)
        {
            var baglam = await BaglamCozAsync(userId, vehicleId, maintenanceRecordId);
            if (baglam == null)
                return new ErrorDataResult<List<DocumentDto>>(Messages.VehicleNotFound);

            var documents = await _documentDal.GetListAsync(d =>
                (vehicleId == null || d.VehicleId == vehicleId) &&
                (maintenanceRecordId == null || d.MaintenanceRecordId == maintenanceRecordId));

            var list = documents.OrderByDescending(d => d.CreatedAt).Select(MapToDto).ToList();
            return new SuccessDataResult<List<DocumentDto>>(list);
        }

        public async Task<IDataResult<DocumentDto>> UploadAsync(int userId, DocumentUploadDto dto)
        {
            if (dto.VehicleId == null && dto.MaintenanceRecordId == null)
                return new ErrorDataResult<DocumentDto>(Messages.DocumentContextRequired);

            var vehicle = await BaglamCozAsync(userId, dto.VehicleId, dto.MaintenanceRecordId);
            if (vehicle == null)
                return new ErrorDataResult<DocumentDto>(Messages.VehicleNotFound);

            var orijinalAd = DocumentContentValidator.GuvenliAd(dto.FileName);
            var icerik = dto.Content ?? Array.Empty<byte>();

            var hata = DocumentContentValidator.Dogrula(orijinalAd, icerik, DosyaSiniri());
            if (hata != null)
                return new ErrorDataResult<DocumentDto>(hata);

            var mevcutToplam = await _documentDal.GetCompanyTotalSizeAsync();
            if (mevcutToplam + icerik.LongLength > Kota())
                return new ErrorDataResult<DocumentDto>(Messages.DocumentQuotaExceeded);

            var uzanti = Path.GetExtension(orijinalAd).ToLowerInvariant();
            var klasor = KlasorYolu();
            Directory.CreateDirectory(klasor);

            var saklananAd = Guid.NewGuid().ToString("N") + uzanti;
            var tamYol = Path.Combine(klasor, saklananAd);
            await File.WriteAllBytesAsync(tamYol, icerik);

            var document = new Document
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = dto.VehicleId,
                MaintenanceRecordId = dto.MaintenanceRecordId,
                OriginalName = orijinalAd,
                StoredName = saklananAd,
                SizeBytes = icerik.LongLength,
                ContentType = DocumentContentValidator.IcerikTipi(uzanti),
                UploadedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _documentDal.AddAsync(document);
            return new SuccessDataResult<DocumentDto>(MapToDto(document), Messages.DocumentUploaded);
        }

        public async Task<IDataResult<DocumentContentDto>> DownloadAsync(int userId, int documentId)
        {
            var document = await ErisilebilirBelgeAsync(userId, documentId);
            if (document == null)
                return new ErrorDataResult<DocumentContentDto>(Messages.DocumentNotFound);

            var tamYol = Path.Combine(KlasorYolu(), document.StoredName);
            if (!File.Exists(tamYol))
                return new ErrorDataResult<DocumentContentDto>(Messages.DocumentNotFound);

            var icerik = await File.ReadAllBytesAsync(tamYol);
            return new SuccessDataResult<DocumentContentDto>(new DocumentContentDto
            {
                OriginalName = document.OriginalName,
                ContentType = document.ContentType,
                Content = icerik
            });
        }

        public async Task<IResult> DeleteAsync(int userId, int documentId)
        {
            var satir = await SatirSilAsync(userId, documentId);
            if (!satir.Success)
                return new ErrorResult(satir.Message);

            DosyaSil(satir.Data);

            return new SuccessResult(Messages.DocumentDeleted);
        }

        public async Task<IDataResult<string>> SatirSilAsync(int userId, int documentId)
        {
            var document = await ErisilebilirBelgeAsync(userId, documentId);
            if (document == null)
                return new ErrorDataResult<string>(Messages.DocumentNotFound);

            await _documentDal.DeleteAsync(document);

            return new SuccessDataResult<string>(document.StoredName);
        }

        public void DosyaSil(string saklananAd)
        {
            if (string.IsNullOrWhiteSpace(saklananAd))
            {
                return;
            }

            var tamYol = Path.Combine(KlasorYolu(), saklananAd);
            if (File.Exists(tamYol))
            {
                File.Delete(tamYol);
            }
        }

        private async Task<Document> ErisilebilirBelgeAsync(int userId, int documentId)
        {
            var document = await _documentDal.GetAsync(d => d.Id == documentId);
            if (document == null)
            {
                return null;
            }

            var vehicle = await BaglamCozAsync(userId, document.VehicleId, document.MaintenanceRecordId);
            return vehicle == null ? null : document;
        }

        private async Task<Vehicle> BaglamCozAsync(int userId, int? vehicleId, int? maintenanceRecordId)
        {
            if (maintenanceRecordId != null)
            {
                var record = await _maintenanceDal.GetAsync(m => m.Id == maintenanceRecordId);
                if (record == null)
                {
                    return null;
                }

                return await _vehicleAccess.GetAccessibleAsync(userId, record.VehicleId);
            }

            if (vehicleId != null)
            {
                return await _vehicleAccess.GetAccessibleAsync(userId, vehicleId.Value);
            }

            return null;
        }

        public static string DepoYolunuCoz(string yapilandirilanYol)
        {
            if (string.IsNullOrWhiteSpace(yapilandirilanYol))
            {
                return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "App_Data", "documents"));
            }

            if (Path.IsPathRooted(yapilandirilanYol))
            {
                return Path.GetFullPath(yapilandirilanYol);
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, yapilandirilanYol));
        }

        private string KlasorYolu()
        {
            return DepoYolunuCoz(_configuration["Documents:StoragePath"]);
        }

        private long DosyaSiniri()
        {
            return SayiOku("Documents:MaxFileSizeBytes", DocumentContentValidator.VarsayilanDosyaSiniri);
        }

        private long Kota()
        {
            return SayiOku("Documents:CompanyQuotaBytes", DocumentContentValidator.VarsayilanKota);
        }

        private long SayiOku(string anahtar, long varsayilan)
        {
            var deger = _configuration[anahtar];
            return long.TryParse(deger, out var sonuc) && sonuc > 0 ? sonuc : varsayilan;
        }

        private static DocumentDto MapToDto(Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                VehicleId = document.VehicleId,
                MaintenanceRecordId = document.MaintenanceRecordId,
                OriginalName = document.OriginalName,
                SizeBytes = document.SizeBytes,
                ContentType = document.ContentType,
                UploadedByUserId = document.UploadedByUserId,
                CreatedAt = document.CreatedAt
            };
        }
    }
}
