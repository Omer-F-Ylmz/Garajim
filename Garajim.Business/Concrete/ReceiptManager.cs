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
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
        private readonly ILogger<ReceiptManager> _logger;

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
            IConfiguration configuration,
            ILogger<ReceiptManager> logger)
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
            _logger = logger;
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

        public async Task<IDataResult<ReceiptUploadResultDto>> UploadAsync(int userId, ReceiptUploadDto dto, bool otoOnay)
        {
            var orijinalAd = DocumentContentValidator.GuvenliAd(dto.FileName);
            var icerik = dto.Content ?? Array.Empty<byte>();

            var hata = DocumentContentValidator.Dogrula(orijinalAd, icerik, DosyaSiniri());
            if (hata != null)
                return new ErrorDataResult<ReceiptUploadResultDto>(hata);

            var mevcutToplam = await _documentDal.GetCompanyTotalSizeAsync();
            if (mevcutToplam + icerik.LongLength > Kota())
                return new ErrorDataResult<ReceiptUploadResultDto>(Messages.DocumentQuotaExceeded);

            var ayBasi = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            if (await _draftDal.GetMonthlyCountAsync(ayBasi) >= AylikLimit())
                return new ErrorDataResult<ReceiptUploadResultDto>(Messages.ReceiptMonthlyLimitExceeded);

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<ReceiptUploadResultDto>(Messages.UserNotFound);

            var uzanti = Path.GetExtension(orijinalAd).ToLowerInvariant();
            var icerikTipi = DocumentContentValidator.IcerikTipi(uzanti);

            var klasor = DocumentManager.DepoYolunuCoz(_configuration["Documents:StoragePath"]);
            Directory.CreateDirectory(klasor);

            var saklananAd = Guid.NewGuid().ToString("N") + uzanti;
            await File.WriteAllBytesAsync(Path.Combine(klasor, saklananAd), icerik);

            var saglayici = Saglayici();
            var kronometre = Stopwatch.StartNew();
            var sonuc = await _extractor.ExtractAsync(icerik, icerikTipi, CancellationToken.None);
            kronometre.Stop();
            var sureMs = (int)kronometre.ElapsedMilliseconds;

            _logger.LogInformation(
                "Fiş çıkarımı tamamlandı. Sağlayıcı={Saglayici} Süre={SureMs}ms Güven={Guven} " +
                "Tarih={TarihDolu} Tutar={TutarDolu} Kdv={KdvDolu} Litre={LitreDolu} " +
                "BirimFiyat={BirimFiyatDolu} Plaka={PlakaDolu} Km={KmDolu} Tur={Tur}",
                saglayici, sureMs, sonuc.GuvenSkoru,
                sonuc.Tarih != null, sonuc.ToplamTutar != null, sonuc.KdvTutari != null, sonuc.Litre != null,
                sonuc.BirimFiyat != null, sonuc.Plaka != null, sonuc.Km != null, sonuc.TahminiTur);

            var draft = new ReceiptDraft
            {
                Saglayici = saglayici,
                SureMs = sureMs,
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

            var onerilenArac = await OnerilenAracAsync(userId, sonuc.Plaka);
            draft.VehicleId = onerilenArac?.Id;

            if (!otoOnay)
            {
                await _draftDal.AddAsync(draft);
                return new SuccessDataResult<ReceiptUploadResultDto>(Sonuc(draft, null), Messages.ReceiptUploaded);
            }

            draft.AtlamaNedeni = OtoOnayEngeli(draft, sonuc, onerilenArac);
            if (draft.AtlamaNedeni != null)
            {
                await _draftDal.AddAsync(draft);
                return new SuccessDataResult<ReceiptUploadResultDto>(Sonuc(draft, null), Messages.ReceiptUploaded);
            }

            var onayDto = new ReceiptConfirmDto
            {
                VehicleId = onerilenArac.Id,
                Tur = draft.TahminiTur,
                Tarih = draft.Tarih.Value,
                Tutar = draft.ToplamTutar.Value,
                Km = draft.Km,
                Litre = draft.Litre,
                BirimFiyat = draft.BirimFiyat
            };

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            draft.Durum = ReceiptDraftStatus.Onaylandi;
            draft.OtoOnaylandi = true;
            draft.DuzeltilenAlanlar = null;
            await _draftDal.AddAsync(draft);

            var olusturulan = await KayitVeBelgeOlusturAsync(userId, draft, onerilenArac, onayDto);

            await _unitOfWork.CommitAsync();

            return new SuccessDataResult<ReceiptUploadResultDto>(Sonuc(draft, olusturulan), Messages.ReceiptAutoConfirmed);
        }

        private string OtoOnayEngeli(ReceiptDraft draft, ReceiptExtractionResult sonuc, Vehicle arac)
        {
            if (sonuc.GuvenSkoru < OtoOnayGuven())
                return "Okuma güveni otomatik onay eşiğinin altında.";

            var eksikler = new List<string>();
            if (draft.Tarih == null) eksikler.Add("tarih");
            if (draft.ToplamTutar == null) eksikler.Add("tutar");
            if (draft.TahminiTur == ReceiptType.Bilinmiyor) eksikler.Add("tür");

            if (eksikler.Count > 0)
                return "Fişten okunamayan alan var: " + string.Join(", ", eksikler) + ".";

            if (arac == null)
                return "Fişteki plaka eşleşen bir araca bağlanamadı.";

            return null;
        }

        private static ReceiptUploadResultDto Sonuc(ReceiptDraft draft, OlusturulanKayitDto olusturulan)
        {
            return new ReceiptUploadResultDto
            {
                TaslakId = draft.Id,
                Durum = draft.Durum.ToString(),
                AtlamaNedeni = draft.AtlamaNedeni,
                OlusturulanKayit = olusturulan,
                Taslak = MapToDto(draft)
            };
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

            await KayitVeBelgeOlusturAsync(userId, draft, vehicle, dto);

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

        private async Task<OlusturulanKayitDto> KayitVeBelgeOlusturAsync(int userId, ReceiptDraft draft, Vehicle vehicle, ReceiptConfirmDto dto)
        {
            int? maintenanceRecordId = null;
            var olusturulan = new OlusturulanKayitDto { Tur = dto.Tur.ToString() };

            if (dto.Tur == ReceiptType.Yakit)
            {
                var record = new FuelRecord
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    Date = dto.Tarih.Date,
                    Liters = dto.Litre ?? 0m,
                    TotalCost = dto.Tutar,
                    Km = dto.Km ?? vehicle.CurrentKm
                };
                await _fuelDal.AddAsync(record);
                olusturulan.Id = record.Id;
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
                olusturulan.Id = record.Id;
            }
            else
            {
                var record = new ExpenseRecord
                {
                    CompanyId = vehicle.CompanyId,
                    VehicleId = vehicle.Id,
                    Category = dto.MasrafKategorisi ?? ExpenseCategory.Diger,
                    Date = dto.Tarih.Date,
                    Amount = dto.Tutar,
                    Note = dto.Not
                };
                await _expenseDal.AddAsync(record);
                olusturulan.Id = record.Id;
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

            return olusturulan;
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

        private async Task<Vehicle> OnerilenAracAsync(int userId, string plaka)
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

            return await _vehicleAccess.GetAccessibleAsync(userId, vehicle.Id);
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
                OtoOnaylandi = draft.OtoOnaylandi,
                AtlamaNedeni = draft.AtlamaNedeni,
                OlusturmaTarihi = draft.OlusturmaTarihi
            };
        }

        public async Task<IDataResult<ReceiptStatsDto>> GetStatsAsync(int userId)
        {
            var drafts = await _draftDal.GetListAsync();

            var istatistik = new ReceiptStatsDto
            {
                ToplamCagri = drafts.Count,
                Onaylanan = drafts.Count(d => d.Durum == ReceiptDraftStatus.Onaylandi),
                OtoOnaylanan = drafts.Count(d => d.OtoOnaylandi),
                Reddedilen = drafts.Count(d => d.Durum == ReceiptDraftStatus.Reddedildi),
                Bekleyen = drafts.Count(d => d.Durum == ReceiptDraftStatus.Bekliyor)
            };

            if (drafts.Count > 0)
            {
                istatistik.OnayOrani = Yuzde(istatistik.Onaylanan, drafts.Count);
                istatistik.RedOrani = Yuzde(istatistik.Reddedilen, drafts.Count);
                istatistik.OrtalamaGuven = Math.Round(drafts.Average(d => d.GuvenSkoru), 3);
                istatistik.OrtalamaSureMs = Math.Round(drafts.Average(d => (double)d.SureMs), 1);

                istatistik.AlanDoluluk["tarih"] = Yuzde(drafts.Count(d => d.Tarih != null), drafts.Count);
                istatistik.AlanDoluluk["toplamTutar"] = Yuzde(drafts.Count(d => d.ToplamTutar != null), drafts.Count);
                istatistik.AlanDoluluk["kdvTutari"] = Yuzde(drafts.Count(d => d.KdvTutari != null), drafts.Count);
                istatistik.AlanDoluluk["litre"] = Yuzde(drafts.Count(d => d.Litre != null), drafts.Count);
                istatistik.AlanDoluluk["birimFiyat"] = Yuzde(drafts.Count(d => d.BirimFiyat != null), drafts.Count);
                istatistik.AlanDoluluk["plaka"] = Yuzde(drafts.Count(d => d.Plaka != null), drafts.Count);
                istatistik.AlanDoluluk["km"] = Yuzde(drafts.Count(d => d.Km != null), drafts.Count);
                istatistik.AlanDoluluk["tur"] = Yuzde(drafts.Count(d => d.TahminiTur != ReceiptType.Bilinmiyor), drafts.Count);
            }

            var onaylananlar = drafts.Where(d => d.Durum == ReceiptDraftStatus.Onaylandi && !d.OtoOnaylandi).ToList();
            if (onaylananlar.Count > 0)
            {
                foreach (var alan in new[] { "Tarih", "Tutar", "Km", "Litre", "BirimFiyat", "Tur", "Arac" })
                {
                    var duzeltilen = onaylananlar.Count(d => DuzeltilenIceriyorMu(d.DuzeltilenAlanlar, alan));
                    istatistik.AlanDuzeltmeOrani[alan.ToLowerInvariant()] = Yuzde(duzeltilen, onaylananlar.Count);
                }
            }

            return new SuccessDataResult<ReceiptStatsDto>(istatistik);
        }

        private static bool DuzeltilenIceriyorMu(string duzeltilenAlanlar, string alan)
        {
            if (string.IsNullOrWhiteSpace(duzeltilenAlanlar))
            {
                return false;
            }

            return duzeltilenAlanlar.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(a => a.Trim() == alan);
        }

        private static double Yuzde(int pay, int payda)
        {
            return payda == 0 ? 0 : Math.Round(pay * 100.0 / payda, 1);
        }

        private string Saglayici()
        {
            return string.Equals(_configuration["Receipts:Provider"], "OpenAI", StringComparison.OrdinalIgnoreCase)
                ? "OpenAI"
                : "Gemini";
        }

        private double OtoOnayGuven()
        {
            var deger = _configuration["Receipts:OtoOnayGuven"];
            return double.TryParse(deger, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sonuc) && sonuc > 0 && sonuc <= 1
                ? sonuc
                : 0.85;
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
