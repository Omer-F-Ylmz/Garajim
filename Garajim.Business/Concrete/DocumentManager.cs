using Garajim.Business.Abstract;
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
        private const long VarsayilanDosyaSiniri = 5 * 1024 * 1024;
        private const long VarsayilanKota = 250 * 1024 * 1024;

        private static readonly Dictionary<string, string> IzinliUzantilar = new Dictionary<string, string>
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".pdf"] = "application/pdf"
        };

        private static readonly Dictionary<string, byte[][]> SihirliBaytlar = new Dictionary<string, byte[][]>
        {
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }
        };

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

            var orijinalAd = Path.GetFileName(dto.FileName ?? string.Empty).Replace("\\", string.Empty);
            if (string.IsNullOrWhiteSpace(orijinalAd))
                return new ErrorDataResult<DocumentDto>(Messages.InvalidValue);

            var uzanti = Path.GetExtension(orijinalAd).ToLowerInvariant();
            if (!IzinliUzantilar.ContainsKey(uzanti))
                return new ErrorDataResult<DocumentDto>(Messages.DocumentExtensionNotAllowed);

            var icerik = dto.Content ?? Array.Empty<byte>();
            if (icerik.LongLength == 0)
                return new ErrorDataResult<DocumentDto>(Messages.InvalidValue);

            if (icerik.LongLength > DosyaSiniri())
                return new ErrorDataResult<DocumentDto>(Messages.DocumentTooLarge);

            if (!SihirliBaytUyuyorMu(uzanti, icerik))
                return new ErrorDataResult<DocumentDto>(Messages.DocumentContentMismatch);

            var mevcutToplam = await _documentDal.GetCompanyTotalSizeAsync();
            if (mevcutToplam + icerik.LongLength > Kota())
                return new ErrorDataResult<DocumentDto>(Messages.DocumentQuotaExceeded);

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
                OriginalName = orijinalAd.Length > 260 ? orijinalAd.Substring(0, 260) : orijinalAd,
                StoredName = saklananAd,
                SizeBytes = icerik.LongLength,
                ContentType = IzinliUzantilar[uzanti],
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
            var document = await ErisilebilirBelgeAsync(userId, documentId);
            if (document == null)
                return new ErrorResult(Messages.DocumentNotFound);

            await _documentDal.DeleteAsync(document);

            var tamYol = Path.Combine(KlasorYolu(), document.StoredName);
            if (File.Exists(tamYol))
            {
                File.Delete(tamYol);
            }

            return new SuccessResult(Messages.DocumentDeleted);
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

        private static bool SihirliBaytUyuyorMu(string uzanti, byte[] icerik)
        {
            if (!SihirliBaytlar.TryGetValue(uzanti, out var imzalar))
            {
                return false;
            }

            return imzalar.Any(imza => icerik.Length >= imza.Length && icerik.Take(imza.Length).SequenceEqual(imza));
        }

        private string KlasorYolu()
        {
            var yol = _configuration["Documents:StoragePath"];
            if (string.IsNullOrWhiteSpace(yol))
            {
                yol = Path.Combine(AppContext.BaseDirectory, "App_Data", "documents");
            }

            return Path.GetFullPath(yol);
        }

        private long DosyaSiniri()
        {
            return SayiOku("Documents:MaxFileSizeBytes", VarsayilanDosyaSiniri);
        }

        private long Kota()
        {
            return SayiOku("Documents:CompanyQuotaBytes", VarsayilanKota);
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
