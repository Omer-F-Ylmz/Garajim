using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Documents;
using Garajim.Business.Concrete.Receipts;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class ReceiptManager : IReceiptService
    {
        private const int VarsayilanAylikLimit = 100;

        private readonly IReceiptDraftDal _draftDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IDocumentDal _documentDal;
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IFuelDal _fuelDal;
        private readonly IExpenseDal _expenseDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IReceiptExtractor _extractor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public ReceiptManager(
            IReceiptDraftDal draftDal,
            IUserDal userDal,
            IVehicleDal vehicleDal,
            IDocumentDal documentDal,
            IMaintenanceDal maintenanceDal,
            IFuelDal fuelDal,
            IExpenseDal expenseDal,
            IVehicleAccessService vehicleAccess,
            IReceiptExtractor extractor,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _draftDal = draftDal;
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _documentDal = documentDal;
            _maintenanceDal = maintenanceDal;
            _fuelDal = fuelDal;
            _expenseDal = expenseDal;
            _vehicleAccess = vehicleAccess;
            _extractor = extractor;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<IDataResult<List<ReceiptDraftDto>>> GetListAsync(int userId, ReceiptDraftStatus? durum)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<List<ReceiptDraftDto>>(Messages.UserNotFound);

            var drafts = await _draftDal.GetListAsync(d => durum == null || d.Durum == durum);

            var list = new List<ReceiptDraftDto>();
            foreach (var draft in drafts.OrderByDescending(d => d.OlusturmaTarihi))
            {
                if (await ErisebilirMi(user, draft))
                {
                    list.Add(MapToDto(draft));
                }
            }

            return new SuccessDataResult<List<ReceiptDraftDto>>(list);
        }

        public async Task<IDataResult<ReceiptDraftDto>> GetByIdAsync(int userId, int id)
        {
            var draft = await ErisilebilirTaslakAsync(userId, id);
            if (draft == null)
                return new ErrorDataResult<ReceiptDraftDto>(Messages.ReceiptNotFound);

            return new SuccessDataResult<ReceiptDraftDto>(MapToDto(draft));
        }

        public async Task<IDataResult<ReceiptDraftDto>> UploadAsync(int userId, ReceiptUploadDto dto)
        {
            var orijinalAd = DocumentContentValidator.GuvenliAd(dto.FileName);
            var icerik = dto.Content ?? Array.Empty<byte>();

            var hata = DocumentContentValidator.Dogrula(orijinalAd, icerik, DosyaSiniri());
            if (hata != null)
                return new ErrorDataResult<ReceiptDraftDto>(hata);

            var mevcutToplam = await _documentDal.GetCompanyTotalSizeAsync();
            if (mevcutToplam + icerik.LongLength > Kota())
                return new ErrorDataResult<ReceiptDraftDto>(Messages.DocumentQuotaExceeded);

            var ayBasi = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            if (await _draftDal.GetMonthlyCountAsync(ayBasi) >= AylikLimit())
                return new ErrorDataResult<ReceiptDraftDto>(Messages.ReceiptMonthlyLimitExceeded);

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<ReceiptDraftDto>(Messages.UserNotFound);

            var uzanti = Path.GetExtension(orijinalAd).ToLowerInvariant();
            var icerikTipi = DocumentContentValidator.IcerikTipi(uzanti);

            var klasor = DocumentManager.DepoYolunuCoz(_configuration["Documents:StoragePath"]);
            Directory.CreateDirectory(klasor);

            var saklananAd = Guid.NewGuid().ToString("N") + uzanti;
            await File.WriteAllBytesAsync(Path.Combine(klasor, saklananAd), icerik);

            var sonuc = await _extractor.ExtractAsync(icerik, icerikTipi, CancellationToken.None);

            var draft = new ReceiptDraft
            {
                CompanyId = user.CompanyId,
                YukleyenUserId = userId,
                DosyaYolu = saklananAd,
                OrijinalAd = orijinalAd,
                IcerikTipi = icerikTipi,
                BoyutBayt = icerik.LongLength,
                Durum = ReceiptDraftStatus.Bekliyor,
                Tarih = sonuc.Tarih,
                ToplamTutar = sonuc.ToplamTutar,
                KdvTutari = sonuc.KdvTutari,
                Litre = sonuc.Litre,
                BirimFiyat = sonuc.BirimFiyat,
                Plaka = sonuc.Plaka,
                Km = sonuc.Km,
                TahminiTur = sonuc.TahminiTur,
                GuvenSkoru = sonuc.GuvenSkoru,
                OlusturmaTarihi = DateTime.UtcNow
            };

            draft.VehicleId = await OnerilenAracAsync(userId, sonuc.Plaka);

            await _draftDal.AddAsync(draft);
            return new SuccessDataResult<ReceiptDraftDto>(MapToDto(draft), Messages.ReceiptUploaded);
        }

        public async Task<IDataResult<ReceiptDraftDto>> ConfirmAsync(int userId, int id, ReceiptConfirmDto dto)
        {
            var draft = await ErisilebilirTaslakAsync(userId, id);
            if (draft == null)
                return new ErrorDataResult<ReceiptDraftDto>(Messages.ReceiptNotFound);

            if (draft.Durum != ReceiptDraftStatus.Bekliyor)
                return new ErrorDataResult<ReceiptDraftDto>(Messages.ReceiptAlreadyHandled);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<ReceiptDraftDto>(Messages.VehicleNotFound);

            if (dto.Tutar <= 0 || !Enum.IsDefined(dto.Tur) || dto.Tur == ReceiptType.Bilinmiyor)
                return new ErrorDataResult<ReceiptDraftDto>(Messages.InvalidValue);

            draft.DuzeltilenAlanlar = DuzeltilenAlanlariBul(draft, dto);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            int? maintenanceRecordId = null;

            if (dto.Tur == ReceiptType.Yakit)
            {
                await _fuelDal.AddAsync(new FuelRecord
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    Date = dto.Tarih.Date,
                    Liters = dto.Litre ?? 0m,
                    TotalCost = dto.Tutar,
                    Km = dto.Km ?? vehicle.CurrentKm
                });
            }
            else if (dto.Tur == ReceiptType.Bakim)
            {
                var record = new MaintenanceRecord
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    Type = dto.BakimTuru ?? MaintenanceType.Diger,
                    Date = dto.Tarih.Date,
                    Km = dto.Km ?? vehicle.CurrentKm,
                    Cost = dto.Tutar,
                    ServiceName = dto.ServisAdi,
                    Note = dto.Not
                };
                await _maintenanceDal.AddAsync(record);
                maintenanceRecordId = record.Id;
            }
            else
            {
                await _expenseDal.AddAsync(new ExpenseRecord
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    Category = dto.MasrafKategorisi ?? ExpenseCategory.Diger,
                    Date = dto.Tarih.Date,
                    Amount = dto.Tutar,
                    Note = dto.Not
                });
            }

            await _documentDal.AddAsync(new Document
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = vehicle.Id,
                MaintenanceRecordId = maintenanceRecordId,
                OriginalName = draft.OrijinalAd,
                StoredName = draft.DosyaYolu,
                SizeBytes = draft.BoyutBayt,
                ContentType = draft.IcerikTipi,
                UploadedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            draft.Durum = ReceiptDraftStatus.Onaylandi;
            draft.VehicleId = vehicle.Id;
            await _draftDal.UpdateAsync(draft);

            await _unitOfWork.CommitAsync();

            return new SuccessDataResult<ReceiptDraftDto>(MapToDto(draft), Messages.ReceiptConfirmed);
        }

        public async Task<IResult> RejectAsync(int userId, int id)
        {
            var draft = await ErisilebilirTaslakAsync(userId, id);
            if (draft == null)
                return new ErrorResult(Messages.ReceiptNotFound);

            if (draft.Durum != ReceiptDraftStatus.Bekliyor)
                return new ErrorResult(Messages.ReceiptAlreadyHandled);

            draft.Durum = ReceiptDraftStatus.Reddedildi;
            await _draftDal.UpdateAsync(draft);

            var tamYol = Path.Combine(DocumentManager.DepoYolunuCoz(_configuration["Documents:StoragePath"]), draft.DosyaYolu);
            if (File.Exists(tamYol))
            {
                File.Delete(tamYol);
            }

            return new SuccessResult(Messages.ReceiptRejected);
        }

        private static string DuzeltilenAlanlariBul(ReceiptDraft draft, ReceiptConfirmDto dto)
        {
            var degisenler = new List<string>();

            if (draft.Tarih?.Date != dto.Tarih.Date)
                degisenler.Add("Tarih");

            if (draft.ToplamTutar != dto.Tutar)
                degisenler.Add("Tutar");

            if (draft.Km != dto.Km)
                degisenler.Add("Km");

            if (draft.Litre != dto.Litre)
                degisenler.Add("Litre");

            if (draft.BirimFiyat != dto.BirimFiyat)
                degisenler.Add("BirimFiyat");

            if (draft.TahminiTur != dto.Tur)
                degisenler.Add("Tur");

            if (draft.VehicleId != dto.VehicleId)
                degisenler.Add("Arac");

            return string.Join(",", degisenler);
        }

        private async Task<int?> OnerilenAracAsync(int userId, string plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka))
            {
                return null;
            }

            var vehicle = await _vehicleDal.GetAsync(v => v.Plate == plaka);
            if (vehicle == null)
            {
                return null;
            }

            return await _vehicleAccess.GetAccessibleAsync(userId, vehicle.Id) == null ? null : vehicle.Id;
        }

        private async Task<ReceiptDraft> ErisilebilirTaslakAsync(int userId, int id)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
            {
                return null;
            }

            var draft = await _draftDal.GetAsync(d => d.Id == id);
            if (draft == null)
            {
                return null;
            }

            return await ErisebilirMi(user, draft) ? draft : null;
        }

        private async Task<bool> ErisebilirMi(AppUser user, ReceiptDraft draft)
        {
            if (user.Role != CompanyRole.Driver)
            {
                return true;
            }

            if (draft.YukleyenUserId == user.Id)
            {
                return true;
            }

            if (draft.VehicleId == null)
            {
                return false;
            }

            return await _vehicleAccess.GetAccessibleAsync(user.Id, draft.VehicleId.Value) != null;
        }

        private static ReceiptDraftDto MapToDto(ReceiptDraft draft)
        {
            return new ReceiptDraftDto
            {
                Id = draft.Id,
                VehicleId = draft.VehicleId,
                Durum = draft.Durum.ToString(),
                OrijinalAd = draft.OrijinalAd,
                Tarih = draft.Tarih,
                ToplamTutar = draft.ToplamTutar,
                KdvTutari = draft.KdvTutari,
                Litre = draft.Litre,
                BirimFiyat = draft.BirimFiyat,
                Plaka = draft.Plaka,
                Km = draft.Km,
                TahminiTur = draft.TahminiTur.ToString(),
                GuvenSkoru = draft.GuvenSkoru,
                DuzeltilenAlanlar = draft.DuzeltilenAlanlar,
                OlusturmaTarihi = draft.OlusturmaTarihi
            };
        }

        private long DosyaSiniri()
        {
            return SayiOku("Documents:MaxFileSizeBytes", DocumentContentValidator.VarsayilanDosyaSiniri);
        }

        private long Kota()
        {
            return SayiOku("Documents:CompanyQuotaBytes", DocumentContentValidator.VarsayilanKota);
        }

        private int AylikLimit()
        {
            return (int)SayiOku("Receipts:AylikLimit", VarsayilanAylikLimit);
        }

        private long SayiOku(string anahtar, long varsayilan)
        {
            var deger = _configuration[anahtar];
            return long.TryParse(deger, out var sonuc) && sonuc > 0 ? sonuc : varsayilan;
        }
    }
}
