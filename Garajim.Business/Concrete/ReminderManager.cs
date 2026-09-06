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

        public static readonly string[] SiralamaAlanlari = { "tarih", "km", "durum" };

        public async Task<IDataResult<SayfaliSonuc<ReminderDto>>> GetSayfaAsync(int userId, int vehicleId, ListeSorgusu sorgu)
        {
            var vehicle = await _vehicleAccess.GetAccessibleAsync(userId, vehicleId);
            if (vehicle == null)
                return new ErrorDataResult<SayfaliSonuc<ReminderDto>>(Messages.VehicleNotFound);

            var siralama = sorgu.SiralamaCoz(SiralamaAlanlari, "tarih");
            if (!siralama.Gecerli)
                return new ErrorDataResult<SayfaliSonuc<ReminderDto>>(Messages.SiralamaGecersiz);

            var sayfa = await _reminderDal.SayfaAsync(vehicleId, sorgu, siralama);
            var liste = sayfa.Kayitlar.Select(MapToDto).ToList();

            return new SuccessDataResult<SayfaliSonuc<ReminderDto>>(
                new SayfaliSonuc<ReminderDto>(liste, sayfa.Toplam, sayfa.Sayfa, sayfa.Boyut));
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
            if (!TekrarGecerliMi(dto.TekrarAy, dto.TekrarKm))
                return new ErrorDataResult<ReminderDto>(Messages.TekrarDegeriGecersiz);
            var reminder = new Reminder
            {
                CompanyId = vehicle.CompanyId,
                VehicleId = dto.VehicleId,
                Type = dto.Type,
                DueDate = dto.DueDate,
                DueKm = dto.DueKm,
                Note = MetinSinirlari.Kirp(dto.Note, MetinSinirlari.Not),
                TekrarAy = dto.TekrarAy,
                TekrarKm = dto.TekrarKm,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };
            await _reminderDal.AddAsync(reminder);
            return new SuccessDataResult<ReminderDto>(MapToDto(reminder), Messages.ReminderAdded);
        }

        public static readonly int EnCokTekrarAy = 120;
        public static readonly int EnCokTekrarKm = 200000;

        private static bool TekrarGecerliMi(int? ay, int? km)
        {
            if (ay != null && (ay.Value < 1 || ay.Value > EnCokTekrarAy))
            {
                return false;
            }

            return km == null || (km.Value >= 1 && km.Value <= EnCokTekrarKm);
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

            await TekrariAcAsync(reminder);

            return new SuccessResult(Messages.ReminderCompleted);
        }

        private async Task TekrariAcAsync(Reminder kaynak)
        {
            if (kaynak.TekrarAy == null && kaynak.TekrarKm == null)
            {
                return;
            }

            if (await _reminderDal.TekrardanUretilmisMiAsync(kaynak.Id))
            {
                return;
            }

            var yeni = new Reminder
            {
                CompanyId = kaynak.CompanyId,
                VehicleId = kaynak.VehicleId,
                Type = kaynak.Type,
                DueDate = kaynak.TekrarAy != null && kaynak.DueDate != null
                    ? kaynak.DueDate.Value.AddMonths(kaynak.TekrarAy.Value)
                    : null,
                DueKm = kaynak.TekrarKm != null && kaynak.DueKm != null
                    ? kaynak.DueKm.Value + kaynak.TekrarKm.Value
                    : null,
                Note = kaynak.Note,
                TekrarAy = kaynak.TekrarAy,
                TekrarKm = kaynak.TekrarKm,
                TekrardanUretenId = kaynak.Id,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            if (yeni.DueDate == null && yeni.DueKm == null)
            {
                return;
            }

            await _reminderDal.AddAsync(yeni);
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
                LastNotifiedAt = reminder.LastNotifiedAt,
                TekrarAy = reminder.TekrarAy,
                TekrarKm = reminder.TekrarKm
            };
        }
    }
}
