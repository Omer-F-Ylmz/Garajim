namespace Garajim.Business.Concrete
{
    public static class TarihToleransi
    {
        public const int YerelSaatPayiGun = 1;

        public static DateTime EnGecGun()
        {
            return DateTime.UtcNow.Date.AddDays(YerelSaatPayiGun);
        }
    }
}
