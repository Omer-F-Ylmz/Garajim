using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public static class KasaTipiAdlari
    {
        public static string ModelDegeri(KasaTipi kasa)
        {
            return kasa switch
            {
                KasaTipi.Sedan => "Sedan",
                KasaTipi.Hatchback5 => "Hatchback/5",
                KasaTipi.Hatchback3 => "Hatchback/3",
                KasaTipi.StationWagon => "Station wagon",
                KasaTipi.Mpv => "MPV",
                KasaTipi.Coupe => "Coupe",
                KasaTipi.Suv => "SUV",
                KasaTipi.Cabrio => "Cabrio",
                KasaTipi.Roadster => "Roadster",
                _ => "Pick-up"
            };
        }

        public static string Ad(KasaTipi kasa)
        {
            return kasa switch
            {
                KasaTipi.Sedan => "Sedan",
                KasaTipi.Hatchback5 => "Hatchback (5 kapı)",
                KasaTipi.Hatchback3 => "Hatchback (3 kapı)",
                KasaTipi.StationWagon => "Station wagon",
                KasaTipi.Mpv => "MPV",
                KasaTipi.Coupe => "Coupe",
                KasaTipi.Suv => "SUV",
                KasaTipi.Cabrio => "Cabrio",
                KasaTipi.Roadster => "Roadster",
                _ => "Pick-up"
            };
        }
    }
}
