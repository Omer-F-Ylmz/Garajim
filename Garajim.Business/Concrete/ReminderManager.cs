using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;

namespace Garajim.Business.Concrete
{
    public class ReminderManager : IReminderService
    {
        private readonly IReminderDal _reminderDal;
        private readonly IVehicleAccessService _vehicleAccess;

        public ReminderManager(IReminderDal reminderDal, IVehicleAccessService vehicleAccess)
        {
            _reminderDal = reminderDal;
            _vehicleAccess = vehicleAccess;
        }

        public async Task<IDataResult<List<ReminderDto>>> GetListAsync(int userId, int vehicleId)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<List<ReminderDto>>(Messages.VehicleNotFound);
            var reminders = await _reminderDal.GetListForVehicleAsync(vehicleId, QueryLimits.MaxListSize);
            var list = reminders.Select(MapToDto).ToList();
            return new SuccessDataResult<List<ReminderDto>>(list);
        }

        public async Task<IDataResult<List<UpcomingReminderDto>>> GetUpcomingAsync(int userId, int days)
        {
            if (days < 1)
                days = 1;
            if (days > 365)
                days = 365;
            var limit = Saat.BugunTr().AddDays(days);
            var list = await _reminderDal.GetUpcomingForUserAsync(userId, limit);
            return new SuccessDataResult<List<UpcomingReminderDto>>(list);
        }

        public async Task<IDataResult<ReminderDto>> AddAsync(int userId, ReminderCreateDto dto)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, dto.VehicleId);
            if (vehicle == null)
                return new ErrorDataResult<ReminderDto>(Messages.VehicleNotFound);
            if (!Enum.IsDefined(dto.Type))
                return new ErrorDataResult<ReminderDto>(Messages.InvalidValue);
            if (dto.DueKm.HasValue && dto.DueKm.Value <= 0)
                return new ErrorDataResult<ReminderDto>(Messages.InvalidValue);
            if (dto.DueDate == null && dto.DueKm == null)
                return new ErrorDataResult<ReminderDto>(Messages.ReminderDateOrKmRequired);
            var reminder = new Reminder
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = dto.VehicleId,
                Type = dto.Type,
                DueDate = dto.DueDate,
                DueKm = dto.DueKm,
                Note = MetinSinirlari.Kirp(dto.Note, MetinSinirlari.Not),
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };
            await _reminderDal.AddAsync(reminder);
            return new SuccessDataResult<ReminderDto>(MapToDto(reminder), Messages.ReminderAdded);
        }

        public async Task<IResult> CompleteAsync(int userId, int id)
        {
            var reminder = await _reminderDal.GetAsync(r => r.Id == id);
            if (reminder == null)
                return new ErrorResult(Messages.ReminderNotFound);
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, reminder.VehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.ReminderNotFound);
            reminder.IsCompleted = true;
            await _reminderDal.UpdateAsync(reminder);
            return new SuccessResult(Messages.ReminderCompleted);
        }

        public async Task<IResult> DeleteAsync(int userId, int id)
        {
            var reminder = await _reminderDal.GetAsync(r => r.Id == id);
            if (reminder == null)
                return new ErrorResult(Messages.ReminderNotFound);
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, reminder.VehicleId);
            if (vehicle == null)
                return new ErrorResult(Messages.ReminderNotFound);
            await _reminderDal.DeleteAsync(reminder);
            return new SuccessResult(Messages.ReminderDeleted);
        }

        private static ReminderDto MapToDto(Reminder reminder)
        {
            return new ReminderDto
            {
                Id = reminder.Id,
                VehicleId = reminder.VehicleId,
                Type = reminder.Type,
                DueDate = reminder.DueDate,
                DueKm = reminder.DueKm,
                Note = reminder.Note,
                IsCompleted = reminder.IsCompleted,
                LastNotifiedAt = reminder.LastNotifiedAt
            };
        }
    }
}
