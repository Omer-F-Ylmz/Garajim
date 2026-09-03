using Garajim.Business.Abstract;

namespace Garajim.Business.Concrete
{
    public class Saat : ISaat
    {
        public const string IanaKimlik = "Europe/Istanbul";
        public const string WindowsKimlik = "Turkey Standard Time";

        public static readonly TimeZoneInfo Dilim = DilimiBul();

        public static readonly ISaat Varsayilan = new Saat();

        public DateTime SimdiUtc => DateTime.UtcNow;

        public DateTime YerelSimdi => Yerel(DateTime.UtcNow);

        public DateTime Bugun => Yerel(DateTime.UtcNow).Date;

        public static DateTime Yerel(DateTime utc)
        {
            var kaynak = utc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
                : utc.ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(kaynak, Dilim);
        }

        public static DateTime BugunTr()
        {
            return Yerel(DateTime.UtcNow).Date;
        }

        public static DateTime GunBasiUtc()
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(BugunTr(), DateTimeKind.Unspecified), Dilim);
        }

        private static TimeZoneInfo DilimiBul()
        {
            foreach (var kimlik in new[] { IanaKimlik, WindowsKimlik })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(kimlik);
                }
                catch (Exception hata) when (hata is TimeZoneNotFoundException or InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.CreateCustomTimeZone(WindowsKimlik, TimeSpan.FromHours(3), WindowsKimlik, WindowsKimlik);
        }
    }
}
