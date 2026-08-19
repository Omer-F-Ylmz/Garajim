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
        private readonly IVehicleDal _vehicleDal;

        public ExpenseManager(IExpenseDal expenseDal, IVehicleDal vehicleDal)
        {
            _expenseDal = expenseDal;
            _vehicleDal = vehicleDal;
        }

        public async Task<IDataResult<List<ExpenseDto>>> GetListAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == vehicleId && v.UserId == userId);
            if (vehicle == null)
                return new ErrorDataResult<List<ExpenseDto>>(Messages.VehicleNotFound);
            var records = await _expenseDal.GetListAsync(e => e.VehicleId == vehicleId);
            var list = records.OrderByDescending(e => e.Date).Select(MapToDto).ToList();
            return new SuccessDataResult<List<ExpenseDto>>(list);
        }

        public async Task<IDataResult<ExpenseDto>> AddAsync(int userId, ExpenseCreateDto dto)
        {
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == dto.VehicleId && v.UserId == userId);
            if (vehicle == null)
                return new ErrorDataResult<ExpenseDto>(Messages.VehicleNotFound);
            if (dto.Amount < 0 || !Enum.IsDefined(dto.Category))
                return new ErrorDataResult<ExpenseDto>(Messages.InvalidValue);
            var record = new ExpenseRecord
            {
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
            var vehicle = await _vehicleDal.GetAsync(v => v.Id == record.VehicleId && v.UserId == userId);
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
