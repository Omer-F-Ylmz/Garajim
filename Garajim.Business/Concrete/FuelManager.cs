using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class FuelManager : IFuelService
    {
        private readonly IFuelDal _fuelDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IVehicleAccessService _vehicleAccess;

        public FuelManager(IFuelDal fuelDal, IVehicleDal vehicleDal, IVehicleAccessService vehicleAccess)
        {
            _fuelDal = fuelDal;
            _vehicleDal = vehicleDal;
            _vehicleAccess = vehicleAccess;
        }

        public async Task<IDataResult<List<FuelDto>>> GetListAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<List<FuelDto>>(Messages.VehicleNotFound);
            var records = await _fuelDal.GetRecentAsync(vehicleId, QueryLimits.MaxListSize);
            var list = records.Select(MapToDto).ToList();
            return new SuccessDataResult<List<FuelDto>>(list);
        }

        public async Task<IDataResult<FuelDto>> AddAsync(int userId, FuelCreateDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<FuelDto>(Messages.VehicleNotFound);
            if (vehicle.Arsivli)
                return new ErrorDataResult<FuelDto>(Messages.AracArsivli);
            var hata = Dogrula(vehicle.FuelType, dto);
            if (hata != null)
                return new ErrorDataResult<FuelDto>(hata);
            var record = new FuelRecord
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = dto.VehicleId,
                Date = dto.Date,
                Liters = dto.Liters,
                TotalCost = dto.TotalCost,
                Km = dto.Km,
                Kwh = dto.Kwh,
                SarjTuru = dto.SarjTuru,
                TamDolum = dto.TamDolum ?? true
            };
            await _fuelDal.AddAsync(record);
            await SupheliBayraklariniGuncelleAsync(dto.VehicleId);
            if (dto.Km > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = dto.Km;
                vehicle.SonKmGuncelleme = DateTime.UtcNow;
                await _vehicleDal.UpdateAsync(vehicle);
            }
            return new SuccessDataResult<FuelDto>(MapToDto(record), Messages.RecordAdded);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var record = await _fuelDal.GetAsync(f => f.Id == id);
            if (record == null)
                return new ErrorResult(Messages.RecordNotFound);
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, record.VehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.RecordNotFound);
            await _fuelDal.DeleteAsync(record);
            await SupheliBayraklariniGuncelleAsync(record.VehicleId);
            return new SuccessResult(Messages.RecordDeleted);
        }

        private async Task SupheliBayraklariniGuncelleAsync(int vehicleId)
        {
            var kayitlar = await _fuelDal.GetListAsync(f => f.VehicleId == vehicleId);
            var sonuc = TuketimHesabi.Hesapla(kayitlar);

            await _fuelDal.SupheliGuncelleAsync(vehicleId, sonuc.SupheliKayitlar.ToList());
        }

        private static string Dogrula(FuelType yakitTuru, FuelCreateDto dto)
        {
            if (!DegerSinirlari.TutarGecerli(dto.TotalCost) || !DegerSinirlari.KmGecerli(dto.Km)
                || !DegerSinirlari.LitreGecerli(dto.Liters) || !DegerSinirlari.KwhGecerli(dto.Kwh)
                || !DegerSinirlari.GecmisTarih(dto.Date))
                return Messages.InvalidValue;
            if (dto.SarjTuru != null && !Enum.IsDefined(dto.SarjTuru.Value))
                return Messages.InvalidValue;

            var litreVar = dto.Liters > 0;
            var kwhVar = dto.Kwh != null && dto.Kwh.Value > 0;

            if (yakitTuru == FuelType.Elektrik)
            {
                if (litreVar)
                    return Messages.ElektrikliAracaYakit;
                return kwhVar ? null : Messages.SarjMiktariGerekli;
            }

            if (yakitTuru == FuelType.Hibrit)
            {
                return litreVar || kwhVar ? null : Messages.InvalidValue;
            }

            if (kwhVar)
                return Messages.YakitliAracaSarj;

            return litreVar ? null : Messages.InvalidValue;
        }

        private static FuelDto MapToDto(FuelRecord record)
        {
            return new FuelDto
            {
                Id = record.Id,
                VehicleId = record.VehicleId,
                Date = record.Date,
                Liters = record.Liters,
                TotalCost = record.TotalCost,
                Km = record.Km,
                Kwh = record.Kwh,
                SarjTuru = record.SarjTuru?.ToString(),
                TamDolum = record.TamDolum,
                SupheliKm = record.SupheliKm
            };
        }
    }
}
