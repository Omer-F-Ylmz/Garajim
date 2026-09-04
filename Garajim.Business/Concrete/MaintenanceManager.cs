using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Business.Katalog;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class MaintenanceManager : IMaintenanceService
    {
        private readonly IMaintenanceDal _maintenanceDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IVehicleAccessService _vehicleAccess;
        private readonly IMaintenancePartDal _partDal;
        private readonly IDocumentDal _documentDal;
        private readonly IDocumentService _documentService;
        private readonly IUnitOfWork _unitOfWork;

        public MaintenanceManager(
            IMaintenanceDal maintenanceDal,
            IVehicleDal vehicleDal,
            IVehicleAccessService vehicleAccess,
            IMaintenancePartDal partDal,
            IDocumentDal documentDal,
            IDocumentService documentService,
            IUnitOfWork unitOfWork)
        {
            _maintenanceDal = maintenanceDal;
            _vehicleDal = vehicleDal;
            _vehicleAccess = vehicleAccess;
            _partDal = partDal;
            _documentDal = documentDal;
            _documentService = documentService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IDataResult<List<MaintenanceDto>>> GetListAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<List<MaintenanceDto>>(Messages.VehicleNotFound);
            var records = await _maintenanceDal.GetRecentAsync(vehicleId, QueryLimits.MaxListSize);
            var parcalar = await _partDal.GetByVehicleAsync(vehicleId) ?? new List<MaintenancePart>();
            var list = records.Select(r => MapToDto(r, parcalar.Where(p => p.MaintenanceRecordId == r.Id))).ToList();
            return new SuccessDataResult<List<MaintenanceDto>>(list);
        }

        public async Task<IDataResult<MaintenanceDto>> AddAsync(int userId, MaintenanceCreateDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<MaintenanceDto>(Messages.VehicleNotFound);
            if (vehicle.Arsivli)
                return new ErrorDataResult<MaintenanceDto>(Messages.AracArsivli);
            if (!DegerSinirlari.TutarGecerli(dto.Cost) || !DegerSinirlari.KmGecerli(dto.Km)
                || !DegerSinirlari.GecmisTarih(dto.Date) || !Enum.IsDefined(dto.Type))
                return new ErrorDataResult<MaintenanceDto>(Messages.InvalidValue);

            var metinHatasi = MetinleriDogrula(dto.ServiceName, dto.Note);
            if (metinHatasi != null)
                return new ErrorDataResult<MaintenanceDto>(metinHatasi);

            var parcaHatasi = ParcalariDogrula(dto.Parcalar);
            if (parcaHatasi != null)
                return new ErrorDataResult<MaintenanceDto>(parcaHatasi);

            var record = new MaintenanceRecord
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = dto.VehicleId,
                Type = dto.Type,
                Date = dto.Date,
                Km = dto.Km,
                Cost = dto.Cost,
                ServiceName = MetinSinirlari.Kirp(dto.ServiceName, MetinSinirlari.ServisAdi),
                Note = MetinSinirlari.Kirp(dto.Note, MetinSinirlari.Not)
            };

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            await _maintenanceDal.AddAsync(record);
            await ParcalariYazAsync(record, dto.Parcalar);

            if (dto.Km > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = dto.Km;
                vehicle.SonKmGuncelleme = DateTime.UtcNow;
                await _vehicleDal.UpdateAsync(vehicle);
            }

            await _unitOfWork.CommitAsync();

            var eklenenParcalar = await _partDal.GetByVehicleAsync(record.VehicleId) ?? new List<MaintenancePart>();
            return new SuccessDataResult<MaintenanceDto>(
                MapToDto(record, eklenenParcalar.Where(p => p.MaintenanceRecordId == record.Id)),
                Messages.RecordAdded);
        }

        public async Task<IDataResult<MaintenanceDto>> UpdateAsync(int userId, int id, MaintenanceUpdateDto dto)
        {
            var record = await _maintenanceDal.GetAsync(m => m.Id == id);
            if (record == null)
                return new ErrorDataResult<MaintenanceDto>(Messages.RecordNotFound);

            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, record.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<MaintenanceDto>(Messages.RecordNotFound);

            if (!DegerSinirlari.TutarGecerli(dto.Cost) || !DegerSinirlari.KmGecerli(dto.Km)
                || !DegerSinirlari.GecmisTarih(dto.Date) || !Enum.IsDefined(dto.Type))
                return new ErrorDataResult<MaintenanceDto>(Messages.InvalidValue);

            var metinHatasi = MetinleriDogrula(dto.ServiceName, dto.Note);
            if (metinHatasi != null)
                return new ErrorDataResult<MaintenanceDto>(metinHatasi);

            var parcaHatasi = ParcalariDogrula(dto.Parcalar);
            if (parcaHatasi != null)
                return new ErrorDataResult<MaintenanceDto>(parcaHatasi);

            record.Type = dto.Type;
            record.Date = dto.Date;
            record.Km = dto.Km;
            record.Cost = dto.Cost;
            record.ServiceName = MetinSinirlari.Kirp(dto.ServiceName, MetinSinirlari.ServisAdi);
            record.Note = MetinSinirlari.Kirp(dto.Note, MetinSinirlari.Not);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            await _maintenanceDal.UpdateAsync(record);
            await _partDal.DeleteByRecordAsync(record.Id);
            await ParcalariYazAsync(record, dto.Parcalar);

            await _unitOfWork.CommitAsync();

            var guncelParcalar = await _partDal.GetByVehicleAsync(record.VehicleId) ?? new List<MaintenancePart>();
            return new SuccessDataResult<MaintenanceDto>(
                MapToDto(record, guncelParcalar.Where(p => p.MaintenanceRecordId == record.Id)),
                Messages.RecordUpdated);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var record = await _maintenanceDal.GetAsync(m => m.Id == id);
            if (record == null)
                return new ErrorResult(Messages.RecordNotFound);
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, record.VehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.RecordNotFound);
            var belgeler = (await _documentDal.GetListAsync(d => d.MaintenanceRecordId == record.Id)).ToList();
            var silinecekDosyalar = belgeler.Select(b => b.StoredName).ToList();

            await using (var islem = await _unitOfWork.BeginTransactionAsync())
            {
                foreach (var belge in belgeler)
                {
                    await _documentDal.DeleteAsync(belge);
                }

                await _maintenanceDal.DeleteAsync(record);
                await _unitOfWork.CommitAsync();
            }

            foreach (var saklananAd in silinecekDosyalar)
            {
                _documentService.DosyaSil(saklananAd);
            }

            return new SuccessResult(Messages.RecordDeleted);
        }

        private static string ParcalariDogrula(List<MaintenancePartDto> parcalar)
        {
            var filtre = UygunsuzIfadeFiltresi.Varsayilan;

            foreach (var parca in parcalar ?? new List<MaintenancePartDto>())
            {
                if (!Enum.IsDefined(parca.ParcaTuru) || parca.Adet <= 0 || parca.Tutar < 0)
                {
                    return Messages.InvalidValue;
                }

                if (!filtre.Temiz(parca.Aciklama) || !filtre.Temiz(parca.Marka))
                {
                    return Messages.UygunsuzIfade;
                }
            }

            return null;
        }

        private static string MetinleriDogrula(string servisAdi, string not)
        {
            var filtre = UygunsuzIfadeFiltresi.Varsayilan;

            return filtre.Temiz(servisAdi) && filtre.Temiz(not) ? null : Messages.UygunsuzIfade;
        }

        private async Task ParcalariYazAsync(MaintenanceRecord record, List<MaintenancePartDto> parcalar)
        {
            foreach (var parca in parcalar ?? new List<MaintenancePartDto>())
            {
                await _partDal.AddAsync(new MaintenancePart
                {
                    CompanyId = record.CompanyId,
                    MaintenanceRecordId = record.Id,
                    VehicleId = record.VehicleId,
                    ParcaTuru = parca.ParcaTuru,
                    Aciklama = MetinSinirlari.Kirp(parca.Aciklama, MetinSinirlari.ParcaAciklama),
                    Adet = parca.Adet,
                    Tutar = parca.Tutar,
                    Marka = MetinSinirlari.Kirp(parca.Marka, MetinSinirlari.ParcaMarka)
                });
            }
        }

        private static MaintenanceDto MapToDto(MaintenanceRecord record, IEnumerable<MaintenancePart> parcalar = null)
        {
            return new MaintenanceDto
            {
                Id = record.Id,
                VehicleId = record.VehicleId,
                Type = record.Type,
                Date = record.Date,
                Km = record.Km,
                Cost = record.Cost,
                ServiceName = record.ServiceName,
                Note = record.Note,
                Parcalar = (parcalar ?? Enumerable.Empty<MaintenancePart>())
                    .Select(p => new MaintenancePartDto
                    {
                        Id = p.Id,
                        ParcaTuru = p.ParcaTuru,
                        Aciklama = p.Aciklama,
                        Adet = p.Adet,
                        Tutar = p.Tutar,
                        Marka = p.Marka
                    }).ToList()
            };
        }
    }
}
