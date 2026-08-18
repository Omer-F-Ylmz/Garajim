using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class ReminderCreateDto
    {
        public int VehicleId { get; set; }
        public ReminderType Type { get; set; }
        public DateTime? DueDate { get; set; }
        public int? DueKm { get; set; }
        public string Note { get; set; }
    }

    public class ReminderDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public ReminderType Type { get; set; }
        public DateTime? DueDate { get; set; }
        public int? DueKm { get; set; }
        public string Note { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastNotifiedAt { get; set; }
    }

    public class UpcomingReminderDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string Plate { get; set; }
        public ReminderType Type { get; set; }
        public DateTime DueDate { get; set; }
        public string Note { get; set; }
    }

    public class ReminderDueDto
    {
        public int ReminderId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Plate { get; set; }
        public ReminderType Type { get; set; }
        public DateTime DueDate { get; set; }
    }
}
