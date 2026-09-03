using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class ExpenseManager : IExpenseService
    {
        private readonly IExpenseDal _expenseDal;
        private readonly IVehicleAccessService _vehicleAccess;

        public ExpenseManager(IExpenseDal expenseDal, IVehicleAccessService vehicleAccess)
        {
            _expenseDal = expenseDal;
            _vehicleAccess = vehicleAccess;
        }

        public async Task<IDataResult<List<ExpenseDto>>> GetListAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<List<ExpenseDto>>(Messages.VehicleNotFound);
            var records = await _expenseDal.GetRecentAsync(vehicleId, QueryLimits.MaxListSize);
            var list = records.Select(MapToDto).ToList();
            return new SuccessDataResult<List<ExpenseDto>>(list);
        }

        public async Task<IDataResult<ExpenseDto>> AddAsync(int userId, ExpenseCreateDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<ExpenseDto>(Messages.VehicleNotFound);
            if (vehicle.Arsivli)
                return new ErrorDataResult<ExpenseDto>(Messages.AracArsivli);
            if (!DegerSinirlari.TutarGecerli(dto.Amount) || !DegerSinirlari.GecmisTarih(dto.Date)
                || !Enum.IsDefined(dto.Category))
                return new ErrorDataResult<ExpenseDto>(Messages.InvalidValue);
            var record = new ExpenseRecord
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = dto.VehicleId,
                Category = dto.Category,
                Date = dto.Date,
                Amount = dto.Amount,
                Note = dto.Note
            };
            await _expenseDal.AddAsync(record);
            return new SuccessDataResult<ExpenseDto>(MapToDto(record), Messages.RecordAdded);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var record = await _expenseDal.GetAsync(e => e.Id == id);
            if (record == null)
                return new ErrorResult(Messages.RecordNotFound);
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, record.VehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.RecordNotFound);
            await _expenseDal.DeleteAsync(record);
            return new SuccessResult(Messages.RecordDeleted);
        }

        private static ExpenseDto MapToDto(ExpenseRecord record)
        {
            return new ExpenseDto
            {
                Id = record.Id,
                VehicleId = record.VehicleId,
                Category = record.Category,
                Date = record.Date,
                Amount = record.Amount,
                Note = record.Note
            };
        }
    }
}
